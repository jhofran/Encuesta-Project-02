namespace Encuesta.Domain.Common;

public interface IDomainEvent
{
    Guid EventId { get; }
    DateTimeOffset OcurridoEn { get; }
}
