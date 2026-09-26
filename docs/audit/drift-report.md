# Informe de auditoría de deriva (drift) — Encuesta System

| Campo | Valor |
|-------|-------|
| Fecha | 2026-09-26 |
| Alcance | `src/` (.NET 10), `tests/`, `frontend/` (Angular 22) frente a `docs/` |
| Método | Lectura del código y de los documentos; `dotnet build`, `dotnet test`, `ng build`, `ng test`, `redocly lint` y pruebas HTTP contra la API en ejecución |
| Criterio de resolución | La documentación se alineó con el **código real**. El código no se modificó |

## 1. Verificación técnica

| Comprobación | Resultado |
|--------------|-----------|
| `dotnet build` | 0 errores, 0 advertencias |
| `dotnet test` | 64 pruebas en verde |
| `ng build` / `ng test` | Compila; 25 pruebas en verde |
| `redocly lint docs/api/Encuesta-v1.yaml` | Válido (antes y después de los cambios) |
| Diagramas Mermaid | 6 diagramas de secuencia (`docs/specs/functional/diagrams/`) y los de `docs/architecture/` renderizan con `mmdc` |

La API se ejecutó contra SQL Server (base `Encuesta_Dev`) con autenticación de desarrollo (`/dev/token`); no se probó con un proveedor OIDC real. Las respuestas HTTP del contrato se comprobaron con llamadas reales para crear, consultar, asignar, publicar, cerrar y para el circuito público (404, 410, 422, 409, 201). Quedan sin comprobar por HTTP: el cierre por plazo vencido (solo pruebas unitarias) y la carrera de doble envío (índice único).

## 2. Contrato OpenAPI frente al código

### Coincide
- Los tres endpoints, sus verbos, rutas y `operationId`.
- Esquemas `CreateEncuestaRequest`, `EncuestaResponse`, `AssignEncuestaRequest` (nombres camelCase, enums como texto, límites de longitud).
- `Location` en el 201; 401 en los tres endpoints; 403/404 en `GET` y `PUT`; 409 en `PUT` sobre encuesta cerrada.

### Desviaciones corregidas en `Encuesta-v1.yaml`

| # | Desviación | Corrección |
|---|-----------|-----------|
| A1 | `PUT /assign` podía devolver **422** (GUID vacío o validación) y el contrato no lo declaraba | Añadida la respuesta 422 |
| A2 | Un `id` que no es GUID no coincide con la ruta (`{id:guid}`) y devuelve **404**, no 400 | Documentado en `GET` |
| A3 | El comportamiento de autenticación no estaba descrito: `sub` debe ser GUID; `role=admin` da acceso a encuestas ajenas | Añadido a la descripción general |
| A4 | Las claves de `errors` son nombres de propiedad en PascalCase (`Titulo`, `Preguntas[0].Opciones`) y los mensajes de validación de campos salen de FluentValidation en inglés | Documentado |
| A5 | Los valores de `type` en `ErrorResponse` tienen un conjunto fijo de *slugs*; los 401 y 404 de enrutamiento usan URIs de RFC 9110 | Documentado en `ErrorResponse.type` |
| A6 | Faltaba aclarar que `TextoLibre` y `Escala1a5` rechazan opciones (422), que el título se recorta y que un título de solo espacios se rechaza | Documentado en `POST` |
| A7 | El contrato no indicaba que solo existen 3 endpoints y que las encuestas permanecen en `Borrador` | Añadido «Estado de implementación» |

### Riesgos conocidos (sin cambio de código)
- **Mensajes en inglés** en validaciones de campos (título vacío, longitudes) frente a los mensajes en español que piden los escenarios Gherkin.
- Un JWT cuyo `sub` no sea GUID produce `UserId = Guid.Empty`: la creación falla con 422 y las consultas con 403.

## 3. Especificaciones Gherkin frente al código

De los 25 escenarios de `02-user-stories.md`, cada uno quedó etiquetado:

