import {
  computed,
  inject,
  Injectable,
  signal
} from '@angular/core';

import {
  finalize,
  Subscription
} from 'rxjs';

import {
  ChatDebugInfo,
  ChatMessage,
  PromptSnapshot
} from 'src/app/core/models/chat.models';

import { ChatStreamService } from './chat-stream.service';
import { ChatRequestMetric } from './chat-request-metric';
import { ConversationDetails } from '../conversations/conversation-api.service';

@Injectable({
  providedIn: 'root'
})
export class ChatStateService {
  private requestSequence = 0;
  private readonly metrics = signal<ChatRequestMetric[]>([]);
  readonly requestHistory = this.metrics.asReadonly();
  readonly prompts = signal<PromptSnapshot[]>([]);
  private finishActiveRequest: (() => void) | null = null;

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

  readonly debugInfo =
    signal<ChatDebugInfo | null>(null);

  readonly hasMessages =
    computed(
      () => this.messages().length > 0
    );

  send(message: string): void {

    const text =
      message.trim();

    if (
      !text ||
      this.isSending()
    ) {
      return;
    }

    const started = performance.now();
    const metric: ChatRequestMetric = {
      id: ++this.requestSequence,
      question: text,
      startedAt: Date.now(),
      firstTextMs: null,
      totalMs: 0,
      searchMs: null,
      sourceCount: null,
      status: 'cancelled'
    };
    let recorded = false;
    const record = () => {
      if (recorded) return;
      recorded = true;
      metric.totalMs = Math.round(performance.now() - started);
      this.metrics.update(history => [{ ...metric }, ...history].slice(0, 20));
      this.finishActiveRequest = null;
    };
    this.finishActiveRequest = record;

    this.error.set(null);
    this.activeTool.set(null);
    this.debugInfo.set(null);
    this.prompts.set([]);
    this.isSending.set(true);

    this.addMessage({
      role: 'user',
      content: text
    });

    this.addMessage({
      role: 'assistant',
      content: '',
      usedTools: [],
      sources: []
    });

    const subscription =
      this.chatStream
        .stream({
          message: text,
          conversationId:
            this.conversationId()
        })
        .pipe(
          finalize(() => {
            record();
            this.isSending.set(false);
            this.activeTool.set(null);
            this.streamSubscription = null;
          })
        )
        .subscribe({

          next: event => {

            switch (event.type) {
              case 'prompt':
                if (event.prompt) this.prompts.update(items => [...items, event.prompt!]);
                break;

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
                  metric.firstTextMs ??= Math.round(performance.now() - started);

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
                metric.status = 'completed';
                metric.searchMs = event.debug?.searchTimeMs ?? null;
                metric.sourceCount = event.sources?.length ?? 0;

                this.setAssistantMetadata(
                  event.usedTools ?? [],
                  event.sources ?? []
                );

                this.debugInfo.set(
                  event.debug ?? null
                );

                this.activeTool.set(null);

                break;
            }
          },

          error: error => {
            metric.status = 'failed';

            console.error(
              'Chat streaming failed.',
              error
            );

            this.error.set(
              error instanceof Error ? error.message : 'אירעה תקלה ביצירת התשובה.'
            );

            this.markAssistantIncomplete();
          }
        });
    this.streamSubscription = subscription.closed ? null : subscription;
  }

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

        const messages =
          [...current];

        const index =
          messages.length - 1;

        const last =
          messages[index];

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

        if (last.role !== 'assistant') {
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

  private removeEmptyAssistantMessage():
    void {

    this.messages.update(
      current => {

        const last =
          current.at(-1);

        if (
          last?.role === 'assistant' &&
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

  private markAssistantIncomplete(): void {
    this.removeEmptyAssistantMessage();
    this.messages.update(current => {
      const last = current.at(-1);
      if (last?.role !== 'assistant') return current;
      return [...current.slice(0, -1), { ...last, incomplete: true }];
    });
  }

  stopGeneration(): void {
    if (this.isSending()) this.markAssistantIncomplete();
    this.finishActiveRequest?.();

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
    this.prompts.set([]);

    this.stopGeneration();

    this.messages.set([]);

    this.conversationId.set(null);

    this.activeConversationId.set(null);

    this.debugInfo.set(null);

    this.error.set(null);
  }

  loadConversation(
    conversation:
      ConversationDetails
  ): void {
    this.prompts.set([]);

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

    this.debugInfo.set(null);

    this.error.set(null);
  }
}
