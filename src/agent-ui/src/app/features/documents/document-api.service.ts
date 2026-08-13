import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { DocumentUploadResponse } from 'src/app/core/models/document.models';

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
