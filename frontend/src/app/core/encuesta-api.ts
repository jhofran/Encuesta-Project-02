import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import {
  AssignEncuestaRequest,
  CreateEncuestaRequest,
  EncuestaResponse,
} from './encuesta.models';

@Injectable({ providedIn: 'root' })
export class EncuestaApi {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/v1/Encuesta';

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
}
