import { Component, inject, signal } from '@angular/core';
import { DatePipe, JsonPipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { ConversationTrace } from 'src/app/core/models/chat.models';
import { PromptViewerComponent } from '../prompt-viewer/prompt-viewer.component';
import { MatDialog } from '@angular/material/dialog';

@Component({
  selector: 'app-trace-viewer',
  imports: [MatDialogModule, DatePipe, JsonPipe],
  template: `
    <h2 mat-dialog-title>אבחון תשובה שמורה</h2>
    <mat-dialog-content>
      <p>זוהי צפייה בנתונים שנשמרו. הכלים אינם מורצים מחדש.</p>
      <p>{{ trace.startedAtUtc | date:'yyyy-MM-dd HH:mm:ss':'UTC' }} UTC · {{ trace.status }}</p>
      <p>זמן שרת כולל: {{ trace.totalMs }} ms · עד טקסט ראשון: {{ trace.firstTextMs ?? '—' }} ms</p>
      <button type="button" (click)="openPrompts()" [disabled]="!trace.prompts.length">Prompt Viewer ({{ trace.prompts.length }})</button>
      @if (!trace.prompts.length) { <p>לא נשמרו צילומי קלט לבקשה זו. הצילום תלוי בהגדרת השרת.</p> }
      <h3>קריאות כלים ({{ trace.toolCalls.length }})</h3>
      @for (call of trace.toolCalls; track $index) {
        <details><summary>{{ $index + 1 }} · {{ call.name }}</summary>
          <h4>Arguments</h4><pre dir="auto">{{ call.arguments }}</pre>
          <h4>Result sent to context</h4><pre dir="auto">{{ call.result }}</pre>
          @if (call.debug) { <h4>Search debug &amp; analytics</h4><pre dir="ltr">{{ call.debug | json }}</pre> }
        </details>
      }
      <details><summary>מקורות ({{ trace.sources.length }})</summary><pre dir="ltr">{{ trace.sources | json }}</pre></details>
      @if (trace.debug) { <details><summary>נתוני החיפוש האחרון</summary><pre dir="ltr">{{ trace.debug | json }}</pre></details> }
    </mat-dialog-content>
    <mat-dialog-actions align="end"><button type="button" mat-dialog-close>סגור</button></mat-dialog-actions>
  `,
  styles: [`pre { white-space: pre-wrap; overflow-wrap: anywhere; max-height: 400px; overflow: auto; } details { margin-block: 12px; } summary, button { cursor: pointer; }`]
})
export class TraceViewerComponent {
  readonly trace = inject<ConversationTrace>(MAT_DIALOG_DATA);
  private readonly dialog = inject(MatDialog);
  readonly prompts = signal(this.trace.prompts);
  openPrompts(): void {
    this.dialog.open(PromptViewerComponent, { data: this.prompts, width: '1000px', maxWidth: '95vw', maxHeight: '90vh' });
  }
}
