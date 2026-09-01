import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatSidenavModule } from '@angular/material/sidenav';
import { SidebarComponent } from "../sidebar/sidebar.component";
import { ChatComponent } from 'src/app/features/chat/chat.component';
import { ConversationListComponent } from "src/app/features/conversations/conversation-list.component";
import { KnowledgeBaseComponent } from "src/app/features/documents/knowledge-base/knowledge-base.component";
@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    MatSidenavModule,
    ChatComponent,
    ConversationListComponent,
    KnowledgeBaseComponent
],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ShellComponent {
}