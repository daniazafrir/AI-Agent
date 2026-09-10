import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal
} from '@angular/core';

import { ChatStateService } from './chat-state.service';
import { ComposerComponent } from './components/composer/composer.component';
import { DocumentUploadComponent } from "../documents/document-upload/document-upload.component";
import { MessageListComponent } from './message-list/message-list.component';
import { KnowledgeBaseComponent } from "../documents/knowledge-base/knowledge-base.component";
import { DebugPanelComponent } from "./debug-panel/debug-panel.component";

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [
    MessageListComponent,
    ComposerComponent,
    DocumentUploadComponent,
    KnowledgeBaseComponent,
    DebugPanelComponent
],
  templateUrl: './chat.component.html',
  styleUrl: './chat.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ChatComponent {

  readonly state =
    inject(ChatStateService);

  readonly isDebugCollapsed = signal(false);

  setDebugCollapsed(collapsed: boolean): void {
    this.isDebugCollapsed.set(collapsed);
  }

  sendMessage(message: string): void {
    this.state.send(message);
  }

  clearConversation(): void {
    this.state.clear();
  }

  stopGeneration(): void {
    this.state.stopGeneration();
  }

  getToolIcon(
    tool: string | null
  ): string {
    switch (tool) {
      case 'search_knowledge':
        return '🔎';

      case 'calculate':
        return '🧮';

      case 'get_current_time':
        return '🕒';

      case 'index_document':
        return '📄';

      default:
        return '🤖';
    }
  }

  getToolText(
    tool: string | null
  ): string {
    switch (tool) {
      case 'search_knowledge':
        return 'מחפש במאגר הידע...';

      case 'calculate':
        return 'מבצע חישוב...';

      case 'get_current_time':
        return 'בודק את השעה...';

      case 'index_document':
        return 'מאנדקס מסמך...';

      default:
        return 'מבצע פעולה...';
    }
  }
}
