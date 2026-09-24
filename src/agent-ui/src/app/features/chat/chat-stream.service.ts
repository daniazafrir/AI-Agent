// chat-stream.service.ts

import { Injectable } from '@angular/core';
import {
  Observable,
  Subscriber
} from 'rxjs';

import { environment } from '../../../environments/environment';
import {
  ChatRequest,
  ChatStreamEvent
} from 'src/app/core/models/chat.models';

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

    let timedOut = false;
    let completed = false;
    const timeout = setTimeout(() => {
      timedOut = true;
      controller.abort();
    }, 120_000);

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

          const rawSources =
            raw.sources ??
            raw.Sources ??
            [];

          const rawDebug =
            raw.debug ??
            raw.Debug ??
            null;

          const event:
            ChatStreamEvent = {
            prompt: (raw.prompt ?? raw.Prompt) ? {
              round: (raw.prompt ?? raw.Prompt).round ?? (raw.prompt ?? raw.Prompt).Round,
              model: (raw.prompt ?? raw.Prompt).model ?? (raw.prompt ?? raw.Prompt).Model,
              capturedAtUtc: (raw.prompt ?? raw.Prompt).capturedAtUtc ?? (raw.prompt ?? raw.Prompt).CapturedAtUtc,
              messagesJson: (raw.prompt ?? raw.Prompt).messagesJson ?? (raw.prompt ?? raw.Prompt).MessagesJson,
              optionsJson: (raw.prompt ?? raw.Prompt).optionsJson ?? (raw.prompt ?? raw.Prompt).OptionsJson
            } : null,

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
              rawSources.map(
                (source: any) => ({
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
                })
              ),

            debug:
  rawDebug
    ? {
        matches: (rawDebug.matches ?? rawDebug.Matches ?? []).map((match: any) => ({
          documentName: match.documentName ?? match.DocumentName ?? '',
          chunkIndex: match.chunkIndex ?? match.ChunkIndex ?? 0,
          score: match.score ?? match.Score ?? 0,
          searchEngine: match.searchEngine ?? match.SearchEngine ?? 'Unknown'
        })),
        toolName:
          rawDebug.toolName ??
          rawDebug.ToolName ??
          '',

        query:
          rawDebug.query ??
          rawDebug.Query ??
          '',

        rawVectorResults:
          rawDebug.rawVectorResults ??
          rawDebug.RawVectorResults ??
          0,

        relevantVectorResults:
          rawDebug.relevantVectorResults ??
          rawDebug.RelevantVectorResults ??
          0,

        vectorResults:
          rawDebug.vectorResults ??
          rawDebug.VectorResults ??
          0,

        keywordResults:
          rawDebug.keywordResults ??
          rawDebug.KeywordResults ??
          0,

        mergedResults:
          rawDebug.mergedResults ??
          rawDebug.MergedResults ??
          0,

        minimumVectorScore:
          rawDebug.minimumVectorScore ??
          rawDebug.MinimumVectorScore ??
          0,

        searchTimeMs:
          rawDebug.searchTimeMs ??
          rawDebug.SearchTimeMs ??
          0
      }
    : null
          };

          if (event.type === 'error') {
            throw new Error(event.content || 'אירעה תקלה ביצירת התשובה. נסו שוב.');
          }
          if (event.type === 'completed') {
            completed = true;
          }
          observer.next(event);
        }
      }

      if (!completed) {
        throw new Error('החיבור נותק לפני שהתשובה הושלמה. אפשר לשלוח הודעה חדשה.');
      }
      observer.complete();

    }
    catch (error) {
      if (timedOut) {
        observer.error(new Error('זמן ההמתנה לתשובה הסתיים. נסו שוב.'));
      } else if (controller.signal.aborted) {
        observer.complete();
      } else {
        observer.error(error);
      }
    } finally {
      clearTimeout(timeout);
      controller.abort();
    }
  }
  stop(): void {
    this.abortController?.abort();
    this.abortController =
      undefined;
  }
}
