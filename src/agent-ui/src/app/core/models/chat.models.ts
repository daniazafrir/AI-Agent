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
  sources?: KnowledgeSource[];

}

export interface KnowledgeSource {
    documentId: string;
    documentName: string;
    chunkIndex: number;
    score: number;
}
export interface ConversationSummary {

    id: string;

    title: string;

    lastUpdated: string;

}

export interface ChatStreamEvent {

  type: string;

  content?: string;

  toolName?: string;

  usedTools?: string[];

  conversationId?: string;

    sources?: KnowledgeSource[] | null;

  

}

export interface KnowledgeChunk {

    documentId: string;

    chunkIndex: number;

    content: string;
}