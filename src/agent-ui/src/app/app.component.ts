import { DatePipe } from '@angular/common';
import { HttpEventType } from '@angular/common/http';
import {
  Component,
  ElementRef,
  OnInit,
  ViewChild,
  inject,
  signal
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { ChatMessage } from './core/models/chat.models';
import { ChatApiService } from './core/services/chat-api.service';
import { DocumentApiService } from './core/services/document-api.service';

@Component({
  selector: 'app-root',
  imports: [FormsModule, DatePipe],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  private readonly chatApi = inject(ChatApiService);
  private readonly documentApi = inject(DocumentApiService);

  @ViewChild('messageInput')
  private messageInput?: ElementRef<HTMLTextAreaElement>;

  readonly messages = signal<ChatMessage[]>([
    this.createWelcomeMessage()
  ]);
  readonly isSending = signal(false);
  readonly isUploading = signal(false);
  readonly uploadProgress = signal(0);
  readonly uploadMessage = signal<string | null>(null);
  readonly uploadError = signal<string | null>(null);

  draft = '';
  selectedFile: File | null = null;
  private conversationId: string | null = null;

  ngOnInit(): void {
    this.conversationId = localStorage.getItem('conversationId');
  }

  send(): void {
    const message = this.draft.trim();
    if (!message || this.isSending()) {
      return;
    }

    this.messages.update(items => [
      ...items,
      {
        id: crypto.randomUUID(),
        role: 'user',
        content: message,
        usedTools: [],
        createdAt: new Date()
      }
    ]);

    this.draft = '';
    this.isSending.set(true);

    this.chatApi.sendMessage({
      message,
      conversationId: this.conversationId
    })
      .pipe(finalize(() => {
        this.isSending.set(false);
        queueMicrotask(() => this.messageInput?.nativeElement.focus());
      }))
      .subscribe({
        next: response => {
          this.conversationId = response.conversationId;
          localStorage.setItem('conversationId', response.conversationId);

          this.messages.update(items => [
            ...items,
            {
              id: crypto.randomUUID(),
              role: 'assistant',
              content: response.answer,
              usedTools: response.usedTools ?? [],
              createdAt: response.createdAt
                ? new Date(response.createdAt)
                : new Date()
            }
          ]);
        },
        error: (error: unknown) => {
          this.messages.update(items => [
            ...items,
            {
              id: crypto.randomUUID(),
              role: 'assistant',
              content: this.readError(error),
              usedTools: [],
              createdAt: new Date(),
              isError: true
            }
          ]);
        }
      });
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.selectedFile = input.files?.item(0) ?? null;
    this.uploadMessage.set(null);
    this.uploadError.set(null);
    this.uploadProgress.set(0);
  }

  uploadDocument(): void {
    if (!this.selectedFile || this.isUploading()) {
      return;
    }

    if (!this.selectedFile.name.toLowerCase().endsWith('.txt')) {
      this.uploadError.set('בשלב זה אפשר להעלות קובצי TXT בלבד.');
      return;
    }

    this.isUploading.set(true);
    this.uploadProgress.set(0);
    this.uploadMessage.set(null);
    this.uploadError.set(null);

    this.documentApi.upload(this.selectedFile)
      .pipe(finalize(() => this.isUploading.set(false)))
      .subscribe({
        next: event => {
          if (event.type === HttpEventType.UploadProgress) {
            const total = event.total ?? this.selectedFile?.size ?? 0;
            const progress = total > 0
              ? Math.round((event.loaded / total) * 100)
              : 0;

            this.uploadProgress.set(progress);
          }

          if (event.type === HttpEventType.Response && event.body) {
            this.uploadProgress.set(100);
            this.uploadMessage.set(
              `המסמך ${event.body.fileName} נוסף בהצלחה (${event.body.chunkCount} מקטעים).`
            );
            this.selectedFile = null;
          }
        },
        error: (error: unknown) => {
          this.uploadError.set(this.readError(error));
        }
      });
  }

  handleKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
  }

  clearConversation(): void {
    this.conversationId = null;
    localStorage.removeItem('conversationId');
    this.messages.set([this.createWelcomeMessage()]);
    queueMicrotask(() => this.messageInput?.nativeElement.focus());
  }

  private createWelcomeMessage(): ChatMessage {
    return {
      id: crypto.randomUUID(),
      role: 'assistant',
      content: 'שלום! אפשר לשאול אותי שאלה, לבצע חישוב, לבדוק שעה או לחפש במסמכים שהעלית.',
      usedTools: [],
      createdAt: new Date()
    };
  }

  private readError(error: unknown): string {
    if (typeof error === 'object' && error !== null && 'error' in error) {
      const payload = (error as { error?: unknown }).error;

      if (typeof payload === 'object' && payload !== null && 'detail' in payload) {
        return String((payload as { detail: unknown }).detail);
      }

      if (typeof payload === 'object' && payload !== null && 'error' in payload) {
        return String((payload as { error: unknown }).error);
      }

      if (typeof payload === 'string' && payload.trim()) {
        return payload;
      }
    }

    return 'לא ניתן להתחבר לשרת. ודאי ש־Agent.Api ו־Mcp.Tools.Server פועלים.';
  }
}
