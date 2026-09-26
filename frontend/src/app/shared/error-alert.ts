import { Component, input } from '@angular/core';
import { FriendlyError } from '../core/problem-details';

@Component({
  selector: 'app-error-alert',
  template: `
    @if (error(); as e) {
      <div class="alert alert-error" role="alert">
        <strong>{{ e.title }}</strong>
        @if (e.detail) {
          <p>{{ e.detail }}</p>
        }
        @if (e.fieldErrors.length) {
          <ul>
            @for (message of e.fieldErrors; track message) {
              <li>{{ message }}</li>
            }
          </ul>
        }
      </div>
    }
  `,
})
export class ErrorAlert {
  readonly error = input<FriendlyError | null>(null);
}
