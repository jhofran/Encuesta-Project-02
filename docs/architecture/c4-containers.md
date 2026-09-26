# Arquitectura C4 — Contenedores

Complementa [domain-model.md](domain-model.md). Decisión de estilo: [ADR-001](adr/ADR-001-clean-architecture-cqrs.md).

> **Sincronizado con el código el 2026-09-26** (auditoría: [drift-report.md](../audit/drift-report.md)). Los diagramas muestran la arquitectura **objetivo**; los elementos aún no construidos están marcados como *(planeado)*.

## 0. Estado de implementación

| Elemento | Estado | Detalle real |
|---|---|---|
| Aplicación Web | ✅ Parcial | Angular 22 (`frontend/`), standalone + signals. Pantallas: login (JWT pegado), inicio, crear encuesta, detalle y asignación. Faltan publicar, cerrar, responder y resultados |
| Web API (.NET 10) | ✅ Parcial | Minimal APIs + MediatR 14 + FluentValidation 12 + JWT Bearer. Solo 3 endpoints (`POST`, `GET {id}`, `PUT {id}/assign`) bajo `/api/v1/Encuesta` |
| SQL Server | 🟡 Modelo listo, sin migraciones | EF Core 10; tablas `Encuesta`, `Pregunta`, `OpcionPregunta`. Falta `dotnet ef migrations add`. No existen `Outbox` ni el índice único de respuestas |
| Redis | 🟡 Solo registrado | `AddStackExchangeRedisCache` si hay cadena `ConnectionStrings:Redis`; si no, caché en memoria. Ningún caso de uso lo consume todavía |
| Dapper (lecturas CQRS) | ❌ Planeado | Las consultas usan EF Core (`GetByIdAsync` con `Include`) |
| Worker de fondo | ❌ Planeado | No existe el proyecto |
| Outbox y despacho de eventos | ❌ Planeado | Los eventos se acumulan en memoria sin persistirse |
| Rate limiting, OpenTelemetry, health checks | ❌ Planeado | |
| Proveedor OIDC | ➖ Externo | `Authentication:Authority` apunta a un valor ficticio (`idp.encuesta.local`) |
| Rutas públicas `/publico/{token}` | ❌ Planeado | Ver §6 |

## 1. Nivel 1 — Contexto del sistema

```mermaid
C4Context
    title Contexto — Encuesta System
    Person(creador, "Creador / Analista", "Diseña encuestas y consulta resultados")
    Person(participante, "Participante", "Responde encuestas vía enlace público")
    Person(admin, "Administrador", "Supervisa y modera")
    System(encuesta, "Encuesta System", "Crear, publicar, responder y analizar encuestas")
    System_Ext(idp, "Proveedor de identidad (OIDC)", "Autenticación de creadores y administradores")

    Rel(creador, encuesta, "Gestiona encuestas y resultados", "HTTPS")
    Rel(participante, encuesta, "Responde encuestas", "HTTPS")
    Rel(admin, encuesta, "Administra", "HTTPS")
    Rel(encuesta, idp, "Valida tokens JWT", "OIDC")
```

## 2. Nivel 2 — Contenedores

```mermaid
C4Container
    title Contenedores — Encuesta System
    Person(creador, "Creador / Analista", "Autenticado")
    Person(participante, "Participante", "Anónimo o identificado")
    System_Ext(idp, "Proveedor OIDC", "Emite JWT")

    System_Boundary(sys, "Encuesta System") {
        Container(web, "Aplicación Web", "Angular 22 SPA (responsive)", "Diseño de encuestas, asignación; formulario de respuesta y resultados (planeado)")
        Container(api, "Web API", ".NET 10 / ASP.NET Core", "Casos de uso CQRS (MediatR), reglas de dominio, JWT y autorización por propietario")
        Container(worker, "Worker de Fondo (planeado)", ".NET 10 Worker Service", "Outbox dispatcher, cierre automático por plazo, avisos por vencer")
        ContainerDb(sql, "Base de datos", "SQL Server", "Fuente de verdad: encuestas; respuestas y outbox (planeado)")
        ContainerDb(redis, "Caché", "Redis", "Registrado sin uso aún; objetivo: lectura pública, resultados, anti-duplicados y rate limiting")
    }

    Rel(creador, web, "Usa", "HTTPS")
    Rel(participante, web, "Usa", "HTTPS")
    Rel(web, api, "Invoca", "JSON/HTTPS")
    Rel(api, idp, "Valida JWT", "OIDC")
    Rel(api, sql, "Lee/escribe (EF Core; Dapper planeado)", "TDS")
    Rel(api, redis, "Cache-aside, contadores", "RESP")
    Rel(worker, sql, "Lee outbox, cierra encuestas", "TDS")
    Rel(worker, redis, "Invalida claves", "RESP")
```

## 3. Responsabilidades por contenedor

