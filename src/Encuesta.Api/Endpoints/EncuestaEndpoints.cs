using Encuesta.Application.Encuestas;
using Encuesta.Application.Encuestas.Assign;
using Encuesta.Application.Encuestas.Close;
using Encuesta.Application.Encuestas.Create;
using Encuesta.Application.Encuestas.Get;
using Encuesta.Application.Encuestas.Publish;
using Encuesta.Application.Public;
using MediatR;

namespace Encuesta.Api.Endpoints;

public sealed record AssignEncuestaRequest(Guid ResponsableId);

public sealed record PublishEncuestaRequest(DateTimeOffset FechaLimite, bool EsAnonima = false, bool RespuestaUnica = false);

public sealed record SubmitRespuestaRequest(string? ParticipanteToken, IReadOnlyList<RespuestaItemRequest>? Respuestas);

public static class EncuestaEndpoints
{
    public static IEndpointRouteBuilder MapEncuestaEndpoints(this IEndpointRouteBuilder app)
    {
        MapGestion(app.MapGroup("/api/v1/Encuesta").WithTags("Encuesta").RequireAuthorization());
        MapPublico(app.MapGroup("/api/v1/public").WithTags("Public").AllowAnonymous());
        return app;
    }

    private static void MapGestion(RouteGroupBuilder group)
    {
        group.MapPost("/", async (CreateEncuestaCommand command, ISender sender, CancellationToken ct) =>
        {
            var response = await sender.Send(command, ct);
            return Results.Created($"/api/v1/Encuesta/{response.Id}", response);
        })
        .WithName("createEncuesta")
        .Produces<EncuestaResponse>(StatusCodes.Status201Created);

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetEncuestaByIdQuery(id), ct)))
        .WithName("getEncuestaById")
        .Produces<EncuestaResponse>();

        group.MapPut("/{id:guid}/assign", async (Guid id, AssignEncuestaRequest body, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new AssignEncuestaCommand(id, body.ResponsableId), ct)))
        .WithName("assignEncuesta")
        .Produces<EncuestaResponse>();

        group.MapPost("/{id:guid}/publish", async (Guid id, PublishEncuestaRequest body, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new PublishEncuestaCommand(id, body.FechaLimite, body.EsAnonima, body.RespuestaUnica), ct)))
        .WithName("publishEncuesta")
        .Produces<EncuestaResponse>();

        group.MapPost("/{id:guid}/close", async (Guid id, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new CloseEncuestaCommand(id), ct)))
        .WithName("closeEncuesta")
        .Produces<EncuestaResponse>();
    }

    private static void MapPublico(RouteGroupBuilder group)
    {
        group.MapGet("/{token}", async (string token, ISender sender, CancellationToken ct) =>
            Results.Ok(await sender.Send(new GetPublicEncuestaQuery(token), ct)))
        .WithName("getPublicEncuesta")
        .Produces<PublicEncuestaResponse>();

        group.MapPost("/{token}/respuestas", async (string token, SubmitRespuestaRequest body, ISender sender, CancellationToken ct) =>
        {
            var response = await sender.Send(new SubmitRespuestaCommand(token, body.ParticipanteToken, body.Respuestas), ct);
            return Results.Created((string?)null, response);
        })
        .WithName("submitRespuesta")
        .Produces<RespuestaRecibidaResponse>(StatusCodes.Status201Created);
    }
}
