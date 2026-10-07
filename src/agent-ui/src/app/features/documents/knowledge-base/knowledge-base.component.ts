import {
  Component,
  DestroyRef,
  inject,
  OnInit,
  signal
} from '@angular/core';

import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import { KnowledgeEventsService } from './knowledge-events.service';

import { RagDocument } from 'src/app/core/models/document.models';
import { DocumentApiService } from '../document-api.service';

@Component({
  selector: 'app-knowledge-base',
  standalone: true,
  imports: [DatePipe],
  templateUrl: './knowledge-base.component.html',
  styleUrl: './knowledge-base.component.css'
})
export class KnowledgeBaseComponent
  implements OnInit {
  private readonly destroyRef = inject(DestroyRef);
  private readonly events = inject(KnowledgeEventsService);
  readonly busy = signal<string | null>(null);
  readonly notice = signal('');
  readonly error = signal('');
  readonly feedbackDocumentId = signal<string | null>(null);

  readonly api =
    inject(DocumentApiService);

  readonly documents =
    signal<RagDocument[]>([]);

  constructor() {

    this.events.refresh$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => this.load());

}
  ngOnInit(): void {
    this.load();
  }
  
  

  load(): void {
    this.api
      .getDocuments()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: documents => {
          this.documents.set(documents);
        },

        error: error => {
          this.feedbackDocumentId.set(null);
          this.error.set('לא ניתן לטעון את רשימת המסמכים.');
          console.error(
            'Failed to load documents.',
            error
          );
        }
      });
  }

  delete(
    document: RagDocument
  ): void {
    if (this.busy()) return;
    this.busy.set(document.id);
    this.api
      .deleteDocument(document.id)
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.busy.set(null)))
      .subscribe({
        next: () => {
          this.documents.update(
            current =>
              current.filter(
                x => x.id !== document.id
              )
          );
        },

        error: error => {
          console.error(
            'Failed to delete document.',
            error
          );
        }
      });
  }
  reindex(document: RagDocument): void {
    if (this.busy()) return;
    this.busy.set(document.id);
    this.error.set('');
    this.notice.set('');
    this.feedbackDocumentId.set(document.id);
    this.api.reindex(document.id).pipe(
      takeUntilDestroyed(this.destroyRef), finalize(() => this.busy.set(null))
    ).subscribe({
      next: result => {
        if (!result.success) { this.error.set('האינדוקס לא הושלם.'); return; }
        this.notice.set('האינדוקס מחדש הושלם.');
        this.load();
      },
      error: () => this.error.set('האינדוקס מחדש נכשל. ייתכן שהאינדקס הווקטורי חלקי; הקטעים השמורים נשמרו וניתן לנסות שוב.')
    });
  }
}
