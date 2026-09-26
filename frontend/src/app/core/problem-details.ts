import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from './encuesta.models';

export interface FriendlyError {
  title: string;
  detail?: string;
  fieldErrors: string[];
}

/** Convierte un error HTTP (RFC 7807) en un mensaje apto para mostrar al usuario. */
export const toFriendlyError = (error: unknown): FriendlyError => {
  if (!(error instanceof HttpErrorResponse)) {
    return { title: 'Ocurrió un error inesperado', fieldErrors: [] };
  }
  if (error.status === 0) {
    return { title: 'No se pudo contactar con el servidor', fieldErrors: [] };
  }

  const problem = error.error as Partial<ProblemDetails> | null;
  const fieldErrors = Object.entries(problem?.errors ?? {}).flatMap(([field, messages]) =>
    messages.map((message) => `${field}: ${message}`),
  );

  return {
    title: problem?.title ?? `Error ${error.status}`,
    detail: problem?.detail,
    fieldErrors,
  };
};
