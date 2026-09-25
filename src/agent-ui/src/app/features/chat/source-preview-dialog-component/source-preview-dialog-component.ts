import { Component, DestroyRef, Inject, inject, signal, computed } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MAT_DIALOG_DATA, MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { KnowledgeChunk, KnowledgeSource } from 'src/app/core/models/chat.models';
import { DocumentApiService } from '../../documents/document-api.service';
import { highlightParts } from './source-highlight';

@Component({
  selector: 'app-source-preview-dialog-component',
  imports: [MatDialogTitle, MatDialogContent, MatDialogActions, MatDialogClose, MatButtonModule],
  templateUrl: './source-preview-dialog-component.html',
  styleUrl: './source-preview-dialog-component.scss',
})
export class SourcePreviewDialogComponent {
  private readonly destroyRef = inject(DestroyRef);
  readonly chunk = signal<KnowledgeChunk | undefined>(undefined);
  readonly loading = signal(false);
  readonly error = signal('');
  readonly search = signal('');
  readonly parts = computed(() => highlightParts(this.chunk()?.content ?? '', this.search()));
  readonly matchCount = computed(() => this.parts().filter(part => part.highlight).length);

  constructor(
    @Inject(MAT_DIALOG_DATA) public source: KnowledgeSource & { answer?: string },
    private api: DocumentApiService) {
    // Literal numeric matches work across languages; they are not citation alignment.
    this.search.set([...new Set(source.answer?.match(/\d+(?:\.\d+)?/g) ?? [])].join(' '));
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.chunk.set(undefined);
    this.api.getChunk(this.source.documentId, this.source.chunkIndex)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: chunk => { this.chunk.set(chunk); this.loading.set(false); },
        error: error => {
          this.error.set(error.status === 404
            ? 'הקטע אינו זמין. ייתכן שהמסמך נמחק או אונדקס מחדש.'
            : 'לא ניתן לטעון את הקטע. אפשר לנסות שוב.');
          this.loading.set(false);
        }
      });
  }
}
