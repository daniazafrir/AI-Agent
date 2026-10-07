import { TestBed } from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { vi } from 'vitest';
import { KnowledgeBaseComponent } from './knowledge-base.component';
import { DocumentApiService } from '../document-api.service';
import { RagDocument } from '../../../core/models/document.models';

describe('KnowledgeBaseComponent reindex feedback', () => {
  it('shows failure in the affected card, clears busy and preserves successful metadata; allows retry', async () => {
    const document: RagDocument = {
      id: 'doc', fileName: 'Handbook.pdf', chunkCount: 5, sizeBytes: 100,
      createdAtUtc: '2026-09-01T00:00:00Z', contentHash: 'hash',
      indexingDetails: { indexedAtUtc: '2026-09-26T14:28:27Z', embeddingModel: 'test',
        embeddingDimensions: 1536, chunkSize: null, chunkOverlap: null, chunkingMode: 'persisted-chunks' }
    };
    const response = new Subject<{ success: boolean }>();
    const api = { getDocuments: vi.fn(() => of([document])), reindex: vi.fn(() => response) };
    await TestBed.configureTestingModule({ imports: [KnowledgeBaseComponent],
      providers: [{ provide: DocumentApiService, useValue: api }] }).compileComponents();
    const fixture = TestBed.createComponent(KnowledgeBaseComponent);
    fixture.detectChanges();
    const button = fixture.nativeElement.querySelector('button.reindex') as HTMLButtonElement;
    button.click();
    fixture.detectChanges();
    expect(button.disabled).toBe(true);
    response.error({ status: 503 });
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.document [role="alert"]').textContent).toContain('נכשל');
    expect(button.disabled).toBe(false);
    expect(fixture.componentInstance.busy()).toBeNull();
    expect(fixture.componentInstance.documents()[0].indexingDetails?.indexedAtUtc).toBe('2026-09-26T14:28:27Z');
    expect(api.getDocuments).toHaveBeenCalledTimes(1);
    const retry = new Subject<{ success: boolean }>();
    api.reindex.mockReturnValue(retry);
    button.click();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
    retry.next({ success: true });
    retry.complete();
    fixture.detectChanges();
    expect(button.disabled).toBe(false);
    expect(fixture.nativeElement.querySelector('.document [role="status"]').textContent).toContain('הושלם');
    expect(api.getDocuments).toHaveBeenCalledTimes(2);
  });
});
