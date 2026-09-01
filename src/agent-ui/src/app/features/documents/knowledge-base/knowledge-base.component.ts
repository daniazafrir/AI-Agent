import {
  Component,
  effect,
  inject,
  OnInit,
  signal
} from '@angular/core';

import { DatePipe } from '@angular/common';

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

  readonly api =
    inject(DocumentApiService);

  readonly documents =
    signal<RagDocument[]>([]);

  constructor() {

    effect(() => {

        this.load();

    });

}
  ngOnInit(): void {
    throw new Error('Method not implemented.');
  }
  
  

  load(): void {
    this.api
      .getDocuments()
      .subscribe({
        next: documents => {
          this.documents.set(documents);
        },

        error: error => {
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
    this.api
      .deleteDocument(document.id)
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
}