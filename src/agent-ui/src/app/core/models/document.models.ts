export interface DocumentUploadResponse {
  success: boolean;
  documentId: string;
  fileName: string;
  chunkCount: number;
  message: string;
}

export interface RagDocument {

    id: string;

    fileName: string;

    sizeBytes: number;

    chunkCount: number;

    createdAtUtc: string;

    contentHash: string;
}

