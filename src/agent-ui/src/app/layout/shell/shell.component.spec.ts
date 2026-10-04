import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ShellComponent } from './shell.component';
import { BreakpointObserver } from '@angular/cdk/layout';
import { BehaviorSubject } from 'rxjs';
import { By } from '@angular/platform-browser';
import { MatSidenav } from '@angular/material/sidenav';
import { ConversationListComponent } from '../../features/conversations/conversation-list.component';

describe('ShellComponent', () => {
  let component: ShellComponent;
  let fixture: ComponentFixture<ShellComponent>;
  let viewport: BehaviorSubject<{ matches: boolean }>;

  beforeEach(async () => {
    viewport = new BehaviorSubject<{ matches: boolean }>({ matches: false });
    await TestBed.configureTestingModule({
      imports: [ShellComponent], providers: [provideHttpClient(), provideHttpClientTesting(),
        { provide: BreakpointObserver, useValue: {
          observe: () => viewport.asObservable(), isMatched: () => viewport.value.matches
        } }]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ShellComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    const http = TestBed.inject(HttpTestingController);
    for (const request of http.match(req => req.method === 'GET' && /\/api\/(documents|conversations)$/.test(req.url))) {
      request.flush([]);
    }
    http.verify();
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('switches from a desktop sidebar to a closed mobile drawer', () => {
    const drawer = fixture.debugElement.query(By.directive(MatSidenav)).componentInstance as MatSidenav;
    expect(drawer.mode).toBe('side');
    expect(drawer.opened).toBe(true);
    viewport.next({ matches: true });
    fixture.detectChanges();
    expect(drawer.mode).toBe('over');
    expect(drawer.opened).toBe(false);
    expect(fixture.nativeElement.querySelector('.mobile-navigation button')).toBeTruthy();
    viewport.next({ matches: false });
    fixture.detectChanges();
    expect(drawer.mode).toBe('side');
    expect(drawer.opened).toBe(true);
  });

  it('opens mobile conversations and closes after selection', () => {
    viewport.next({ matches: true });
    fixture.detectChanges();
    const drawer = fixture.debugElement.query(By.directive(MatSidenav)).componentInstance as MatSidenav;
    fixture.nativeElement.querySelector('.mobile-navigation button').click();
    fixture.detectChanges();
    expect(drawer.opened).toBe(true);
    const list = fixture.debugElement.query(By.directive(ConversationListComponent)).componentInstance as ConversationListComponent;
    list.conversationSelected.emit();
    fixture.detectChanges();
    expect(drawer.opened).toBe(false);
  });
});
