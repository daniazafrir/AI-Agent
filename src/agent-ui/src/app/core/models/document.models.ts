export interface DocumentUploadResponse {
  success: boolean;
  documentId: string;
  fileName: string;
  chunkCount: number;
  message: string;
}
