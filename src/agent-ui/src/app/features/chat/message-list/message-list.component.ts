import {
  AfterViewChecked,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  ViewChild,
  inject
} from '@angular/core';
import { ChatStateService } from '../chat-state.service';
import { MatDialog } from '@angular/material/dialog';
import { KnowledgeSource } from 'src/app/core/models/chat.models';
import { ConversationTrace } from 'src/app/core/models/chat.models';
import { TraceViewerComponent } from '../trace-viewer/trace-viewer.component';
import { SourcePreviewDialogComponent } from '../source-preview-dialog-component/source-preview-dialog-component';


@Component({
  selector: 'app-message-list',
  standalone: true,
  templateUrl: './message-list.component.html',
  styleUrl: './message-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MessageListComponent
  implements AfterViewChecked {
  private readonly dialog =
    inject(MatDialog);
  openTrace(trace: ConversationTrace): void {
    this.dialog.open(TraceViewerComponent, { data: trace, width: '1000px', maxWidth: '95vw', maxHeight: '90vh' });
  }

openSource(
    source: KnowledgeSource, answer: string
): void {

    this.dialog.open(
        SourcePreviewDialogComponent,
        {
            width: '800px',
            maxWidth: '95vw',
            maxHeight: '80vh',
            data: { ...source, answer }
        }
    );
}
  readonly state =
    inject(ChatStateService);

  @ViewChild('scrollContainer')
  private scrollContainer?:
    ElementRef<HTMLDivElement>;

  ngAfterViewChecked(): void {
    this.scrollToBottom();
  }

  private scrollToBottom(): void {
    const element =
      this.scrollContainer?.nativeElement;

    if (!element) {
      return;
    }

    element.scrollTop =
      element.scrollHeight;
  }
}
