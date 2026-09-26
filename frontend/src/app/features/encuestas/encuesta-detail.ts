import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth';
import { EncuestaApi } from '../../core/encuesta-api';
import { EncuestaResponse, SlaStatus, TIPOS_PREGUNTA } from '../../core/encuesta.models';
import { FriendlyError, toFriendlyError } from '../../core/problem-details';
import { ErrorAlert } from '../../shared/error-alert';
import { isGuid } from './guid';

const SLA_LABELS: Record<SlaStatus, string> = {
  SinPlazo: 'Sin plazo',
  Vigente: 'Vigente',
  PorVencer: 'Por vencer',
  Vencido: 'Vencido',
};

@Component({
  selector: 'app-encuesta-detail',
  imports: [ReactiveFormsModule, RouterLink, DatePipe, ErrorAlert],
  templateUrl: './encuesta-detail.html',
})
export class EncuestaDetail {
  private readonly api = inject(EncuestaApi);
  private readonly auth = inject(AuthService);

  /** Parámetro de ruta :id (withComponentInputBinding). */
  readonly id = input.required<string>();

  protected readonly encuesta = signal<EncuestaResponse | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<FriendlyError | null>(null);
  protected readonly assigning = signal(false);
  protected readonly assigned = signal(false);
  protected readonly assignError = signal<FriendlyError | null>(null);

  protected readonly responsable = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, (c) => (isGuid(c.value) ? null : { guid: true })],
  });

  protected readonly esCerrada = computed(() => this.encuesta()?.estado === 'Cerrada');
  protected readonly miId = this.auth.userId;

  constructor() {
    effect(() => this.load(this.id()));
  }

  protected slaLabel(status: SlaStatus): string {
    return SLA_LABELS[status];
  }

  protected tipoLabel(tipo: string): string {
    return TIPOS_PREGUNTA.find((t) => t.value === tipo)?.label ?? tipo;
  }

  protected asignarAMi(): void {
    const me = this.miId();
    if (me) this.responsable.setValue(me);
  }

  protected assign(): void {
    this.responsable.markAsTouched();
    if (this.responsable.invalid || this.assigning()) return;

    this.assigning.set(true);
    this.assignError.set(null);
    this.assigned.set(false);

    this.api.assign(this.id(), this.responsable.value.trim()).subscribe({
      next: (encuesta) => {
        this.encuesta.set(encuesta);
        this.assigned.set(true);
        this.assigning.set(false);
        this.responsable.reset();
      },
      error: (err: unknown) => {
        this.assignError.set(toFriendlyError(err));
        this.assigning.set(false);
      },
    });
  }

  private load(id: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.getById(id).subscribe({
      next: (encuesta) => {
        this.encuesta.set(encuesta);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.encuesta.set(null);
        this.error.set(toFriendlyError(err));
        this.loading.set(false);
      },
    });
  }
}
