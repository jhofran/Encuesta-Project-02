import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AssignEncuestaRequest,
  CreateEncuestaRequest,
  EncuestaResponse,
  PublicEncuestaResponse,
  PublishEncuestaRequest,
  RespuestaRecibidaResponse,
  SubmitRespuestaRequest,
} from './encuesta.models';

@Injectable({ providedIn: 'root' })
export class EncuestaApi {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/v1/Encuesta';
  private readonly publicBase = '/api/v1/public';

  create(request: CreateEncuestaRequest): Observable<EncuestaResponse> {
    return this.http.post<EncuestaResponse>(this.base, request);
  }

  getById(id: string): Observable<EncuestaResponse> {
    return this.http.get<EncuestaResponse>(`${this.base}/${encodeURIComponent(id)}`);
  }

  assign(id: string, responsableId: string): Observable<EncuestaResponse> {
    const body: AssignEncuestaRequest = { responsableId };
    return this.http.put<EncuestaResponse>(`${this.base}/${encodeURIComponent(id)}/assign`, body);
  }

  publish(id: string, request: PublishEncuestaRequest): Observable<EncuestaResponse> {
    return this.http.post<EncuestaResponse>(`${this.base}/${encodeURIComponent(id)}/publish`, request);
  }

  close(id: string): Observable<EncuestaResponse> {
    return this.http.post<EncuestaResponse>(`${this.base}/${encodeURIComponent(id)}/close`, {});
  }

  getPublic(token: string): Observable<PublicEncuestaResponse> {
    return this.http.get<PublicEncuestaResponse>(`${this.publicBase}/${encodeURIComponent(token)}`);
  }

  submitRespuesta(token: string, request: SubmitRespuestaRequest): Observable<RespuestaRecibidaResponse> {
    return this.http.post<RespuestaRecibidaResponse>(
      `${this.publicBase}/${encodeURIComponent(token)}/respuestas`,
      request,
    );
  }
}
