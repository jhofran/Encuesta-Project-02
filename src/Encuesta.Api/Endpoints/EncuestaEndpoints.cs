using Encuesta.Application.Encuestas;
using Encuesta.Application.Encuestas.Assign;
using Encuesta.Application.Encuestas.Create;
using Encuesta.Application.Encuestas.Get;
using MediatR;

namespace Encuesta.Api.Endpoints;

public sealed record AssignEncuestaRequest(Guid ResponsableId);

public static class EncuestaEndpoints
{
    public static IEndpointRouteBuilder MapEncuestaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/Encuesta").WithTags("Encuesta").RequireAuthorization();

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

        return app;
    }
}
