import {
  ChangeDetectionStrategy,
  Component,
  inject
} from '@angular/core';

import { ChatStateService } from './chat-state.service';
import { ComposerComponent } from './components/composer/composer.component';
import { DocumentUploadComponent } from "../documents/document-upload/document-upload.component";
import { MessageListComponent } from './message-list/message-list.component';

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [
    MessageListComponent,
    ComposerComponent,
    DocumentUploadComponent
],
  templateUrl: './chat.component.html',
  styleUrl: './chat.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ChatComponent {

  readonly state =
    inject(ChatStateService);

  sendMessage(message: string): void {
    this.state.send(message);
  }

  clearConversation(): void {
    this.state.clear();
  }
}