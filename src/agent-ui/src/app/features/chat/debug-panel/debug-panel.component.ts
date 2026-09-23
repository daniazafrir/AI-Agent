// debug-panel.component.ts

import {
  ChangeDetectionStrategy,
  Component,
  input,
  output
} from '@angular/core';

import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';

import { ChatDebugInfo } from 'src/app/core/models/chat.models';

@Component({
  selector: 'app-debug-panel',
  standalone: true,
  imports: [
    MatIconModule,
    MatDividerModule
  ],
  templateUrl: './debug-panel.component.html',
  styleUrl: './debug-panel.component.scss',
  changeDetection:
    ChangeDetectionStrategy.OnPush
})
export class DebugPanelComponent {

  readonly debug =
    input<ChatDebugInfo | null>(null);

  readonly sourceCount = input(0);

  readonly collapsed = input(false);
  readonly collapsedChange = output<boolean>();

  toggleCollapsed(): void {
    this.collapsedChange.emit(!this.collapsed());
  }
}
