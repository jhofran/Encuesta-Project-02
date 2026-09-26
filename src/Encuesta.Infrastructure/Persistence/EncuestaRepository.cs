using Encuesta.Application.Abstractions;
using Encuesta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using EncuestaAggregate = Encuesta.Domain.Entities.Encuesta;

namespace Encuesta.Infrastructure.Persistence;

internal sealed class EncuestaRepository(EncuestaDbContext db) : IEncuestaRepository
{
    public void Add(EncuestaAggregate encuesta) => db.Encuestas.Add(encuesta);

    public Task<EncuestaAggregate?> GetByIdAsync(Guid id, CancellationToken ct) =>
        ConPreguntas().FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<EncuestaAggregate?> GetByTokenAsync(string token, CancellationToken ct) =>
        ConPreguntas().FirstOrDefaultAsync(e => e.Token == token, ct);

    private IQueryable<EncuestaAggregate> ConPreguntas() =>
        db.Encuestas
            .Include(e => e.Preguntas).ThenInclude(p => p.Opciones)
            .AsSplitQuery();
}

internal sealed class RespuestaRepository(EncuestaDbContext db) : IRespuestaRepository
{
    public void Add(RespuestaEncuesta respuesta) => db.Respuestas.Add(respuesta);

    public Task<bool> ExisteAsync(Guid encuestaId, string huella, CancellationToken ct) =>
        db.Respuestas.AnyAsync(r => r.EncuestaId == encuestaId && r.Huella == huella, ct);
}
