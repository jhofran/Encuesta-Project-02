import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { EncuestaForm } from './encuesta-form';

describe('EncuestaForm', () => {
  // Se accede a miembros protegidos del componente (solo en pruebas).
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  let component: any;
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EncuestaForm],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    const fixture = TestBed.createComponent(EncuestaForm);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('HU-01: rechaza un título vacío y no llama a la API', () => {
    component.submit();

    expect(component.form.controls.titulo.hasError('required')).toBe(true);
    http.expectNone('/api/v1/Encuesta');
  });

  it('HU-01: una pregunta de opción única con menos de 2 opciones es inválida (RN-05)', () => {
    component.addPregunta();
    const pregunta = component.preguntas.at(0);
    pregunta.controls.tipo.setValue('OpcionUnica');
    component.onTipoChange(pregunta);
    component.removeOpcion(pregunta, 0);
    pregunta.updateValueAndValidity();

    expect(pregunta.hasError('opcionesMinimas')).toBe(true);
  });

  it('HU-01: al cambiar a un tipo de opción se crean 2 opciones; con texto libre se limpian', () => {
    component.addPregunta();
    const pregunta = component.preguntas.at(0);

    pregunta.controls.tipo.setValue('OpcionMultiple');
    component.onTipoChange(pregunta);
    expect(component.opcionesDe(pregunta).length).toBe(2);

    pregunta.controls.tipo.setValue('TextoLibre');
    component.onTipoChange(pregunta);
    expect(component.opcionesDe(pregunta).length).toBe(0);
  });

  it('HU-01: envía la encuesta válida al backend', () => {
    component.form.controls.titulo.setValue('  Satisfacción del cliente ');
    component.addPregunta();
    const pregunta = component.preguntas.at(0);
    pregunta.controls.texto.setValue('¿Qué mejorarías?');

    component.submit();

    const req = http.expectOne('/api/v1/Encuesta');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      titulo: 'Satisfacción del cliente',
      preguntas: [{ texto: '¿Qué mejorarías?', tipo: 'TextoLibre', esObligatoria: false }],
    });
  });

  it('muestra el detalle RFC 7807 cuando la API responde 422', () => {
    component.form.controls.titulo.setValue('Titulo');
    component.submit();

    http.expectOne('/api/v1/Encuesta').flush(
      { type: 'x', title: 'Regla de negocio incumplida', status: 422, detail: 'Se requieren al menos 2 opciones' },
      { status: 422, statusText: 'Unprocessable Entity' },
    );

    expect(component.error().title).toBe('Regla de negocio incumplida');
    expect(component.saving()).toBe(false);
  });
});
