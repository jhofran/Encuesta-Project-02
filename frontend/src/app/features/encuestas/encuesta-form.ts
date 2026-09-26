import { Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormArray,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { EncuestaApi } from '../../core/encuesta-api';
import {
  CreateEncuestaRequest,
  LIMITES,
  TIPOS_PREGUNTA,
  TipoPregunta,
  tipoConOpciones,
} from '../../core/encuesta.models';
import { FriendlyError, toFriendlyError } from '../../core/problem-details';
import { ErrorAlert } from '../../shared/error-alert';

const opcionControl = (value = '') =>
  new FormControl(value, {
    nonNullable: true,
    validators: [Validators.required, Validators.maxLength(LIMITES.textoOpcion)],
  });

/** RN-05: las preguntas de opción única/múltiple requieren al menos 2 opciones. */
const opcionesMinimas = (group: AbstractControl): ValidationErrors | null => {
  const tipo = group.get('tipo')?.value as TipoPregunta;
  const opciones = group.get('opciones') as FormArray;
  return tipoConOpciones(tipo) && opciones.length < LIMITES.minOpciones
    ? { opcionesMinimas: true }
    : null;
};

const preguntaGroup = () =>
  new FormGroup(
    {
      texto: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(LIMITES.textoPregunta)],
      }),
      tipo: new FormControl<TipoPregunta>('TextoLibre', { nonNullable: true }),
      esObligatoria: new FormControl(false, { nonNullable: true }),
      opciones: new FormArray<FormControl<string>>([]),
    },
    { validators: opcionesMinimas },
  );

type PreguntaGroup = ReturnType<typeof preguntaGroup>;

@Component({
  selector: 'app-encuesta-form',
  imports: [ReactiveFormsModule, RouterLink, ErrorAlert],
  templateUrl: './encuesta-form.html',
})
export class EncuestaForm {
  private readonly api = inject(EncuestaApi);
  private readonly router = inject(Router);

  protected readonly limites = LIMITES;
  protected readonly tipos = TIPOS_PREGUNTA;
  protected readonly conOpciones = tipoConOpciones;

  protected readonly saving = signal(false);
  protected readonly error = signal<FriendlyError | null>(null);

  protected readonly form = new FormGroup({
    titulo: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(LIMITES.titulo)],
    }),
    descripcion: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(LIMITES.descripcion)],
    }),
    preguntas: new FormArray<PreguntaGroup>([], [Validators.maxLength(LIMITES.preguntas)]),
  });

  protected get preguntas(): FormArray<PreguntaGroup> {
    return this.form.controls.preguntas;
  }

  protected opcionesDe(pregunta: PreguntaGroup): FormArray<FormControl<string>> {
    return pregunta.controls.opciones;
  }

  protected addPregunta(): void {
    if (this.preguntas.length < LIMITES.preguntas) this.preguntas.push(preguntaGroup());
  }

  protected removePregunta(index: number): void {
    this.preguntas.removeAt(index);
  }

  protected addOpcion(pregunta: PreguntaGroup): void {
    const opciones = this.opcionesDe(pregunta);
    if (opciones.length < LIMITES.opciones) opciones.push(opcionControl());
  }

  protected removeOpcion(pregunta: PreguntaGroup, index: number): void {
    this.opcionesDe(pregunta).removeAt(index);
  }

  /** Al cambiar el tipo, las opciones se reinician: solo los tipos de opción las admiten. */
  protected onTipoChange(pregunta: PreguntaGroup): void {
    const opciones = this.opcionesDe(pregunta);
    opciones.clear();
    if (tipoConOpciones(pregunta.controls.tipo.value)) {
      for (let i = 0; i < LIMITES.minOpciones; i++) opciones.push(opcionControl());
    }
    pregunta.updateValueAndValidity();
  }

  protected submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid || this.saving()) return;

    this.saving.set(true);
    this.error.set(null);

    this.api.create(this.toRequest()).subscribe({
      next: (encuesta) => void this.router.navigate(['/encuestas', encuesta.id]),
      error: (err: unknown) => {
        this.error.set(toFriendlyError(err));
        this.saving.set(false);
      },
    });
  }

  private toRequest(): CreateEncuestaRequest {
    const { titulo, descripcion, preguntas } = this.form.getRawValue();
    return {
      titulo: titulo.trim(),
      ...(descripcion.trim() && { descripcion: descripcion.trim() }),
      preguntas: preguntas.map((p) => ({
        texto: p.texto.trim(),
        tipo: p.tipo,
        esObligatoria: p.esObligatoria,
        ...(tipoConOpciones(p.tipo) && { opciones: p.opciones.map((o) => o.trim()) }),
      })),
    };
  }
}
