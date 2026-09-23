import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
  OnInit,
  signal
} from '@angular/core';

import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';

import { ConversationApiService } from '../../features/conversations/conversation-api.service';
import { ChatStateService } from '../../features/chat/chat-state.service';
import { ConversationSummary } from 'src/app/core/models/chat.models';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [
    MatButtonModule,
    MatDividerModule,
    MatIconModule
  ],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SidebarComponent implements OnInit {

  private readonly api =
    inject(ConversationApiService);

   readonly chatState =
    inject(ChatStateService);

  readonly conversations =
    signal<ConversationSummary[]>([]);

  readonly loading =
    signal(false);

  readonly error =
    signal<string | null>(null);

  constructor() {
    effect(() => {
      // The server emits the ID only after the user message has been saved.
      const id = this.chatState.activeConversationId();
      this.chatState.requestHistory();
      if (id) this.loadConversations();
    });
  }

  ngOnInit(): void {
    this.loadConversations();
  }

  loadConversations(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.getAll().subscribe({
      next: conversations => {
        this.conversations.set(conversations);
        this.loading.set(false);
      },

      error: error => {
        console.error(
          'Failed to load conversations.',
          error
        );

        this.error.set(
          'Failed to load conversations.'
        );

        this.loading.set(false);
      }
    });
  }

  openConversation(id: string): void {
    this.api.getById(id).subscribe({
      next: conversation => {
        this.chatState.loadConversation(
          conversation
        );
      },

      error: error => {
        console.error(
          'Failed to load conversation.',
          error
        );
      }
    });
  }

  newConversation(): void {
    this.chatState.clear();
  }
}
