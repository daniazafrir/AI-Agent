export interface ChatRequest {
  message: string;
  conversationId: string | null;
}

export interface ChatResponse {
  conversationId: string;
  answer: string;
  usedTools: string[];
}

export type ChatRole = 'user' | 'assistant';

export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  usedTools?: string[];
}

export interface ConversationSummary {

    id: string;

    title: string;

    lastUpdated: string;

}