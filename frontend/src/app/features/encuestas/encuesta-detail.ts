import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
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

const DIA_MS = 24 * 60 * 60 * 1000;

/** Valor para <input type="datetime-local"> (hora local, sin zona). */
const toLocalInput = (date: Date): string => {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
};

@Component({
  selector: 'app-encuesta-detail',
  imports: [FormsModule, ReactiveFormsModule, RouterLink, DatePipe, ErrorAlert],
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
  protected readonly publishing = signal(false);
  protected readonly closing = signal(false);
  protected readonly stateError = signal<FriendlyError | null>(null);
  protected readonly copied = signal(false);

  protected readonly responsable = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, (c) => (isGuid(c.value) ? null : { guid: true })],
  });

  protected readonly publishForm = new FormGroup({
    fechaLimite: new FormControl(toLocalInput(new Date(Date.now() + 7 * DIA_MS)), {
      nonNullable: true,
      validators: [Validators.required],
    }),
    esAnonima: new FormControl(false, { nonNullable: true }),
    respuestaUnica: new FormControl(false, { nonNullable: true }),
  });

  protected readonly esCerrada = computed(() => this.encuesta()?.estado === 'Cerrada');
  protected readonly miId = this.auth.userId;

  /** Enlace público que se comparte con los participantes (solo con la encuesta publicada). */
  protected readonly publicLink = computed(() => {
    const token = this.encuesta()?.token;
    return token ? `${location.origin}/e/${token}` : null;
  });

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

  protected publish(): void {
    this.publishForm.markAllAsTouched();
    if (this.publishForm.invalid || this.publishing()) return;

    const { fechaLimite, esAnonima, respuestaUnica } = this.publishForm.getRawValue();
    this.publishing.set(true);
    this.stateError.set(null);

    this.api
      .publish(this.id(), { fechaLimite: new Date(fechaLimite).toISOString(), esAnonima, respuestaUnica })
      .subscribe({
        next: (encuesta) => {
          this.encuesta.set(encuesta);
          this.publishing.set(false);
        },
        error: (err: unknown) => {
          this.stateError.set(toFriendlyError(err));
          this.publishing.set(false);
        },
      });
  }

  protected close(): void {
    if (this.closing() || !confirm('¿Cerrar la encuesta? Dejará de aceptar respuestas y no puede reabrirse.')) return;

    this.closing.set(true);
    this.stateError.set(null);
    this.api.close(this.id()).subscribe({
      next: (encuesta) => {
        this.encuesta.set(encuesta);
        this.closing.set(false);
      },
      error: (err: unknown) => {
        this.stateError.set(toFriendlyError(err));
        this.closing.set(false);
      },
    });
  }

  protected async copyLink(): Promise<void> {
    const link = this.publicLink();
    if (!link) return;
    try {
      await navigator.clipboard.writeText(link);
      this.copied.set(true);
    } catch {
      this.copied.set(false);
    }
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
