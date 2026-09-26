using Encuesta.Application.Abstractions;
using Encuesta.Domain.Enums;
using FluentValidation;
using MediatR;
using EncuestaAggregate = Encuesta.Domain.Entities.Encuesta;

namespace Encuesta.Application.Encuestas.Create;

public sealed record CreatePreguntaRequest(
    string Texto, TipoPregunta Tipo, bool EsObligatoria = false, IReadOnlyList<string>? Opciones = null);

public sealed record CreateEncuestaCommand(
    string Titulo, string? Descripcion, IReadOnlyList<CreatePreguntaRequest>? Preguntas)
    : IRequest<EncuestaResponse>;

public sealed class CreateEncuestaCommandValidator : AbstractValidator<CreateEncuestaCommand>
{
    public CreateEncuestaCommandValidator()
    {
        RuleFor(x => x.Titulo).NotEmpty().MaximumLength(EncuestaAggregate.MaxTitulo);
        RuleFor(x => x.Descripcion).MaximumLength(EncuestaAggregate.MaxDescripcion);
        RuleFor(x => x.Preguntas).Must(p => p is null || p.Count <= EncuestaAggregate.MaxPreguntas)
            .WithMessage($"Máximo {EncuestaAggregate.MaxPreguntas} preguntas");
        RuleForEach(x => x.Preguntas).SetValidator(new CreatePreguntaValidator());
    }
}

public sealed class CreatePreguntaValidator : AbstractValidator<CreatePreguntaRequest>
{
    public CreatePreguntaValidator()
    {
        RuleFor(x => x.Texto).NotEmpty().MaximumLength(EncuestaAggregate.MaxTextoPregunta);
        RuleFor(x => x.Tipo).IsInEnum();
        RuleFor(x => x.Opciones).Must(o => o is null || o.Count <= EncuestaAggregate.MaxOpciones)
            .WithMessage($"Máximo {EncuestaAggregate.MaxOpciones} opciones");
        RuleForEach(x => x.Opciones).NotEmpty().MaximumLength(EncuestaAggregate.MaxTextoOpcion);
        RuleFor(x => x.Opciones)
            .Must(o => o is { Count: >= 2 })
            .When(x => x.Tipo is TipoPregunta.OpcionUnica or TipoPregunta.OpcionMultiple)
            .WithMessage("Se requieren al menos 2 opciones");
    }
}

public sealed class CreateEncuestaHandler(
    IEncuestaRepository repository, IUnitOfWork unitOfWork, ICurrentUser currentUser, TimeProvider clock)
    : IRequestHandler<CreateEncuestaCommand, EncuestaResponse>
{
    public async Task<EncuestaResponse> Handle(CreateEncuestaCommand request, CancellationToken cancellationToken)
    {
        var ahora = clock.GetUtcNow();
        var encuesta = EncuestaAggregate.Crear(currentUser.UserId, request.Titulo, request.Descripcion, ahora);

        foreach (var p in request.Preguntas ?? [])
            encuesta.AgregarPregunta(p.Texto, p.Tipo, p.EsObligatoria, p.Opciones, ahora);

        repository.Add(encuesta);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return EncuestaResponse.From(encuesta, ahora);
    }
}
