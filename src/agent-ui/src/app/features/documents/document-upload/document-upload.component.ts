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

  readonly state =
    inject(DocumentState);

  readonly selectedFile =
    signal<File | null>(null);

  selectFile(event: Event): void {

    const input =
      event.target as HTMLInputElement;

    const file =
      input.files?.[0] ?? null;

    this.state.success.set(null);
    this.state.error.set(null);
    this.state.progress.set(0);

    if (!file) {
      this.selectedFile.set(null);
      return;
    }

    if (!this.isValidFile(file)) {

      this.state.error.set(
        'Only TXT files up to 2MB are supported.'
      );

      this.selectedFile.set(null);

      input.value = '';

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

    const isTxt =
      file.type === 'text/plain'
      || file.name
        .toLowerCase()
        .endsWith('.txt');

    return isTxt
      && file.size <= maxSize;
  }
}