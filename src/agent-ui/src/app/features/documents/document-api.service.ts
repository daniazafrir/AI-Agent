import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { DocumentUploadResponse, RagDocument } from 'src/app/core/models/document.models';
import { KnowledgeChunk } from 'src/app/core/models/chat.models';

@Injectable({
    providedIn: 'root'
})
export class DocumentApiService {
    private readonly http = inject(HttpClient);

    upload(file: File): Observable<HttpEvent<DocumentUploadResponse>> {
        const formData = new FormData();
        formData.append('file', file, file.name);

        return this.http.post<DocumentUploadResponse>(
            `${environment.apiUrl}/api/documents`,
            formData,
            {
                observe: 'events',
                reportProgress: true
            }
        );
    }

    getDocuments(): Observable<RagDocument[]> {

        return this.http.get<RagDocument[]>(
            `${environment.apiUrl}/api/documents`
        );
    }

    deleteDocument(
        id: string
    ): Observable<void> {

        return this.http.delete<void>(
            `${environment.apiUrl}/api/documents/${id}`
        );
    }

     getChunk(
        documentId: string,
        chunkIndex: number) {

        return this.http.get<KnowledgeChunk>(
            `${environment.apiUrl}/api/documents/${documentId}/chunks/${chunkIndex}`);
    }
}

