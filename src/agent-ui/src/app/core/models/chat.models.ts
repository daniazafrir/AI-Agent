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
  analytics?: {
    returnedChunks: number; includedChunks: number; omittedChunks: number; contextCharacters: number;
    chunks: { documentId: string | null; documentName: string | null; chunkIndex: number | null;
      contentCharacters: number; included: boolean; reason: string }[];
  } | null;
  toolName: string;
  query: string;

  rawVectorResults: number;
  relevantVectorResults: number;
  vectorResults: number;

  keywordResults: number;
  mergedResults: number;

  minimumVectorScore: number;

  searchTimeMs: number;
  embeddingTimeMs?: number | null;
  vectorSearchTimeMs?: number | null;
  keywordSearchTimeMs?: number | null;
  rankingTimeMs?: number | null;
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
