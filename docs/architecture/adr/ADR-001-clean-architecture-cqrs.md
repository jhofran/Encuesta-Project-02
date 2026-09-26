# ADR-001: Clean Architecture + CQRS frente a Monolito Anémico

| Campo | Valor |
|-------|-------|
| Estado | Propuesto |
| Fecha | 2026-09-26 |
| Decisores | Arquitectura, Lead de desarrollo |
| Relacionado | [domain-model.md](../domain-model.md), [c4-containers.md](../c4-containers.md) |

## Contexto

El módulo Encuesta tiene reglas de negocio con estado y transiciones (`Borrador → Publicada → Cerrada`, RN-01…RN-07), un plazo que cambia el comportamiento (`SLAStatus`), y dos perfiles de carga muy distintos:

- **Escritura de baja frecuencia y alta reglamentación:** crear, editar, publicar, cerrar.
- **Lectura pública de alta concurrencia:** participantes abriendo y respondiendo el enlace, y resultados agregados casi en tiempo real (OB-04).

Se evaluó cómo estructurar la Web API en .NET 10.

## Alternativas

### A. Monolito anémico (capas Controller → Service → Repository con entidades sin comportamiento)
- Entidades como bolsas de propiedades (`Estado` con setter público); reglas repartidas en servicios `EncuestaService`.
- Un único modelo (entidades EF) para leer y escribir.

### B. Clean Architecture + CQRS con dominio rico (elegida)
- Capas `Domain ← Application ← Infrastructure/Api`, dependencias hacia el dominio.
- Agregado `Encuesta` con invariantes y eventos de dominio.
- Comandos (escritura, vía agregados/EF Core) separados de consultas (lectura, vía proyecciones/Dapper + Redis).

## Decisión

Adoptar **Clean Architecture con CQRS** (mismo proceso y misma base de datos; sin *event sourcing*).

## Comparación

| Criterio | Monolito anémico | Clean Arch + CQRS |
|----------|------------------|-------------------|
| Protección de invariantes (RN-01…07) | Dispersas; cualquier servicio puede poner `Estado = Cerrada` sin validar | Encapsuladas en el agregado; estados inválidos irrepresentables |
| Testabilidad | Reglas mezcladas con EF/HTTP; requiere BD o mocks pesados | Dominio y casos de uso se prueban en memoria, sin infraestructura |
| Escalado de lectura vs. escritura | Un modelo para todo; consultas de resultados cargan grafos de entidades | Lecturas optimizadas (Dapper, Redis) independientes de la escritura |
| Cache de la ruta pública | Acoplada a servicios; invalidación ad hoc | Invalidación dirigida por eventos (`EncuestaCerrada`, `RespuestaRegistrada`) |
| Cambio de infraestructura (BD, caché, IdP) | Alto impacto: la lógica depende de EF | Bajo: puertos e implementaciones en Infrastructure |
| Trazabilidad con historias | Difusa | Un comando/consulta por historia o escenario |
| Coste inicial y curva | Bajo | Mayor: más proyectos, más código de estructura |
| Riesgo de "big ball of mud" | Alto al crecer (ej. lógica condicional, nuevos tipos de pregunta) | Bajo por límites explícitos |

## Consecuencias

**Positivas**
- Reglas de negocio en un solo lugar, verificables con pruebas unitarias rápidas.
- La ruta más crítica (responder) se cachea y optimiza sin afectar el modelo de escritura.
- Los eventos de dominio habilitan cierre automático, notificaciones e invalidación de caché de forma desacoplada.
- Base sólida si luego se extraen módulos (p. ej. Invitaciones, fuera del alcance del MVP).

**Negativas / costes**
- Más código de estructura (comandos, handlers, mapeos) para un dominio inicialmente pequeño.
- Consistencia eventual entre escritura y proyecciones/caché (segundos); aceptable para resultados (< 5 s).
- Requiere disciplina del equipo para no filtrar EF/HTTP al dominio.

**Mitigaciones**
- Estructura de solución fija (`Domain`, `Application`, `Infrastructure`, `Api`) y pruebas de arquitectura (p. ej. NetArchTest) que verifican la dirección de dependencias.
- CQRS ligero: handlers propios o MediatR; sin buses de mensajes ni *event sourcing* en el MVP.
- Outbox transaccional para publicar eventos sin pérdida.

## Cuándo reconsiderar

- Si el alcance se reduce a un formulario CRUD sin reglas de estado, el coste podría no justificarse.
- Si las lecturas exigen escalado independiente, evaluar réplicas de lectura o un almacén de proyecciones dedicado (evolución natural, no ruptura).

## Referencias
- Requisitos: RN-01…RN-07, OB-04 en [01-vision-document.md](../../specs/functional/01-vision-document.md)
- Historias: HU-01…HU-05 en [02-user-stories.md](../../specs/functional/02-user-stories.md)

## Estado de implementación (auditoría 2026-09-26)

La decisión se aplica: solución `Domain ← Application ← Infrastructure/Api`, dominio rico (`Encuesta` con invariantes y eventos), comandos y consultas con MediatR y un `ValidationBehavior` (FluentValidation). Matices respecto al texto de este ADR:

- **CQRS ligero:** hoy comandos y consultas usan el mismo modelo EF Core (`IEncuestaRepository`); las lecturas con Dapper/proyecciones y la caché Redis siguen **planeadas**.
- **Outbox:** no implementado; los eventos de dominio se acumulan en memoria sin despacharse.
- **Pruebas de arquitectura (NetArchTest):** no implementadas; la dirección de dependencias solo la garantizan las referencias de proyecto.
- **MediatR 14** requiere licencia comercial según el tamaño de la organización (`MediatR:LicenseKey`); no estaba contemplado al redactar el ADR. Si es un problema, los handlers propios son la alternativa.
