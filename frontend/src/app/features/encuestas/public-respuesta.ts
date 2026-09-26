import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { EncuestaApi } from '../../core/encuesta-api';
import {
  PreguntaResponse,
  PublicEncuestaResponse,
  RespuestaItemRequest,
} from '../../core/encuesta.models';
import { FriendlyError, toFriendlyError } from '../../core/problem-details';
import { ErrorAlert } from '../../shared/error-alert';

const storageKey = (token: string) => `encuesta.participante.${token}`;

/** Token aleatorio por navegador y encuesta; el backend solo guarda su hash (respuesta única). */
const participanteToken = (token: string): string => {
  try {
    const existing = localStorage.getItem(storageKey(token));
    if (existing) return existing;
    const created = crypto.randomUUID();
    localStorage.setItem(storageKey(token), created);
    return created;
  } catch {
    return crypto.randomUUID();
  }
};

/** Pantalla pública (sin sesión) para responder una encuesta desde su enlace. */
@Component({
  selector: 'app-public-respuesta',
  imports: [DatePipe, ErrorAlert],
  templateUrl: './public-respuesta.html',
})
export class PublicRespuesta {
  private readonly api = inject(EncuestaApi);

  /** Parámetro de ruta :token (withComponentInputBinding). */
  readonly token = input.required<string>();

  protected readonly encuesta = signal<PublicEncuestaResponse | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<FriendlyError | null>(null);
  protected readonly submitError = signal<FriendlyError | null>(null);
  protected readonly sending = signal(false);
  protected readonly sent = signal(false);
  protected readonly showMissing = signal(false);

  /** Valores por pregunta (id de opción, texto o número). */
  private readonly answers = signal<Record<string, string[]>>({});

  protected readonly missing = computed(() =>
    (this.encuesta()?.preguntas ?? []).filter(
      (p) => p.esObligatoria && (this.answers()[p.id] ?? []).length === 0,
    ),
  );

  protected readonly escala = [1, 2, 3, 4, 5];

  constructor() {
    effect(() => this.load(this.token()));
  }

  protected isSelected(p: PreguntaResponse, value: string): boolean {
    return (this.answers()[p.id] ?? []).includes(value);
  }

  protected setSingle(p: PreguntaResponse, value: string): void {
    this.answers.update((a) => ({ ...a, [p.id]: value.trim() ? [value] : [] }));
  }

  protected toggle(p: PreguntaResponse, value: string, checked: boolean): void {
    this.answers.update((a) => {
      const current = new Set(a[p.id] ?? []);
      if (checked) current.add(value);
      else current.delete(value);
      return { ...a, [p.id]: [...current] };
    });
  }

  /** Sin FormsModule no hay ngSubmit: se evita el envío nativo del formulario (recargaría la página). */
  protected onSubmit(event: Event): void {
    event.preventDefault();
    this.submit();
  }

  protected submit(): void {
    const encuesta = this.encuesta();
    if (!encuesta || this.sending()) return;
    if (this.missing().length > 0) {
      this.showMissing.set(true);
      return;
    }

    const respuestas: RespuestaItemRequest[] = encuesta.preguntas
      .map((p) => ({ preguntaId: p.id, valores: this.answers()[p.id] ?? [] }))
      .filter((r) => r.valores.length > 0);

    this.sending.set(true);
    this.submitError.set(null);
    this.api
      .submitRespuesta(this.token(), {
        participanteToken: encuesta.respuestaUnica ? participanteToken(this.token()) : undefined,
        respuestas,
      })
      .subscribe({
        next: () => {
          this.sent.set(true);
          this.sending.set(false);
        },
        error: (err: unknown) => {
          this.submitError.set(toFriendlyError(err));
          this.sending.set(false);
        },
      });
  }

  private load(token: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.getPublic(token).subscribe({
      next: (encuesta) => {
        this.encuesta.set(encuesta);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.error.set(toFriendlyError(err));
        this.loading.set(false);
      },
    });
  }
}
