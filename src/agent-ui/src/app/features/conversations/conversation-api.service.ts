import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ChatMessage, ConversationSummary } from 'src/app/core/models/chat.models';
import { environment } from '../../../environments/environment';


export interface ConversationDetails {
  conversationId: string;
  messages: ChatMessage[];
}

@Injectable({
  providedIn: 'root'
})
export class ConversationApiService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<ConversationSummary[]> {
    return this.http.get<ConversationSummary[]>(
      `${environment.apiUrl}/api/conversations`
    );
  }

  getById(
    conversationId: string
  ): Observable<ConversationDetails> {
    return this.http.get<ConversationDetails>(
      `${environment.apiUrl}/api/conversations/${conversationId}`
    );
  }
}