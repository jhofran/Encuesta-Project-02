using Encuesta.Application.Abstractions;
using MediatR;

namespace Encuesta.Application.Encuestas.Close;

public sealed record CloseEncuestaCommand(Guid Id) : IRequest<EncuestaResponse>;

public sealed class CloseEncuestaHandler(
    IEncuestaRepository repository, IUnitOfWork unitOfWork, ICurrentUser currentUser, TimeProvider clock)
    : IRequestHandler<CloseEncuestaCommand, EncuestaResponse>
{
    public async Task<EncuestaResponse> Handle(CloseEncuestaCommand request, CancellationToken cancellationToken)
    {
        var encuesta = await repository.GetOwnedAsync(
            request.Id, currentUser, "Solo el propietario o un administrador pueden cerrar", cancellationToken);
        var ahora = clock.GetUtcNow();

        encuesta.Cerrar(ahora);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return EncuestaResponse.From(encuesta, ahora);
    }
}
