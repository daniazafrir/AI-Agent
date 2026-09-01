import { Injectable } from '@angular/core';
import {
    Observable,
    Subscriber
} from 'rxjs';

import { environment } from '../../../environments/environment';
import { ChatRequest, ChatStreamEvent } from 'src/app/core/models/chat.models';


@Injectable({
    providedIn: 'root'
})
export class ChatStreamService {
    private abortController?: AbortController;

    stream(
        request: ChatRequest
    ): Observable<ChatStreamEvent> {

        return new Observable<ChatStreamEvent>(
            observer => {

                const controller =
                    new AbortController();

                this.abortController =
                    controller;

                this.startStreaming(
                    request,
                    observer,
                    controller
                );

                return () => {
                    controller.abort();

                    if (
                        this.abortController ===
                        controller
                    ) {
                        this.abortController =
                            undefined;
                    }
                };
            }
        );
    }

    private async startStreaming(
        request: ChatRequest,
        observer: Subscriber<ChatStreamEvent>,
        controller: AbortController
    ): Promise<void> {
        try {

            const response =
                await fetch(
                    `${environment.apiUrl}/api/chat/stream`,
                    {
                        method: 'POST',

                        headers: {
                            'Content-Type':
                                'application/json'
                        },

                        body:
                            JSON.stringify(request),

                        signal:
                            controller.signal
                    }
                );

            if (!response.ok) {
                throw new Error(
                    `Streaming request failed with HTTP ${response.status}.`
                );
            }

            if (!response.body) {
                throw new Error(
                    'Streaming response has no body.'
                );
            }

            const reader =
                response.body.getReader();

            const decoder =
                new TextDecoder();

            let buffer = '';

            while (true) {

                const {
                    value,
                    done
                } = await reader.read();

                if (done) {
                    break;
                }

                buffer += decoder.decode(
                    value,
                    {
                        stream: true
                    }
                );

                const blocks =
                    buffer.split(/\r?\n\r?\n/);

                buffer =
                    blocks.pop() ?? '';

                for (const block of blocks) {

                    const dataLine =
                        block
                            .split(/\r?\n/)
                            .find(line =>
                                line.startsWith('data:')
                            );

                    if (!dataLine) {
                        continue;
                    }

                    const json =
                        dataLine
                            .substring(5)
                            .trim();

                    if (!json) {
                        continue;
                    }

                    const raw =
                        JSON.parse(json);

                    /*
                     * כרגע ה-.NET שלך מחזיר
                     * Type / Content / UsedTools
                     * ב-PascalCase.
                     *
                     * אנחנו מנרמלים כאן ל-camelCase.
                     */
                    const rawSources =
                        raw.sources ??
                        raw.Sources ??
                        [];

                    const event: ChatStreamEvent = {

                        type:
                            raw.type ??
                            raw.Type,

                        content:
                            raw.content ??
                            raw.Content,

                        toolName:
                            raw.toolName ??
                            raw.ToolName,

                        usedTools:
                            raw.usedTools ??
                            raw.UsedTools,

                        conversationId:
                            raw.conversationId ??
                            raw.ConversationId,

                        sources:
                            rawSources.map((source: any) => ({
                                documentId:
                                    source.documentId ??
                                    source.DocumentId,

                                documentName:
                                    source.documentName ??
                                    source.DocumentName,

                                chunkIndex:
                                    source.chunkIndex ??
                                    source.ChunkIndex,

                                score:
                                    source.score ??
                                    source.Score
                            }))
                    };

                    observer.next(event);
                }
            }

            observer.complete();

        }
        catch (error) {

            if (
                controller.signal.aborted
            ) {
                observer.complete();
                return;
            }

            observer.error(error);
        }
    }

    stop(): void {
        this.abortController?.abort();
        this.abortController = undefined;
    }
}