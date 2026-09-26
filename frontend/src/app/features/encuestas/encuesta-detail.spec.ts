import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { EncuestaResponse } from '../../core/encuesta.models';
import { EncuestaDetail } from './encuesta-detail';

const ID = '11111111-1111-1111-1111-111111111111';

const encuesta = (over: Partial<EncuestaResponse> = {}): EncuestaResponse => ({
  id: ID,
  creadorId: '22222222-2222-2222-2222-222222222222',
  titulo: 'Satisfacción',
  descripcion: null,
  estado: 'Borrador',
  slaStatus: 'SinPlazo',
  esAnonima: false,
  respuestaUnica: false,
  token: null,
  fechaLimite: null,
  preguntas: [{ id: 'p1', texto: '¿Cómo calificas?', tipo: 'Escala1a5', esObligatoria: true, orden: 0, opciones: [] }],
  creadaEn: '2026-09-26T00:00:00Z',
  ...over,
});

describe('EncuestaDetail', () => {
  let fixture: ComponentFixture<EncuestaDetail>;
  let http: HttpTestingController;
  let el: HTMLElement;

  const open = async (body: EncuestaResponse) => {
    fixture = TestBed.createComponent(EncuestaDetail);
    fixture.componentRef.setInput('id', ID);
    http = TestBed.inject(HttpTestingController);
    el = fixture.nativeElement as HTMLElement;
    await fixture.whenStable();
    http.expectOne(`/api/v1/Encuesta/${ID}`).flush(body);
    await fixture.whenStable();
  };

  const buttonByText = (text: string) =>
    [...el.querySelectorAll('button, a.btn')].find((b) => b.textContent?.includes(text)) as HTMLElement;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  afterEach(() => http.verify());

  // HU-02 Escenario: Publicar una encuesta válida
  it('HU-02: publica con la fecha límite elegida y muestra el enlace público', async () => {
    await open(encuesta());
    (el.querySelector('input[formControlName=esAnonima]') as HTMLInputElement).click();

    buttonByText('Publicar encuesta').click();

    const req = http.expectOne(`/api/v1/Encuesta/${ID}/publish`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body.esAnonima).toBe(true);
    expect(req.request.body.respuestaUnica).toBe(false);
    expect(new Date(req.request.body.fechaLimite).getTime()).toBeGreaterThan(Date.now());
    req.flush(encuesta({ estado: 'Publicada', slaStatus: 'Vigente', token: 'ABC123', esAnonima: true }));
    await fixture.whenStable();

    expect((el.querySelector('#link') as HTMLInputElement).value).toBe(`${location.origin}/e/ABC123`);
    expect(buttonByText('Cerrar encuesta')).toBeTruthy();
  });

  // HU-02 Escenario: No publicar una encuesta sin preguntas
  it('HU-02: sin preguntas no ofrece publicar', async () => {
    await open(encuesta({ preguntas: [] }));

    expect(el.textContent).toContain('Agrega al menos una pregunta');
    expect(buttonByText('Publicar encuesta')).toBeUndefined();
  });

  // HU-02: errores del backend
  it('HU-02: muestra el error de la API al publicar', async () => {
    await open(encuesta());

    buttonByText('Publicar encuesta').click();
    http.expectOne(`/api/v1/Encuesta/${ID}/publish`).flush(
      { type: 'x', title: 'Regla de negocio incumplida', status: 422, detail: 'La fecha límite debe ser futura' },
      { status: 422, statusText: 'Unprocessable Entity' },
    );
    await fixture.whenStable();

    expect(el.textContent).toContain('La fecha límite debe ser futura');
  });

  // HU-04 Escenario: Cierre manual
  it('HU-04: cierra la encuesta tras confirmar', async () => {
    vi.spyOn(globalThis, 'confirm').mockReturnValue(true);
    await open(encuesta({ estado: 'Publicada', token: 'ABC123', slaStatus: 'Vigente' }));

    buttonByText('Cerrar encuesta').click();

    http.expectOne(`/api/v1/Encuesta/${ID}/close`).flush(encuesta({ estado: 'Cerrada', token: 'ABC123' }));
    await fixture.whenStable();

    expect(el.textContent).toContain('Encuesta cerrada');
  });

  it('HU-04: no cierra si el usuario cancela la confirmación', async () => {
    vi.spyOn(globalThis, 'confirm').mockReturnValue(false);
    await open(encuesta({ estado: 'Publicada', token: 'ABC123', slaStatus: 'Vigente' }));

    buttonByText('Cerrar encuesta').click();

    http.expectNone(`/api/v1/Encuesta/${ID}/close`);
  });

  // Contrato PUT /assign: el formulario dispara el envío
  it('asigna el responsable al enviar el formulario', async () => {
    await open(encuesta());
    const input = el.querySelector('#responsable') as HTMLInputElement;
    input.value = '33333333-3333-3333-3333-333333333333';
    input.dispatchEvent(new Event('input'));

    buttonByText('Asignar').click();

    const req = http.expectOne(`/api/v1/Encuesta/${ID}/assign`);
    expect(req.request.body).toEqual({ responsableId: '33333333-3333-3333-3333-333333333333' });
    req.flush(encuesta({ creadorId: '33333333-3333-3333-3333-333333333333' }));
  });
});
