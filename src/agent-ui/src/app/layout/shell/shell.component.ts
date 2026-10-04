import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { BreakpointObserver } from '@angular/cdk/layout';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import { MatSidenavModule } from '@angular/material/sidenav';
import { ChatComponent } from 'src/app/features/chat/chat.component';
import { ConversationListComponent } from 'src/app/features/conversations/conversation-list.component';

@Component({
  selector: 'app-shell', standalone: true,
  imports: [MatSidenavModule, ChatComponent, ConversationListComponent],
  templateUrl: './shell.component.html', styleUrl: './shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ShellComponent {
  private readonly breakpoints = inject(BreakpointObserver);
  readonly mobile = toSignal(
    this.breakpoints.observe('(max-width: 768px)').pipe(map(result => result.matches)),
    { initialValue: this.breakpoints.isMatched('(max-width: 768px)') }
  );
}
