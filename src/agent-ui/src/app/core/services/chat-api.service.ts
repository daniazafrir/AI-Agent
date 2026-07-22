import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ChatRequest, ChatResponse } from '../models/chat.models';

@Injectable({
  providedIn: 'root'
})
export class ChatApiService {
  private http = inject(HttpClient);

  sendMessage(request: ChatRequest) {
    return this.http.post<ChatResponse>(
      `${environment.apiUrl}/api/chat`,
      request
    );
  }
}