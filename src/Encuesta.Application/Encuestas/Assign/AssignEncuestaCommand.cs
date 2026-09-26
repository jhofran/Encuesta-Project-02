using Encuesta.Application.Abstractions;
using FluentValidation;
using MediatR;

namespace Encuesta.Application.Encuestas.Assign;

public sealed record AssignEncuestaCommand(Guid Id, Guid ResponsableId) : IRequest<EncuestaResponse>;

public sealed class AssignEncuestaCommandValidator : AbstractValidator<AssignEncuestaCommand>
{
    public AssignEncuestaCommandValidator() => RuleFor(x => x.ResponsableId).NotEmpty();
}

public sealed class AssignEncuestaHandler(
    IEncuestaRepository repository, IUnitOfWork unitOfWork, ICurrentUser currentUser, TimeProvider clock)
    : IRequestHandler<AssignEncuestaCommand, EncuestaResponse>
{
    public async Task<EncuestaResponse> Handle(AssignEncuestaCommand request, CancellationToken cancellationToken)
    {
        var encuesta = await repository.GetOwnedAsync(
            request.Id, currentUser, "Solo el propietario o un administrador pueden reasignar", cancellationToken);

        encuesta.Asignar(request.ResponsableId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return EncuestaResponse.From(encuesta, clock.GetUtcNow());
    }
}
