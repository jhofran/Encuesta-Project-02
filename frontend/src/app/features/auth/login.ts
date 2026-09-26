import { Component, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, looksLikeJwt } from '../../core/auth';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
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
  `,
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly token = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required],
  });
  protected readonly invalid = signal(false);

  protected submit(): void {
    if (this.token.invalid) return;
    if (!looksLikeJwt(this.token.value)) {
      this.invalid.set(true);
      return;
    }
    this.invalid.set(false);
    this.auth.login(this.token.value);
    void this.router.navigate(['/']);
  }
}
