import { Component, inject } from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { isGuid } from './guid';

@Component({
  selector: 'app-home',
  imports: [FormsModule, ReactiveFormsModule, RouterLink],
  template: `
    <section class="card">
      <h1>Encuestas</h1>
      <p class="muted">Crea una encuesta nueva o abre una existente por su identificador.</p>
      <a routerLink="/encuestas/nueva" class="btn btn-primary">Crear encuesta</a>
    </section>

    <section class="card">
      <h2>Abrir encuesta</h2>
      <form (ngSubmit)="open()" class="row">
        <div class="grow">
          <label for="id">Identificador (GUID)</label>
          <input id="id" type="text" [formControl]="id" placeholder="00000000-0000-0000-0000-000000000000" />
          @if (id.touched && id.invalid) {
            <p class="field-error" role="alert">Ingresa un GUID válido.</p>
          }
        </div>
        <button type="submit" class="btn">Abrir</button>
      </form>
    </section>
  `,
})
export class Home {
  private readonly router = inject(Router);

  protected readonly id = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, (c) => (isGuid(c.value) ? null : { guid: true })],
  });

  protected open(): void {
    this.id.markAsTouched();
    if (this.id.invalid) return;
    void this.router.navigate(['/encuestas', this.id.value.trim()]);
  }
}
