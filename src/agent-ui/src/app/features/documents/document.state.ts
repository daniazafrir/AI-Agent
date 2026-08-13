import {
  Injectable,
  signal
} from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class DocumentState {

  readonly uploading =
    signal(false);

  readonly progress =
    signal(0);

  readonly success =
    signal<string | null>(null);

  readonly error =
    signal<string | null>(null);

}