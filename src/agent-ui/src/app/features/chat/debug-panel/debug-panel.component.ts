// debug-panel.component.ts

import {
  ChangeDetectionStrategy,
  Component,
  input,
  output
} from '@angular/core';

import { MatIconModule } from '@angular/material/icon';
import { DatePipe } from '@angular/common';
import { ChatRequestMetric } from '../chat-request-metric';
import { MatDividerModule } from '@angular/material/divider';

import { ChatDebugInfo } from 'src/app/core/models/chat.models';

@Component({
  selector: 'app-debug-panel',
  standalone: true,
  imports: [
    DatePipe,
    MatIconModule,
    MatDividerModule
  ],
  templateUrl: './debug-panel.component.html',
  styleUrl: './debug-panel.component.scss',
  changeDetection:
    ChangeDetectionStrategy.OnPush
})
export class DebugPanelComponent {
  readonly history = input<readonly ChatRequestMetric[]>([]);

  duration(value: number | null): string {
    return value === null ? '—' : `${Math.round(value)} ms`;
  }

  statusLabel(status: ChatRequestMetric['status']): string {
    return { completed: 'הושלמה', cancelled: 'נעצרה', failed: 'נכשלה' }[status];
  }

  readonly debug =
    input<ChatDebugInfo | null>(null);

  readonly sourceCount = input(0);

  readonly collapsed = input(false);
  readonly collapsedChange = output<boolean>();

  toggleCollapsed(): void {
    this.collapsedChange.emit(!this.collapsed());
  }
}
