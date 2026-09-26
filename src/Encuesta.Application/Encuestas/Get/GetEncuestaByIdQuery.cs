using Encuesta.Application.Abstractions;
using Encuesta.Application.Common;
using MediatR;

namespace Encuesta.Application.Encuestas.Get;

public sealed record GetEncuestaByIdQuery(Guid Id) : IRequest<EncuestaResponse>;

public sealed class GetEncuestaByIdHandler(
    IEncuestaRepository repository, ICurrentUser currentUser, TimeProvider clock)
    : IRequestHandler<GetEncuestaByIdQuery, EncuestaResponse>
{
    public async Task<EncuestaResponse> Handle(GetEncuestaByIdQuery request, CancellationToken cancellationToken)
    {
        var encuesta = await repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("La encuesta no existe");

        if (encuesta.CreadorId != currentUser.UserId && !currentUser.IsAdmin)
            throw new ForbiddenException("Sin permiso sobre la encuesta");

        return EncuestaResponse.From(encuesta, clock.GetUtcNow());
    }
}
