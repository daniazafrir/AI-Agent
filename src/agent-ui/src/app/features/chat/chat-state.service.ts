import { finalize, Subscription } from "rxjs";
import { ChatStreamService } from "./chat-stream.service";
import { ChatMessage } from "src/app/core/models/chat.models";
import { computed, inject, Injectable, signal } from "@angular/core";
import { ConversationDetails } from "../conversations/conversation-api.service";

@Injectable({
    providedIn: 'root'
})
export class ChatStateService {

    private readonly chatStream =
        inject(ChatStreamService);

    private streamSubscription:
        Subscription | null = null;

    readonly messages =
        signal<ChatMessage[]>([]);

    readonly conversationId =
        signal<string | null>(null);

    readonly isSending =
        signal(false);

    readonly error =
        signal<string | null>(null);

    readonly activeConversationId =
        signal<string | null>(null);

    readonly activeTool =
        signal<string | null>(null);

    readonly hasMessages =
        computed(
            () => this.messages().length > 0
        );

    

    private setAssistantMetadata(
        usedTools: string[],
        sources: {
            documentId: string;
            documentName: string;
            chunkIndex: number;
            score: number;
        }[]
        ): void {

        this.messages.update(
            current => {

            if (current.length === 0) {
                return current;
            }

            const messages = [...current];

            const index = messages.length - 1;

            const last = messages[index];

            if (last.role !== 'assistant') {
                return current;
            }

            messages[index] = {
                ...last,
                usedTools,
                sources
            };

            return messages;
            }
        );
        }
    send(message: string): void {

        const text =
            message.trim();

        if (
            !text ||
            this.isSending()
        ) {
            return;
        }

        this.error.set(null);
        this.activeTool.set(null);
        this.isSending.set(true);

        /*
         * הודעת המשתמש
         */
        this.addMessage({
            role: 'user',
            content: text
        });

        /*
         * Placeholder של ה-Agent.
         * אליו נוסיף chunks.
         */
        this.addMessage({
            role: 'assistant',
            content: '',
            usedTools: [],
            sources: []
        });

        this.streamSubscription =
            this.chatStream
                .stream({
                    message: text,
                    conversationId:
                        this.conversationId()
                })
                .pipe(
                    finalize(() => {
                        this.isSending.set(false);
                        this.activeTool.set(null);
                        this.streamSubscription = null;
                    })
                )
                .subscribe({

                    next: event => {
                        switch (event.type) {
                            case 'conversation':
                                if (event.conversationId) {
                                    this.conversationId.set(
                                        event.conversationId
                                    );

                                    this.activeConversationId.set(
                                        event.conversationId
                                    );
                                }
                                break;

                            case 'content':
                                if (event.content) {
                                    this.appendAssistantContent(
                                        event.content
                                    );
                                }
                                break;

                            case 'tool-started':
                                this.activeTool.set(
                                    event.toolName ?? null
                                );
                                break;

                            case 'tool-completed':
                                this.activeTool.set(null);
                                break;

                            case 'completed':
                                this.setAssistantMetadata(
                                    event.usedTools ?? [],
                                    event.sources ?? []
                                );

                                this.activeTool.set(null);
                                break;
                        }
                    },

                });
    }


    private appendAssistantContent(
        chunk: string
    ): void {

        this.messages.update(
            current => {

                if (current.length === 0) {
                    return current;
                }

                const messages =
                    [...current];

                const index =
                    messages.length - 1;

                const last =
                    messages[index];

                if (
                    last.role !==
                    'assistant'
                ) {
                    return current;
                }

                messages[index] = {
                    ...last,
                    content:
                        last.content + chunk
                };

                return messages;
            }
        );
    }


    private setAssistantTools(
        usedTools: string[]
    ): void {

        this.messages.update(
            current => {

                if (current.length === 0) {
                    return current;
                }

                const messages =
                    [...current];

                const index =
                    messages.length - 1;

                const last =
                    messages[index];

                if (
                    last.role !==
                    'assistant'
                ) {
                    return current;
                }

                messages[index] = {
                    ...last,
                    usedTools
                };

                return messages;
            }
        );
    }


    private removeEmptyAssistantMessage():
        void {

        this.messages.update(
            current => {

                const last =
                    current.at(-1);

                if (
                    last?.role ===
                    'assistant' &&
                    !last.content
                ) {
                    return current.slice(
                        0,
                        -1
                    );
                }

                return current;
            }
        );
    }


    stopGeneration(): void {
        this.streamSubscription
            ?.unsubscribe();

        this.streamSubscription = null;

        this.isSending.set(false);
        this.activeTool.set(null);
    }


    addMessage(
        message: ChatMessage
    ): void {

        this.messages.update(
            current => [
                ...current,
                message
            ]
        );
    }


    appendMessages(
        messages: ChatMessage[]
    ): void {

        this.messages.update(
            current => [
                ...current,
                ...messages
            ]
        );
    }


    setMessages(
        messages: ChatMessage[]
    ): void {

        this.messages.set(
            messages
        );
    }


    setConversationId(
        conversationId:
            string | null
    ): void {

        this.conversationId.set(
            conversationId
        );
    }


    clear(): void {

        this.stopGeneration();

        this.messages.set([]);

        this.conversationId.set(
            null
        );

        this.activeConversationId.set(
            null
        );

        this.error.set(null);
    }


    loadConversation(
        conversation:
            ConversationDetails
    ): void {

        this.stopGeneration();

        this.messages.set(
            conversation.messages
        );

        this.conversationId.set(
            conversation.conversationId
        );

        this.activeConversationId.set(
            conversation.conversationId
        );

        this.error.set(null);
    }
}