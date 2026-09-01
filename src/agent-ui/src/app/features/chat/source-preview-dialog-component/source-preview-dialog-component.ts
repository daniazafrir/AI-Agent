import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogContent, MatDialogActions, MatDialogClose } from '@angular/material/dialog';
import { KnowledgeChunk, KnowledgeSource } from 'src/app/core/models/chat.models';
import { DocumentApiService } from '../../documents/document-api.service';

@Component({
  selector: 'app-source-preview-dialog-component',
  imports: [MatDialogContent, MatDialogActions, MatDialogClose],
  templateUrl: './source-preview-dialog-component.html',
  styleUrl: './source-preview-dialog-component.scss',
})
export class SourcePreviewDialogComponent {
    chunk?: KnowledgeChunk;

    loading = true;

    constructor(

        @Inject(MAT_DIALOG_DATA)
        public source: KnowledgeSource,

        private api: DocumentApiService) {

        this.api
            .getChunk(
                source.documentId,
                source.chunkIndex)
            .subscribe(x => {

                this.chunk = x;

                this.loading = false;
            });
    }
    
}
