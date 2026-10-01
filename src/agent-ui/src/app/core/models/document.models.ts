export interface DocumentUploadResponse {
  success: boolean;
  documentId: string;
  fileName: string;
  chunkCount: number;
  message: string;
}

export interface RagDocument {
    indexingDetails?: { indexedAtUtc: string; embeddingModel: string | null; embeddingDimensions: number | null;
      chunkSize: number | null; chunkOverlap: number | null; chunkingMode: string } | null;

    id: string;

    fileName: string;

    sizeBytes: number;

    chunkCount: number;

    createdAtUtc: string;

    contentHash: string;
}
