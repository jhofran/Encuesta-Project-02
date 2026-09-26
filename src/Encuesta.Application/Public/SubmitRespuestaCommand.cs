using System.Security.Cryptography;
using System.Text;
using Encuesta.Application.Abstractions;
using Encuesta.Application.Common;
using Encuesta.Domain.Entities;
using Encuesta.Domain.Exceptions;
using FluentValidation;
using MediatR;
using EncuestaAggregate = Encuesta.Domain.Entities.Encuesta;

namespace Encuesta.Application.Public;

public sealed record RespuestaItemRequest(Guid PreguntaId, IReadOnlyList<string>? Valores);

/// <param name="ParticipanteToken">
/// Valor aleatorio generado por el navegador. Solo se usa (como hash) para impedir respuestas
/// duplicadas en encuestas de respuesta única.
/// </param>
public sealed record SubmitRespuestaCommand(
    string Token, string? ParticipanteToken, IReadOnlyList<RespuestaItemRequest>? Respuestas)
    : IRequest<RespuestaRecibidaResponse>;

public sealed record RespuestaRecibidaResponse(Guid Id, DateTimeOffset EnviadaEn);

public sealed class SubmitRespuestaCommandValidator : AbstractValidator<SubmitRespuestaCommand>
{
    public SubmitRespuestaCommandValidator()
    {
        RuleFor(x => x.ParticipanteToken).MaximumLength(100);
        RuleFor(x => x.Respuestas).NotNull().WithMessage("Las respuestas son obligatorias");
        RuleForEach(x => x.Respuestas).ChildRules(r =>
            r.RuleForEach(i => i.Valores).MaximumLength(RespuestaEncuesta.MaxTextoLibre));
    }
}

public sealed class SubmitRespuestaHandler(
    IEncuestaRepository encuestas,
    IRespuestaRepository respuestas,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock)
    : IRequestHandler<SubmitRespuestaCommand, RespuestaRecibidaResponse>
{
    public async Task<RespuestaRecibidaResponse> Handle(SubmitRespuestaCommand request, CancellationToken cancellationToken)
    {
        var encuesta = await encuestas.GetByTokenAsync(request.Token, cancellationToken)
            ?? throw new NotFoundException("Encuesta no encontrada");

        var items = (request.Respuestas ?? [])
            .Select(r => new RespuestaPregunta(r.PreguntaId, r.Valores ?? []))
            .ToList();
        var participanteId = currentUser.UserId == Guid.Empty ? (Guid?)null : currentUser.UserId;

        var respuesta = RespuestaEncuesta.Registrar(
            encuesta, items, participanteId, Huella(encuesta.Id, request.ParticipanteToken), clock.GetUtcNow());

        if (respuesta.Huella is not null && await respuestas.ExisteAsync(encuesta.Id, respuesta.Huella, cancellationToken))
            throw new DomainConflictException("Ya has respondido esta encuesta");

        respuestas.Add(respuesta);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new RespuestaRecibidaResponse(respuesta.Id, respuesta.EnviadaEn);
    }

    private static string? Huella(Guid encuestaId, string? participanteToken) =>
        string.IsNullOrWhiteSpace(participanteToken)
            ? null
            : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{encuestaId}:{participanteToken.Trim()}")));
}
