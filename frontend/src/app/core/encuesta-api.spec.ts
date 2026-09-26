import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { EncuestaApi } from './encuesta-api';
import { EncuestaResponse } from './encuesta.models';

describe('EncuestaApi', () => {
  let api: EncuestaApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(EncuestaApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('POST /api/v1/Encuesta envía el comando de creación', () => {
    const body = { titulo: 'Satisfacción', preguntas: [] };

    api.create(body).subscribe();

    const req = http.expectOne('/api/v1/Encuesta');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(body);
    req.flush({} as EncuestaResponse);
  });

  it('GET /api/v1/Encuesta/{id} consulta por identificador', () => {
    api.getById('abc').subscribe();

    const req = http.expectOne('/api/v1/Encuesta/abc');
    expect(req.request.method).toBe('GET');
    req.flush({} as EncuestaResponse);
  });

  it('PUT /api/v1/Encuesta/{id}/assign envía responsableId', () => {
    api.assign('abc', 'user-1').subscribe();

    const req = http.expectOne('/api/v1/Encuesta/abc/assign');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ responsableId: 'user-1' });
    req.flush({} as EncuestaResponse);
  });
});
