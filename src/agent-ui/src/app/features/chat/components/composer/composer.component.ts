import {
  ChangeDetectionStrategy,
  Component,
  input,
  output
} from '@angular/core';

import {
  FormControl,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';

import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-composer',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './composer.component.html',
  styleUrl: './composer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ComposerComponent {

  readonly loading = input(false);

  readonly send = output<string>();

  readonly stop = output<void>();

  readonly message = new FormControl('', {
    nonNullable: true,
    validators: [
      Validators.required,
      Validators.maxLength(4000)
    ]
  });

  submit(): void {

    const value =
      this.message.value.trim();

    if (
      !value ||
      this.message.invalid ||
      this.loading()
    ) {
      return;
    }

    this.send.emit(value);

    this.message.reset();
  }

  stopGeneration(): void {
    this.stop.emit();
  }

  onKeyDown(
    event: KeyboardEvent
  ): void {

    if (
      event.key === 'Enter' &&
      !event.shiftKey
    ) {
      event.preventDefault();

      this.submit();
    }
  }
}