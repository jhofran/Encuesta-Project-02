# Encuesta System

Documentación en [docs/](docs/). Backend .NET 10 en `src/`, pruebas en `tests/`, frontend Angular en `frontend/`.

## Ejecución local

Requisitos: .NET 10 SDK, Node.js, una instancia de SQL Server accesible.

1. Base de datos: en `src/Encuesta.Api/appsettings.Development.json` ajusta `ConnectionStrings:SqlServer` (por defecto `.\SERVER`, base `Encuesta_Dev`). La migración se aplica sola al arrancar en Development.
2. Backend: `dotnet run --project src/Encuesta.Api --urls http://localhost:5261` con `ASPNETCORE_ENVIRONMENT=Development`.
3. Frontend: `cd frontend && npm install && npx ng serve` → http://localhost:4200 (el proxy envía `/api` y `/dev` al backend).
4. En la pantalla de login usa «Entrar como usuario» o «Entrar como admin». Esos tokens salen de `POST /dev/token`, que **solo existe en Development**.

## Flujo de uso
1. Entra con «Entrar como usuario» y crea una encuesta.
2. En el detalle, elige la fecha límite y pulsa «Publicar encuesta»: aparece el enlace `/e/{token}`.
3. Abre ese enlace (en otra ventana, sin sesión) para responder. «Cerrar encuesta» deja de aceptar respuestas.

## Pruebas
- `dotnet test`
- `cd frontend && npx ng test --watch=false`

## Migraciones
`dotnet tool restore` y luego `dotnet dotnet-ef migrations add <Nombre> -p src/Encuesta.Infrastructure -s src/Encuesta.Api -o Persistence/Migrations`.
