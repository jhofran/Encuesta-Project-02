using Encuesta.Application.Abstractions;
using Encuesta.Application.Common;
using EncuestaAggregate = Encuesta.Domain.Entities.Encuesta;

namespace Encuesta.Application.Encuestas;

internal static class EncuestaAccess
{
    /// <summary>Carga la encuesta y exige que el usuario sea su propietario o administrador.</summary>
    public static async Task<EncuestaAggregate> GetOwnedAsync(
        this IEncuestaRepository repository, Guid id, ICurrentUser user, string forbiddenMessage, CancellationToken ct)
    {
        var encuesta = await repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("La encuesta no existe");

        if (encuesta.CreadorId != user.UserId && !user.IsAdmin)
            throw new ForbiddenException(forbiddenMessage);

        return encuesta;
    }
}
