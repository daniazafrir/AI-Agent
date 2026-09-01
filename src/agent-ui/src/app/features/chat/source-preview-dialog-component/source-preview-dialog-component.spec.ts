import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SourcePreviewDialogComponent } from './source-preview-dialog-component';

describe('SourcePreviewDialogComponent', () => {
  let component: SourcePreviewDialogComponent;
  let fixture: ComponentFixture<SourcePreviewDialogComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SourcePreviewDialogComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SourcePreviewDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
