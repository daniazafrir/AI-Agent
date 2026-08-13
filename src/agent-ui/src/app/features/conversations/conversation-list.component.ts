import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal
} from '@angular/core';

import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ChatStateService } from '../chat/chat-state.service';
import { ConversationApiService } from './conversation-api.service';
import { ConversationSummary } from 'src/app/core/models/chat.models';


@Component({
  selector: 'app-conversation-list',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './conversation-list.component.html',
  styleUrl: './conversation-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConversationListComponent
  implements OnInit {

  private readonly api =
    inject(ConversationApiService);

  readonly state =
    inject(ChatStateService);

  readonly conversations =
    signal<ConversationSummary[]>([]);

  readonly loading =
    signal(false);

  readonly error =
    signal<string | null>(null);

  ngOnInit(): void {
    this.loadConversations();
  }

  loadConversations(): void {

    this.loading.set(true);
    this.error.set(null);

    this.api.getAll().subscribe({

      next: conversations => {

        this.conversations.set(
          conversations
        );

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

  openConversation(
    id: string
  ): void {

    this.api
      .getById(id)
      .subscribe({

        next: conversation => {

          this.state.loadConversation(
            conversation
          );

        },

        error: error => {

          console.error(
            'Failed to open conversation.',
            error
          );

        }

      });

  }

  newConversation(): void {

    this.state.clear();

    this.loadConversations();

  }

}