import { computed, inject, Injectable, signal } from '@angular/core';
import { finalize } from 'rxjs';

import { ChatApiService } from '../../core/services/chat-api.service';
import { ChatMessage } from 'src/app/core/models/chat.models';
import { ConversationDetails } from '../conversations/conversation-api.service';

@Injectable({
    providedIn: 'root'
})
export class ChatStateService {

    private readonly chatApi = inject(ChatApiService);

    readonly messages = signal<ChatMessage[]>([]);
    readonly conversationId = signal<string | null>(null);
    readonly isSending = signal(false);
    readonly error = signal<string | null>(null);
    readonly activeConversationId =
        signal<string | null>(null);
    readonly hasMessages = computed(
        () => this.messages().length > 0
    );

    send(message: string): void {

        const text = message.trim();

        if (!text || this.isSending()) {
            return;
        }

        this.error.set(null);
        this.isSending.set(true);

        this.addMessage({
            role: 'user',
            content: text
        });

        this.chatApi
            .sendMessage({
                message: text,
                conversationId: this.conversationId()
            })
            .pipe(
                finalize(() => {
                    this.isSending.set(false);
                })
            )
            .subscribe({
                next: response => {

                    this.conversationId.set(
                        response.conversationId
                    );

                    this.addMessage({
                        role: 'assistant',
                        content: response.answer,
                        usedTools: response.usedTools
                    });
                },

                error: error => {

                    console.error(
                        'Failed to send chat message.',
                        error
                    );

                    this.error.set(
                        'Failed to send the message. Please try again.'
                    );
                }
            });
    }

    addMessage(message: ChatMessage): void {
        this.messages.update(current => [
            ...current,
            message
        ]);
    }

    appendMessages(messages: ChatMessage[]): void {
        this.messages.update(current => [
            ...current,
            ...messages
        ]);
    }

    setMessages(messages: ChatMessage[]): void {
        this.messages.set(messages);
    }

    setConversationId(
        conversationId: string | null
    ): void {
        this.conversationId.set(conversationId);
    }

    clear(): void {
        this.messages.set([]);
        this.conversationId.set(null);
        this.activeConversationId.set(null);
        this.error.set(null);
        this.isSending.set(false);
    }

    loadConversation(conversation: ConversationDetails): void {
        this.messages.set(conversation.messages);

        this.conversationId.set(conversation.conversationId);

        this.activeConversationId.set(conversation.conversationId);

        this.error.set(null);
        this.isSending.set(false);

    }
}