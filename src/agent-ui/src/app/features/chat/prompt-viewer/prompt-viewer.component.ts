import { ChangeDetectionStrategy, Component, computed, inject, signal, Signal } from '@angular/core';
import { DatePipe, JsonPipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { PromptSnapshot } from 'src/app/core/models/chat.models';

@Component({
  selector: 'app-prompt-viewer',
  standalone: true,
  imports: [DatePipe, JsonPipe, MatDialogModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>Prompt Viewer</h2>
    <mat-dialog-content>
      <p dir="rtl">צילום הקלט שהוכן לקריאה למודל, לפי סבב. תוצאות כלי החיפוש מופיעות בהודעות tool.
        הנתונים שייכים לתשובה שנבחרה ואינם אישור שהספק קיבל את הקריאה.</p>
      <nav aria-label="Model rounds">
        @for (round of rounds(); track round.round; let i = $index) {
          <button type="button" [attr.aria-pressed]="selected() === i" (click)="selected.set(i)">
            סבב {{ round.round }}
          </button>
        }
      </nav>
      @if (current(); as round) {
        <p dir="ltr">{{ round.model }} · {{ round.capturedAtUtc | date:'yyyy-MM-dd HH:mm:ss':'UTC' }} UTC</p>
        <h3>Messages</h3>
        @for (message of messages(); track $index) {
          <details open>
            <summary>{{ $index + 1 }} · {{ message.role }}</summary>
            <pre dir="ltr">{{ message | json }}</pre>
          </details>
        }
        <details>
          <summary>Tools &amp; request options</summary>
          <pre dir="ltr">{{ options() | json }}</pre>
        </details>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end"><button type="button" mat-dialog-close>סגירה</button></mat-dialog-actions>
  `,
  styles: [`
    :host { display: block; }
    nav { display: flex; gap: 8px; flex-wrap: wrap; }
    button { padding: 8px 16px; cursor: pointer; border: 1px solid #ccc; border-radius: 8px; background: white; }
    button[aria-pressed="true"] { background: #e3e8ff; border-color: #5365bc; }
    details { margin: 12px 0; border: 1px solid #ddd; border-radius: 8px; padding: 12px; }
    summary { cursor: pointer; font-weight: 600; }
    pre { white-space: pre-wrap; overflow-wrap: anywhere; max-height: 360px; overflow: auto; font-size: 13px; }
  `]
})
export class PromptViewerComponent {
  readonly rounds = inject<Signal<readonly PromptSnapshot[]>>(MAT_DIALOG_DATA);
  readonly selected = signal(0);
  readonly current = computed(() => this.rounds()[this.selected()] ?? this.rounds()[0]);
  readonly messages = computed(() => JSON.parse(this.current()?.messagesJson ?? '[]') as { role: string }[]);
  readonly options = computed(() => JSON.parse(this.current()?.optionsJson ?? '{}') as unknown);
}
