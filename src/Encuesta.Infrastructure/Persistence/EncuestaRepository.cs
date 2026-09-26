using Encuesta.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using EncuestaAggregate = Encuesta.Domain.Entities.Encuesta;

namespace Encuesta.Infrastructure.Persistence;

internal sealed class EncuestaRepository(EncuestaDbContext db) : IEncuestaRepository
{
    public void Add(EncuestaAggregate encuesta) => db.Encuestas.Add(encuesta);

    public Task<EncuestaAggregate?> GetByIdAsync(Guid id, CancellationToken ct) =>
        db.Encuestas
            .Include(e => e.Preguntas).ThenInclude(p => p.Opciones)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == id, ct);
}
