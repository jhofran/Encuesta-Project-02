# Arquitectura C4 — Contenedores

Complementa [domain-model.md](domain-model.md). Decisión de estilo: [ADR-001](adr/ADR-001-clean-architecture-cqrs.md).

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
        Container(web, "Aplicación Web", "SPA (responsive)", "Diseño de encuestas, formulario de respuesta y resultados")
        Container(api, "Web API", ".NET 10 / ASP.NET Core", "Casos de uso CQRS, reglas de dominio, autenticación y autorización")
        Container(worker, "Worker de Fondo", ".NET 10 Worker Service", "Outbox dispatcher, cierre automático por plazo, avisos por vencer")
        ContainerDb(sql, "Base de datos", "SQL Server", "Fuente de verdad: encuestas, respuestas, outbox")
        ContainerDb(redis, "Caché", "Redis", "Lectura pública de encuestas, resultados agregados, anti-duplicados y rate limiting")
    }

    Rel(creador, web, "Usa", "HTTPS")
    Rel(participante, web, "Usa", "HTTPS")
    Rel(web, api, "Invoca", "JSON/HTTPS")
    Rel(api, idp, "Valida JWT", "OIDC")
    Rel(api, sql, "Lee/escribe (EF Core / Dapper)", "TDS")
    Rel(api, redis, "Cache-aside, contadores", "RESP")
    Rel(worker, sql, "Lee outbox, cierra encuestas", "TDS")
    Rel(worker, redis, "Invalida claves", "RESP")
```

## 3. Responsabilidades por contenedor

| Contenedor | Tecnología | Responsabilidad | Historias |
|------------|-----------|-----------------|-----------|
| Aplicación Web | SPA responsive | UI de creación, respuesta (móvil) y resultados | HU-01…05 |
| **Web API** | **.NET 10**, ASP.NET Core Minimal APIs, MediatR/handlers propios | Comandos y consultas, validación, autorización por propietario, exportación CSV | HU-01…05 |
| Worker de Fondo | .NET 10 Worker Service | Despachar outbox, `CerrarSiVencida`, emitir `EncuestaPorVencer` | HU-04 |
| **SQL Server** | SQL Server 2022+ | Persistencia transaccional; índice único `(EncuestaId, ParticipanteId)` para RF-05; tabla `Outbox` | Todas |
| **Redis Cache** | Redis 7+ | Ver §4 | HU-03, HU-05 |

## 4. Uso de Redis

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
        DAP[Dapper - Lectura]
        RC[Redis Cache]
        OB[Outbox]
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

Dependencias: `Api → Application → Domain`; `Infrastructure → Application/Domain`. El dominio no referencia nada externo.

## 6. Flujo clave: responder encuesta (HU-03)

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
