import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PublicEncuestaResponse } from '../../core/encuesta.models';
import { PublicRespuesta } from './public-respuesta';

const encuesta = (over: Partial<PublicEncuestaResponse> = {}): PublicEncuestaResponse => ({
  titulo: 'Satisfacción',
  descripcion: null,
  esAnonima: false,
  respuestaUnica: false,
  fechaLimite: null,
  preguntas: [
    { id: 'p1', texto: '¿Cómo calificas?', tipo: 'Escala1a5', esObligatoria: true, orden: 0, opciones: [] },
    {
      id: 'p2', texto: '¿Color?', tipo: 'OpcionUnica', esObligatoria: false, orden: 1,
      opciones: [{ id: 'o1', texto: 'Rojo', orden: 0 }, { id: 'o2', texto: 'Azul', orden: 1 }],
    },
    {
      id: 'p3', texto: '¿Frutas?', tipo: 'OpcionMultiple', esObligatoria: false, orden: 2,
      opciones: [{ id: 'o3', texto: 'Pera', orden: 0 }, { id: 'o4', texto: 'Uva', orden: 1 }],
    },
  ],
  ...over,
});

describe('PublicRespuesta', () => {
  let fixture: ComponentFixture<PublicRespuesta>;
  let http: HttpTestingController;
  let el: HTMLElement;

  const open = async (token = 'tok') => {
    fixture = TestBed.createComponent(PublicRespuesta);
    fixture.componentRef.setInput('token', token);
    http = TestBed.inject(HttpTestingController);
    el = fixture.nativeElement as HTMLElement;
    await fixture.whenStable();
  };

  const flushGet = async (body: PublicEncuestaResponse) => {
    http.expectOne('/api/v1/public/tok').flush(body);
    await fixture.whenStable();
  };

  const click = async (selector: string, index = 0) => {
    (el.querySelectorAll(selector)[index] as HTMLElement).click();
    await fixture.whenStable();
  };

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    localStorage.clear();
  });

  afterEach(() => http.verify());

  // HU-03 Escenario: Enviar respuestas válidas
  it('HU-03: muestra las preguntas y envía las respuestas válidas', async () => {
    await open();
    await flushGet(encuesta());

    expect(el.querySelector('h1')?.textContent).toContain('Satisfacción');
    expect(el.querySelectorAll('fieldset').length).toBe(3);

    await click('input[type=radio][name="p-p1"]', 3); // escala = 4
    await click('input[type=radio][name="p-p2"]', 1); // Azul
    await click('input[type=checkbox]', 0);           // Pera
    await click('input[type=checkbox]', 1);           // Uva
    await click('button[type=submit]');

    const req = http.expectOne('/api/v1/public/tok/respuestas');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      participanteToken: undefined,
      respuestas: [
        { preguntaId: 'p1', valores: ['4'] },
        { preguntaId: 'p2', valores: ['o2'] },
        { preguntaId: 'p3', valores: ['o3', 'o4'] },
      ],
    });
    req.flush({ id: 'r1', enviadaEn: '2026-09-26T00:00:00Z' });
    await fixture.whenStable();

    expect(el.textContent).toContain('¡Gracias por participar!');
  });

  // HU-03 Escenario: Faltan preguntas obligatorias
  it('HU-03: no envía si falta una pregunta obligatoria', async () => {
    await open();
    await flushGet(encuesta());

    await click('button[type=submit]');

    http.expectNone('/api/v1/public/tok/respuestas');
    expect(el.textContent).toContain('Responde las preguntas obligatorias');
  });

  // HU-03 Escenario: Respuesta duplicada en encuesta de respuesta única
  it('HU-03: en respuesta única envía el token del participante y muestra el 409', async () => {
    await open();
    await flushGet(encuesta({ respuestaUnica: true }));
    await click('input[type=radio][name="p-p1"]', 0);
    await click('button[type=submit]');

    const req = http.expectOne('/api/v1/public/tok/respuestas');
    expect(req.request.body.participanteToken).toMatch(/^[0-9a-f-]{36}$/);
    req.flush(
      { type: 'x', title: 'Conflicto de estado', status: 409, detail: 'Ya has respondido esta encuesta' },
      { status: 409, statusText: 'Conflict' },
    );
    await fixture.whenStable();

    expect(el.textContent).toContain('Ya has respondido esta encuesta');
    expect(el.textContent).not.toContain('¡Gracias por participar!');
  });

  // HU-03 Escenario: Encuesta cerrada
  it('HU-03: encuesta cerrada muestra el mensaje y no el formulario', async () => {
    await open();
    http.expectOne('/api/v1/public/tok').flush(
      { type: 'x', title: 'Encuesta no disponible', status: 410, detail: 'Esta encuesta ya no acepta respuestas' },
      { status: 410, statusText: 'Gone' },
    );
    await fixture.whenStable();

    expect(el.textContent).toContain('Esta encuesta ya no acepta respuestas');
    expect(el.querySelector('form')).toBeNull();
  });

  // HU-03 Escenario: Enlace inexistente
  it('HU-03: enlace inexistente muestra Encuesta no encontrada', async () => {
    await open();
    http.expectOne('/api/v1/public/tok').flush(
      { type: 'x', title: 'Recurso no encontrado', status: 404, detail: 'Encuesta no encontrada' },
      { status: 404, statusText: 'Not Found' },
    );
    await fixture.whenStable();

    expect(el.textContent).toContain('Encuesta no encontrada');
  });
});
