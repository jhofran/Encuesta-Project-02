using Encuesta.Domain.Entities;
using EncuestaAggregate = Encuesta.Domain.Entities.Encuesta;

namespace Encuesta.Application.Abstractions;

public interface IEncuestaRepository
{
    void Add(EncuestaAggregate encuesta);

    /// <summary>Devuelve la encuesta con preguntas y opciones, con seguimiento de cambios.</summary>
    Task<EncuestaAggregate?> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>Busca por el token del enlace público (solo existe en encuestas publicadas).</summary>
    Task<EncuestaAggregate?> GetByTokenAsync(string token, CancellationToken ct);
}

public interface IRespuestaRepository
{
    void Add(RespuestaEncuesta respuesta);

    Task<bool> ExisteAsync(Guid encuestaId, string huella, CancellationToken ct);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct);
}

public interface ICurrentUser
{
    /// <summary><see cref="Guid.Empty"/> si la petición es anónima.</summary>
    Guid UserId { get; }
    bool IsAdmin { get; }
}
