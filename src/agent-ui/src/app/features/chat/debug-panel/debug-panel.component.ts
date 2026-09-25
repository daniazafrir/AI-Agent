// debug-panel.component.ts

import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  inject,
  output
} from '@angular/core';

import { MatIconModule } from '@angular/material/icon';
import { DatePipe, DecimalPipe } from '@angular/common';
import { ChatRequestMetric } from '../chat-request-metric';
import { MatDividerModule } from '@angular/material/divider';

import { ChatDebugInfo, PromptSnapshot } from 'src/app/core/models/chat.models';
import { MatDialog } from '@angular/material/dialog';
import { PromptViewerComponent } from '../prompt-viewer/prompt-viewer.component';

@Component({
  selector: 'app-debug-panel',
  standalone: true,
  imports: [
    DatePipe,
    DecimalPipe,
    MatIconModule,
    MatDividerModule
  ],
  templateUrl: './debug-panel.component.html',
  styleUrl: './debug-panel.component.scss',
  changeDetection:
    ChangeDetectionStrategy.OnPush
})
export class DebugPanelComponent {
  private readonly dialog = inject(MatDialog);
  readonly prompts = input<readonly PromptSnapshot[]>([]);

  openPrompts(): void {
    this.dialog.open(PromptViewerComponent, {
      data: this.prompts, width: '1000px', maxWidth: '95vw', maxHeight: '90vh'
    });
  }
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

  readonly stages = computed(() => {
    const info = this.debug();
    const rows = [
      { label: 'Embedding', ms: info?.embeddingTimeMs },
      { label: 'Vector search', ms: info?.vectorSearchTimeMs },
      { label: 'Keyword search', ms: info?.keywordSearchTimeMs },
      { label: 'Ranking', ms: info?.rankingTimeMs }
    ].map(row => ({ ...row, ms: typeof row.ms === 'number' && Number.isFinite(row.ms) && row.ms >= 0 ? row.ms : null }));
    const maximum = Math.max(1, ...rows.map(row => row.ms ?? 0));
    return rows.map(row => ({ ...row, width: (row.ms ?? 0) / maximum * 100 }));
  });

  readonly collapsed = input(false);
  readonly collapsedChange = output<boolean>();

  toggleCollapsed(): void {
    this.collapsedChange.emit(!this.collapsed());
  }
}
