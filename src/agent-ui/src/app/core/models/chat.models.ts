export interface ChatRequest {
  message: string;
  conversationId: string | null;
}

export interface ChatResponse {
  conversationId: string;
  answer: string;
  usedTools: string[];
  createdAt: string;
}

export type ChatRole = 'user' | 'assistant';

export interface ChatMessage {
  id: string;
  role: ChatRole;
  content: string;
  usedTools: string[];
  createdAt: Date;
  isError?: boolean;
}
