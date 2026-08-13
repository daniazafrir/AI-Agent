import {
  ChangeDetectionStrategy,
  Component
} from '@angular/core';

import { MatSidenavModule } from '@angular/material/sidenav';

import { ChatComponent } from '../../features/chat/chat.component';
import { ConversationListComponent } from '../../features/conversations/conversation-list.component';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [
    MatSidenavModule,
    ChatComponent,
    ConversationListComponent
  ],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ShellComponent {
}