| Contenedor | Tecnología | Responsabilidad | Historias |
|------------|-----------|-----------------|-----------|
| Aplicación Web | Angular 22 SPA responsive | UI de creación y asignación (implementado); respuesta (móvil) y resultados (planeado) | HU-01 (parcial) |
| **Web API** | **.NET 10**, ASP.NET Core Minimal APIs, MediatR, FluentValidation | Comandos y consultas, validación, autorización por propietario/administrador; exportación CSV (planeado) | HU-01 (parcial) |
| Worker de Fondo (planeado) | .NET 10 Worker Service | Despachar outbox, `CerrarSiVencida`, emitir `EncuestaPorVencer` | HU-04 |
| **SQL Server** | SQL Server 2022+ | Persistencia transaccional; índice único `(EncuestaId, ParticipanteId)` para RF-05; tabla `Outbox` | Todas |
| **Redis Cache** | Redis 7+ | Ver §4 (diseño objetivo; hoy solo registrado) | HU-03, HU-05 (planeado) |

## 4. Uso de Redis

> Diseño objetivo. Hoy ningún caso de uso lee ni escribe estas claves.

| Clave | Contenido | TTL / invalidación | Motivo |
|-------|-----------|--------------------|--------|
| `encuesta:pub:{token}` | Vista pública (preguntas, estado, fecha límite) | Hasta `FechaLimite` o evento `EncuestaCerrada` | Ruta más concurrida (HU-03) sin tocar SQL |
| `resultados:{encuestaId}` | Agregados por pregunta | 30 s + invalidación con `RespuestaRegistrada` | OB-04: resultados < 5 s |
| `resp:{encuestaId}:{huella}` | Marca de "ya respondió" (cookie/token de un solo uso en anónimas) | Hasta `FechaLimite` | Verificación rápida RF-05; la garantía final es el índice único en SQL |
| `rl:{ip}:{ruta}` | Contador de tasa | Ventana deslizante 1 min | Mitiga abuso/bots del enlace público |

Reglas: Redis **nunca es fuente de verdad**; ante fallo de Redis la API degrada a SQL (cache-aside con *fail-open*). La unicidad de respuesta se garantiza en SQL Server.

## 5. Estructura interna de la Web API (Clean Architecture)

```mermaid
flowchart LR
    subgraph API["Encuesta.Api (Presentación)"]
        EP[Endpoints / Middleware]
    end
    subgraph APP["Encuesta.Application"]
        CMD[Commands + Handlers]
        QRY[Queries + Handlers]
        VAL[Validadores / Behaviors]
    end
    subgraph DOM["Encuesta.Domain"]
        AGG[Agregados, VOs, Eventos]
    end
    subgraph INF["Encuesta.Infrastructure"]
        EF[EF Core - Escritura]
        DAP[Dapper - Lectura, planeado]
        RC[Redis Cache, registrado sin uso]
        OB[Outbox, planeado]
    end
    EP --> CMD
    EP --> QRY
    CMD --> AGG
    CMD -. puerto .-> EF
    QRY -. puerto .-> DAP
    QRY -. puerto .-> RC
    EF --> OB
    INF -. implementa puertos de .-> APP
    APP --> DOM
```

En el código actual `QRY` usa EF Core mediante `IEncuestaRepository` (no hay puertos de lectura separados). Dependencias: `Api → Application → Domain`; `Infrastructure → Application/Domain`. El dominio no referencia nada externo.

## 6. Flujo clave: responder encuesta (HU-03) — *planeado, no implementado*

```mermaid
sequenceDiagram
    actor P as Participante
    participant W as Aplicación Web
    participant A as Web API .NET 10
    participant R as Redis
    participant S as SQL Server
    P->>W: Abre enlace /e/{token}
    W->>A: GET /publico/{token}
    A->>R: GET encuesta:pub:{token}
    alt Hit
        R-->>A: Vista pública
    else Miss
        A->>S: Consulta encuesta
        S-->>A: Datos
        A->>R: SET (TTL hasta FechaLimite)
    end
    A-->>W: 200 preguntas (o 404/410)
    P->>W: Envía respuestas
    W->>A: POST /publico/{token}/respuestas
    A->>R: Verificar resp:{encuestaId}:{huella}
    A->>A: Comando RegistrarRespuesta (valida RN-03/04/06)
    A->>S: INSERT respuesta + outbox (transacción)
    S-->>A: OK (o violación de índice único → 409)
    A->>R: SET resp:... e invalidar resultados:{id}
    A-->>W: 201
```

## 7. Requisitos no funcionales y decisiones

| Atributo | Meta | Táctica |
|----------|------|---------|
| Rendimiento | Lectura pública p95 < 200 ms; resultados < 5 s | Redis cache-aside; consultas con Dapper sobre proyecciones |
| Consistencia | RF-05 sin duplicados | Índice único en SQL + verificación previa en Redis |
| Disponibilidad | Degradación si cae Redis | *Fail-open* a SQL |
| Seguridad | Autorización por propietario; anonimato (RN-06) | JWT/OIDC, política por recurso; no persistir identidad en anónimas; rate limiting |
| Observabilidad | Trazabilidad extremo a extremo | OpenTelemetry (trazas, métricas, logs), health checks |
| Evolución | Escalar lecturas/escrituras por separado | CQRS ([ADR-001](adr/ADR-001-clean-architecture-cqrs.md)); réplicas de lectura futuras |
| Despliegue | Contenedores | Docker; API y Worker escalan de forma independiente |
