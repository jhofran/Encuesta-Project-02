using Encuesta.Application.Abstractions;
using Encuesta.Application.Common;
using Encuesta.Application.Encuestas;
using Encuesta.Domain.Enums;
using Encuesta.Domain.Exceptions;
using MediatR;
using EncuestaAggregate = Encuesta.Domain.Entities.Encuesta;

namespace Encuesta.Application.Public;

/// <summary>Vista pública de la encuesta: no expone propietario, token ni estado interno.</summary>
public sealed record PublicEncuestaResponse(
    string Titulo,
    string? Descripcion,
    bool EsAnonima,
    bool RespuestaUnica,
    DateTimeOffset? FechaLimite,
    IReadOnlyList<PreguntaResponse> Preguntas)
{
    public static PublicEncuestaResponse From(EncuestaAggregate e) => new(
        e.Titulo,
        e.Descripcion,
        e.EsAnonima,
        e.RespuestaUnica,
        e.FechaLimite,
        EncuestaResponse.PreguntasOrdenadas(e));
}

public sealed record GetPublicEncuestaQuery(string Token) : IRequest<PublicEncuestaResponse>;

public sealed class GetPublicEncuestaHandler(IEncuestaRepository repository, TimeProvider clock)
    : IRequestHandler<GetPublicEncuestaQuery, PublicEncuestaResponse>
{
    public async Task<PublicEncuestaResponse> Handle(GetPublicEncuestaQuery request, CancellationToken cancellationToken)
    {
        var encuesta = await repository.GetByTokenAsync(request.Token, cancellationToken)
            ?? throw new NotFoundException("Encuesta no encontrada");

        if (!encuesta.AceptaRespuestas(clock.GetUtcNow()))
            throw new EncuestaNoDisponibleException();

        return PublicEncuestaResponse.From(encuesta);
    }
}
