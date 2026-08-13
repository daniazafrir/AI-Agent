import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { ChatMessage } from 'src/app/core/models/chat.models';

@Component({
  selector: 'app-message',
  standalone: true,
  templateUrl: './message.component.html',
  styleUrl: './message.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MessageComponent {

  readonly message =
    input.required<ChatMessage>();

}