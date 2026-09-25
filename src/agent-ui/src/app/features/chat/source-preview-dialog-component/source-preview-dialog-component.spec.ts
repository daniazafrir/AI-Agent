import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SourcePreviewDialogComponent } from './source-preview-dialog-component';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';
import { DocumentApiService } from '../../documents/document-api.service';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

describe('SourcePreviewDialogComponent', () => {
  let component: SourcePreviewDialogComponent;
  let fixture: ComponentFixture<SourcePreviewDialogComponent>;
  const getChunk = vi.fn();

  beforeEach(async () => {
    getChunk.mockReset().mockReturnValue(of({ documentId: 'doc', chunkIndex: 0, content: 'Employees receive 19 days. <script>bad()</script>' }));
    await TestBed.configureTestingModule({
      imports: [SourcePreviewDialogComponent],
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: { documentId: 'doc', documentName: 'Handbook', chunkIndex: 0, score: 0.03, answer: '19 ימי חופשה' } },
        { provide: DocumentApiService, useValue: { getChunk } }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SourcePreviewDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads the selected source and renders text safely with numeric highlights', () => {
    expect(getChunk).toHaveBeenCalledWith('doc', 0);
    expect(fixture.nativeElement.querySelector('mark').textContent).toBe('19');
    expect(fixture.nativeElement.querySelector('script')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('<script>bad()</script>');
  });

  it('clears highlights without removing source text', () => {
    component.search.set('');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('mark')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Employees receive 19 days.');
  });

  it('ends loading after a missing source and allows retry', () => {
    getChunk.mockReturnValueOnce(throwError(() => ({ status: 404 })));
    component.load();
    fixture.detectChanges();
    expect(component.loading()).toBe(false);
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('אינו זמין');
    component.load();
    fixture.detectChanges();
    expect(component.error()).toBe('');
    expect(component.chunk()?.content).toContain('19');
  });
});
