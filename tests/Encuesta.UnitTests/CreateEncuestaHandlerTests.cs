using Encuesta.Application.Abstractions;
using Encuesta.Application.Encuestas.Create;
using Encuesta.Domain.Enums;
using FluentAssertions;
using NSubstitute;
using EncuestaAggregate = Encuesta.Domain.Entities.Encuesta;

namespace Encuesta.UnitTests;

public class CreateEncuestaHandlerTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);

    private readonly IEncuestaRepository _repository = Substitute.For<IEncuestaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly CreateEncuestaHandler _handler;

    public CreateEncuestaHandlerTests()
    {
        _currentUser.UserId.Returns(_userId);
        _handler = new CreateEncuestaHandler(_repository, _unitOfWork, _currentUser, new FixedTimeProvider(Ahora));
    }

    [Fact]
    [Trait("Story", "HU-01")]
    public async Task Handle_ComandoValido_PersisteBorradorDelUsuarioActual() // HU-01 Escenario: Crear encuesta en borrador con preguntas válidas
    {
        var command = new CreateEncuestaCommand(
            "Satisfacción del cliente", null,
            [new CreatePreguntaRequest("¿Cómo calificas el servicio?", TipoPregunta.Escala1a5, true)]);

        var response = await _handler.Handle(command, CancellationToken.None);

        response.Estado.Should().Be(EstadoEncuesta.Borrador);
        response.CreadorId.Should().Be(_userId);
        response.SlaStatus.Should().Be(SLAStatus.SinPlazo);
        response.Preguntas.Should().ContainSingle();
        _repository.Received(1).Add(Arg.Is<EncuestaAggregate>(e => e.Id == response.Id));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Story", "HU-01")]
    public async Task Handle_ReglaDeDominioIncumplida_NoPersiste() // HU-01 Escenario: Rechazar título vacío
    {
        var command = new CreateEncuestaCommand(" ", null, null);

        var act = () => _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<Domain.Exceptions.DomainValidationException>();
        _repository.DidNotReceive().Add(Arg.Any<EncuestaAggregate>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Story", "HU-01")]
    public void Validator_OpcionUnicaConUnaOpcion_Falla() // HU-01 Escenario: Pregunta de opción con menos de dos opciones
    {
        var command = new CreateEncuestaCommand(
            "Titulo", null, [new CreatePreguntaRequest("¿Color?", TipoPregunta.OpcionUnica, false, ["Rojo"])]);

        var result = new CreateEncuestaCommandValidator().Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Se requieren al menos 2 opciones");
    }

    [Fact]
    [Trait("Story", "HU-01")]
    public void Validator_SinTitulo_Falla() // HU-01 Escenario: Rechazar título vacío
    {
        var result = new CreateEncuestaCommandValidator().Validate(new CreateEncuestaCommand("", null, null));

        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEncuestaCommand.Titulo));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
