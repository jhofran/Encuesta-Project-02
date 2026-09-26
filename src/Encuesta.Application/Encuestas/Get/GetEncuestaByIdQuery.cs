using Encuesta.Application.Abstractions;
using MediatR;

namespace Encuesta.Application.Encuestas.Get;

public sealed record GetEncuestaByIdQuery(Guid Id) : IRequest<EncuestaResponse>;

public sealed class GetEncuestaByIdHandler(
    IEncuestaRepository repository, ICurrentUser currentUser, TimeProvider clock)
    : IRequestHandler<GetEncuestaByIdQuery, EncuestaResponse>
{
    public async Task<EncuestaResponse> Handle(GetEncuestaByIdQuery request, CancellationToken cancellationToken)
    {
        var encuesta = await repository.GetOwnedAsync(
            request.Id, currentUser, "Sin permiso sobre la encuesta", cancellationToken);

        return EncuestaResponse.From(encuesta, clock.GetUtcNow());
    }
}
