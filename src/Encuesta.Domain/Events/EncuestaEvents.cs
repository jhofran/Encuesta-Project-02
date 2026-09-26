using Encuesta.Domain.Common;
using Encuesta.Domain.Enums;

namespace Encuesta.Domain.Events;

public sealed record EncuestaCreada(Guid EncuestaId, Guid CreadorId, DateTimeOffset OcurridoEn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public sealed record EncuestaPublicada(Guid EncuestaId, string Token, DateTimeOffset FechaLimite, DateTimeOffset OcurridoEn)
    : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public sealed record EncuestaCerrada(Guid EncuestaId, MotivoCierre Motivo, DateTimeOffset OcurridoEn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public sealed record PreguntaAgregada(Guid EncuestaId, Guid PreguntaId, DateTimeOffset OcurridoEn) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
