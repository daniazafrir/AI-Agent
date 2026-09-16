import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal
} from '@angular/core';

import {
  HttpEventType,
  HttpResponse
} from '@angular/common/http';

import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';

import { DocumentApiService } from '../document-api.service';
import { DocumentState } from '../document.state';
import { KnowledgeEventsService } from '../knowledge-base/knowledge-events.service';

@Component({
  selector: 'app-document-upload',
  standalone: true,
  imports: [
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule
  ],
  templateUrl: './document-upload.component.html',
  styleUrl: './document-upload.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DocumentUploadComponent {

  private readonly documentApi =
    inject(DocumentApiService);

  private readonly events =
    inject(KnowledgeEventsService);
  
  readonly state =
    inject(DocumentState);

  readonly selectedFile =
    signal<File | null>(null);

  readonly isDragging = signal(false);
  private dragDepth = 0;

  selectFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!this.state.uploading()) this.acceptFiles(input.files);
    input.value = '';
  }

  onDragEnter(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    if (this.state.uploading() || !event.dataTransfer?.types.includes('Files')) return;
    this.dragDepth++;
    this.isDragging.set(true);
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    if (event.dataTransfer) {
      event.dataTransfer.dropEffect = this.state.uploading() ? 'none' : 'copy';
    }
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragDepth = Math.max(0, this.dragDepth - 1);
    if (this.dragDepth === 0) this.isDragging.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.dragDepth = 0;
    this.isDragging.set(false);
    if (!this.state.uploading()) this.acceptFiles(event.dataTransfer?.files ?? null);
  }

  private acceptFiles(files: FileList | null): void {
    if (!files?.length) return;
    this.state.success.set(null);
    this.state.error.set(null);
    this.state.progress.set(0);
    this.selectedFile.set(null);

    if (files.length !== 1) {
      this.state.error.set('Please select one file at a time.');
      return;
    }
    const file = files[0];
    if (!this.isValidFile(file)) {
      this.state.error.set('Only TXT or PDF files up to 2MB are supported.');
      return;
    }
    this.selectedFile.set(file);
  }
  upload(): void {

    const file =
      this.selectedFile();

    if (!file || this.state.uploading()) {
      return;
    }

    this.state.uploading.set(true);
    this.state.progress.set(0);
    this.state.success.set(null);
    this.state.error.set(null);

    this.documentApi
      .upload(file)
      .subscribe({

        next: event => {

          if (
            event.type ===
            HttpEventType.UploadProgress
          ) {

            const total =
              event.total ?? file.size;

            const progress =
              total > 0
                ? Math.round(
                  100 * event.loaded / total
                )
                : 0;

            this.state.progress.set(
              progress
            );

            return;
          }

          if (
            event instanceof HttpResponse
          ) {

            this.state.progress.set(100);

            this.state.success.set(
              `${file.name} uploaded successfully.`
            );

            this.state.uploading.set(false);

            this.selectedFile.set(null);

            this.events.refresh();
          }

        },

        error: error => {

          console.error(
            'Document upload failed.',
            error
          );

          this.state.error.set(
            'Failed to upload the document.'
          );

          this.state.uploading.set(false);
          this.state.progress.set(0);
        }
      });
  }

  clearSelection(): void {
    this.selectedFile.set(null);
    this.state.progress.set(0);
    this.state.success.set(null);
    this.state.error.set(null);
  }

  private isValidFile(
  file: File
): boolean {

  const maxSize =
    2 * 1024 * 1024;

  const fileName =
    file.name.toLowerCase();

  const isTxt =
    file.type === 'text/plain'
    || fileName.endsWith('.txt');

  const isPdf =
    file.type === 'application/pdf'
    || fileName.endsWith('.pdf');

  return (
    isTxt ||
    isPdf
  ) &&
    file.size <= maxSize;
}
}
