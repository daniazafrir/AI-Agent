// src/app/core/models/chat.models.ts

export interface ChatRequest {
  message: string;
  conversationId?: string | null;
}

export interface PromptSnapshot {
  round: number;
  model: string;
  capturedAtUtc: string;
  messagesJson: string;
  optionsJson: string;
}

export interface KnowledgeSource {
  documentId: string;
  documentName: string;
  chunkIndex: number;
  score: number;
}

export interface ChatDebugInfo {
  toolName: string;
  query: string;

  rawVectorResults: number;
  relevantVectorResults: number;
  vectorResults: number;

  keywordResults: number;
  mergedResults: number;

  minimumVectorScore: number;

  searchTimeMs: number;
  matches?: { documentName: string; chunkIndex: number; score: number; searchEngine: string }[];
}
export interface ChatStreamEvent {
  type: string;
  content?: string | null;
  toolName?: string | null;
  usedTools?: string[] | null;
  conversationId?: string | null;
  sources?: KnowledgeSource[] | null;
  debug?: ChatDebugInfo | null;
  prompt?: PromptSnapshot | null;
}

export interface ChatMessage {
  incomplete?: boolean;
  role: 'user' | 'assistant';
  content: string;
  usedTools?: string[];
  sources?: KnowledgeSource[];
}
export interface ConversationSummary {

    id: string;

    title: string;

    lastUpdated: string;

}

export interface KnowledgeChunk {

    documentId: string;

    chunkIndex: number;

    content: string;
}