| Etiqueta | Escenarios | Historias |
|----------|-----------|-----------|
| `@implementado` | 16 | HU-01 (3), HU-02 (4), HU-03 (7), HU-04 (2: cierre manual, no cerrar borrador) |
| `@solo-dominio` | 3 | HU-01 (no editar publicada), HU-04 (cierre automático, no reabrir) |
| `@pendiente` | 6 | HU-01 (editar borrador), HU-05 (5) |

Desviaciones de texto y comportamiento (tabla completa en `02-user-stories.md`): mensaje de título vacío, confirmación tras crear, edición de borrador inexistente, reapertura/`Duplicar` inexistentes, mensaje «ya no acepta respuestas» inexistente, 403 con mensaje distinto, fecha límite igual a «ahora» rechazada.

## 4. Documentación de arquitectura frente al código

| Documento | Deriva | Acción |
|-----------|--------|--------|
| `domain-model.md` | `PlazoRespuesta` y `TokenPublico` no existen como Value Objects (son `DateTimeOffset?` y `string`) | Diagrama y texto alineados |
| `domain-model.md` | Firma real `Crear(creadorId, titulo, descripcion, ahora)`; `AgregarPregunta` devuelve `Pregunta`; `CerrarSiVencida` devuelve `bool` | Corregido |
| `domain-model.md` | Falta `Asignar` y `EvaluarSla`; sobran `QuitarPregunta` y `Duplicar` (no implementados) | Corregido; lo pendiente en la sección 0 |
| `domain-model.md` | Reloj `IClock` → en el código es `TimeProvider` | Corregido |
| `domain-model.md` | Eventos: solo 4 de 6 existen y ninguno se despacha (`ClearDomainEvents` sin uso, sin outbox) | Documentado |
| `c4-containers.md` | Frontend descrito como «SPA» genérica → Angular 22 | Corregido |
| `c4-containers.md` | Worker de fondo, Dapper, outbox, rate limiting, OpenTelemetry, health checks no existen | Marcados *(planeado)* |
| `c4-containers.md` | Redis solo está registrado; ningún caso de uso lo usa | Documentado |
| `c4-containers.md` | Rutas `/publico/{token}` no implementadas | Flujo marcado *planeado* |
| `ADR-001` | NetArchTest, outbox y Dapper no implementados; MediatR 14 requiere licencia | Añadido «Estado de implementación» |
| `01-vision-document.md` | RF-01 dice «≥ 1 pregunta» pero la API acepta encuestas sin preguntas | Sección 11 con estado por requisito |
| Diagramas de secuencia 01 | Rutas y flujo distintos al real (`/encuestas`, sin JWT ni `ValidationBehavior`) | Rehecho según el código; nuevo diagrama 06 (asignar) |
| Diagramas 02–05 | Sin endpoint en el código | Nota «PLANEADO» |

## 5. Brechas de código que este informe **no** corrige

Se dejan como trabajo pendiente porque el encargo era actualizar la documentación:

1. Mensajes de validación de título/longitudes en español (alinear con Gherkin) o revisar el Gherkin.
2. HU-05: resultados agregados y exportación CSV.
3. Edición de borrador (`QuitarPregunta`) y `Duplicar`.
4. Outbox y despacho de eventos de dominio.
5. Worker de cierre automático por plazo (`CerrarSiVencida` ya existe en el dominio).
6. Decidir si RF-01 exige ≥ 1 pregunta al crear o solo al publicar.
7. Endpoint de eliminación para el administrador (RF-09).
8. Caché Redis en la ruta pública y límite de tasa (el enlace es anónimo).

## 6. Actualización posterior: circuito publicar → responder → cerrar

Tras esta auditoría se implementaron HU-02, HU-03 y el cierre manual de HU-04, y los documentos se volvieron a alinear: contrato OpenAPI (5 endpoints de gestión y 2 públicos, respuesta 410), etiquetas Gherkin, estado de requisitos, modelo de dominio, C4 y los diagramas 02, 03 y 04. Durante las pruebas se corrigieron tres defectos: el orden de preguntas y opciones (las claves son GUID), y los formularios de Angular sin `FormsModule` cuyo `(ngSubmit)` nunca se disparaba.
