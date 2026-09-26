import { HttpClient } from '@angular/common/http';
import { Component, inject, isDevMode, signal } from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, looksLikeJwt } from '../../core/auth';
import { FriendlyError, toFriendlyError } from '../../core/problem-details';
import { ErrorAlert } from '../../shared/error-alert';

@Component({
  selector: 'app-login',
  imports: [FormsModule, ReactiveFormsModule, ErrorAlert],
  template: `
    <section class="card narrow">
      <h1>Ingresar</h1>
      <p class="muted">
        Pega el token JWT emitido por el proveedor de identidad (OIDC). La API valida su firma y
        vencimiento.
      </p>
      <form (ngSubmit)="submit()">
        <label for="token">Token de acceso</label>
        <textarea id="token" rows="5" [formControl]="token" autocomplete="off"></textarea>
        @if (invalid()) {
          <p class="field-error" role="alert">El token no tiene el formato de un JWT.</p>
        }
        <button type="submit" class="btn btn-primary" [disabled]="token.invalid">Continuar</button>
      </form>
    </section>

    @if (devMode) {
      <section class="card narrow">
        <h2>Desarrollo local</h2>
        <p class="muted">
          Solo disponible con el backend en <code>Development</code>: genera un token firmado con la
          clave de desarrollo.
        </p>
        <div class="row">
          <button type="button" class="btn" (click)="devLogin(false)">Entrar como usuario</button>
          <button type="button" class="btn" (click)="devLogin(true)">Entrar como admin</button>
        </div>
        <app-error-alert [error]="devError()" />
      </section>
    }
  `,
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);

  protected readonly devMode = isDevMode();
  protected readonly token = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required],
  });
  protected readonly invalid = signal(false);
  protected readonly devError = signal<FriendlyError | null>(null);

  protected submit(): void {
    if (this.token.invalid) return;
    if (!looksLikeJwt(this.token.value)) {
      this.invalid.set(true);
      return;
    }
    this.invalid.set(false);
    this.enter(this.token.value);
  }

  protected devLogin(admin: boolean): void {
    this.devError.set(null);
    this.http.post<{ token: string }>('/dev/token', { admin }).subscribe({
      next: ({ token }) => this.enter(token),
      error: (err: unknown) => this.devError.set(toFriendlyError(err)),
    });
  }

  private enter(token: string): void {
    this.auth.login(token);
    void this.router.navigate(['/']);
  }
}
