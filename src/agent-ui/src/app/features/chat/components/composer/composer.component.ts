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
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';

@Component({
  selector: 'app-composer',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule
  ],
  templateUrl: './composer.component.html',
  styleUrl: './composer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ComposerComponent {
  readonly loading = input(false);
  readonly send = output<string>();

  readonly message = new FormControl('', {
    nonNullable: true,
    validators: [
      Validators.required,
      Validators.maxLength(4000)
    ]
  });

  submit(): void {
    const value = this.message.value.trim();

    if (!value || this.message.invalid || this.loading()) {
      return;
    }

    this.send.emit(value);
    this.message.reset();
  }

  onKeyDown(event: KeyboardEvent): void {
    if (
      event.key === 'Enter' &&
      (event.ctrlKey || event.metaKey)
    ) {
      event.preventDefault();
      this.submit();
    }
  }
}