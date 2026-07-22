import { HttpClient, HttpEvent } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { DocumentUploadResponse } from '../models/document.models';

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
}
