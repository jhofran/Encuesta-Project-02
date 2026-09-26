/** Tipos derivados del contrato docs/api/Encuesta-v1.yaml. */

export type TipoPregunta = 'OpcionUnica' | 'OpcionMultiple' | 'TextoLibre' | 'Escala1a5';
export type EstadoEncuesta = 'Borrador' | 'Publicada' | 'Cerrada';
export type SlaStatus = 'SinPlazo' | 'Vigente' | 'PorVencer' | 'Vencido';

export const TIPOS_PREGUNTA: readonly { value: TipoPregunta; label: string }[] = [
  { value: 'OpcionUnica', label: 'Opción única' },
  { value: 'OpcionMultiple', label: 'Opción múltiple' },
  { value: 'TextoLibre', label: 'Texto libre' },
  { value: 'Escala1a5', label: 'Escala (1–5)' },
];

/** Límites de validación del contrato (CreateEncuestaRequest) y del dominio. */
export const LIMITES = {
  titulo: 200,
  descripcion: 2000,
  preguntas: 100,
  textoPregunta: 500,
  opciones: 20,
  textoOpcion: 200,
  minOpciones: 2,
} as const;

export const tipoConOpciones = (tipo: TipoPregunta): boolean =>
  tipo === 'OpcionUnica' || tipo === 'OpcionMultiple';

export interface CreatePreguntaRequest {
  texto: string;
  tipo: TipoPregunta;
  esObligatoria: boolean;
  opciones?: string[];
}

export interface CreateEncuestaRequest {
  titulo: string;
  descripcion?: string;
  preguntas?: CreatePreguntaRequest[];
}

export interface AssignEncuestaRequest {
  responsableId: string;
}

export interface OpcionResponse {
  id: string;
  texto: string;
  orden: number;
}

export interface PreguntaResponse {
  id: string;
  texto: string;
  tipo: TipoPregunta;
  esObligatoria: boolean;
  orden: number;
  opciones?: OpcionResponse[];
}

export interface EncuestaResponse {
  id: string;
  creadorId: string;
  titulo: string;
  descripcion?: string | null;
  estado: EstadoEncuesta;
  slaStatus: SlaStatus;
  esAnonima: boolean;
  respuestaUnica: boolean;
  token?: string | null;
  fechaLimite?: string | null;
  preguntas: PreguntaResponse[];
  creadaEn: string;
}

/** RFC 7807 (ErrorResponse). */
export interface ProblemDetails {
  type: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
}
