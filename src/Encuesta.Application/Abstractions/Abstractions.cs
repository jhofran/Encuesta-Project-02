using EncuestaAggregate = Encuesta.Domain.Entities.Encuesta;

namespace Encuesta.Application.Abstractions;

public interface IEncuestaRepository
{
    void Add(EncuestaAggregate encuesta);

    /// <summary>Devuelve la encuesta con preguntas y opciones, con seguimiento de cambios.</summary>
    Task<EncuestaAggregate?> GetByIdAsync(Guid id, CancellationToken ct);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
}

public interface ICurrentUser
{
    Guid UserId { get; }
    bool IsAdmin { get; }
}
