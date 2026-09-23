export interface ChatRequestMetric {
  id: number;
  question: string;
  startedAt: number;
  firstTextMs: number | null;
  totalMs: number;
  searchMs: number | null;
  sourceCount: number | null;
  status: 'completed' | 'cancelled' | 'failed';
}
