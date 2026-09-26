using Encuesta.Application.Abstractions;
using Encuesta.Application.Common;
using Encuesta.Application.Encuestas.Close;
using Encuesta.Application.Encuestas.Publish;
using Encuesta.Application.Public;
using Encuesta.Domain.Entities;
using Encuesta.Domain.Enums;
using Encuesta.Domain.Exceptions;
using FluentAssertions;
using NSubstitute;
using EncuestaAggregate = Encuesta.Domain.Entities.Encuesta;

namespace Encuesta.UnitTests;

public class PublicHandlersTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);

    private readonly IEncuestaRepository _encuestas = Substitute.For<IEncuestaRepository>();
    private readonly IRespuestaRepository _respuestas = Substitute.For<IRespuestaRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _user = Substitute.For<ICurrentUser>();
    private readonly TimeProvider _clock = new FixedClock(Ahora);
    private readonly Guid _propietario = Guid.NewGuid();

    private EncuestaAggregate Borrador()
    {
        var e = EncuestaAggregate.Crear(_propietario, "Satisfacción", null, Ahora);
        e.AgregarPregunta("¿Cómo calificas?", TipoPregunta.Escala1a5, true, null, Ahora);
        return e;
    }

    private EncuestaAggregate Publicada(bool respuestaUnica = false)
    {
        var e = Borrador();
        e.Publicar(Ahora.AddDays(7), false, respuestaUnica, Ahora);
        _encuestas.GetByTokenAsync(e.Token!, Arg.Any<CancellationToken>()).Returns(e);
        return e;
    }

    private SubmitRespuestaHandler SubmitHandler() => new(_encuestas, _respuestas, _unitOfWork, _user, _clock);

    private static SubmitRespuestaCommand Submit(EncuestaAggregate e, string? participante = null, string valor = "4") =>
        new(e.Token!, participante, [new RespuestaItemRequest(e.Preguntas[0].Id, [valor])]);

    // ------------------------------------------------------------ HU-02 Publicar

    [Fact]
    [Trait("Story", "HU-02")]
    public async Task Publish_ElPropietario_PublicaYGuarda() // HU-02 Escenario: Publicar una encuesta válida
    {
        var e = Borrador();
        _user.UserId.Returns(_propietario);
        _encuestas.GetByIdAsync(e.Id, Arg.Any<CancellationToken>()).Returns(e);
        var handler = new PublishEncuestaHandler(_encuestas, _unitOfWork, _user, _clock);

        var response = await handler.Handle(new PublishEncuestaCommand(e.Id, Ahora.AddDays(30), true, false), CancellationToken.None);

        response.Estado.Should().Be(EstadoEncuesta.Publicada);
        response.Token.Should().NotBeNullOrEmpty();
        response.SlaStatus.Should().Be(SLAStatus.Vigente);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Story", "HU-02")]
    public async Task Publish_UsuarioAjeno_EsRechazado()
    {
        var e = Borrador();
        _user.UserId.Returns(Guid.NewGuid());
        _encuestas.GetByIdAsync(e.Id, Arg.Any<CancellationToken>()).Returns(e);
        var handler = new PublishEncuestaHandler(_encuestas, _unitOfWork, _user, _clock);

        var act = () => handler.Handle(new PublishEncuestaCommand(e.Id, Ahora.AddDays(1), false, false), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // ------------------------------------------------------------ HU-04 Cerrar

    [Fact]
    [Trait("Story", "HU-04")]
    public async Task Close_Administrador_CierraEncuestaAjena() // HU-04 Escenario: Cierre manual
    {
        var e = Publicada();
        _user.UserId.Returns(Guid.NewGuid());
        _user.IsAdmin.Returns(true);
        _encuestas.GetByIdAsync(e.Id, Arg.Any<CancellationToken>()).Returns(e);
        var handler = new CloseEncuestaHandler(_encuestas, _unitOfWork, _user, _clock);

        var response = await handler.Handle(new CloseEncuestaCommand(e.Id), CancellationToken.None);

        response.Estado.Should().Be(EstadoEncuesta.Cerrada);
    }

    // ------------------------------------------------------------ HU-03 Responder

    [Fact]
    [Trait("Story", "HU-03")]
    public async Task GetPublic_EncuestaVigente_DevuelveLasPreguntasSinDatosInternos() // HU-03 Escenario: Enviar respuestas válidas (abrir enlace)
    {
        var e = Publicada();

        var response = await new GetPublicEncuestaHandler(_encuestas, _clock)
            .Handle(new GetPublicEncuestaQuery(e.Token!), CancellationToken.None);

        response.Titulo.Should().Be("Satisfacción");
        response.Preguntas.Should().ContainSingle();
    }

    [Fact]
    [Trait("Story", "HU-03")]
    public async Task GetPublic_TokenInexistente_EsNotFound() // HU-03 Escenario: Enlace inexistente
    {
        var act = () => new GetPublicEncuestaHandler(_encuestas, _clock)
            .Handle(new GetPublicEncuestaQuery("no-existe"), CancellationToken.None);

        (await act.Should().ThrowAsync<NotFoundException>()).WithMessage("Encuesta no encontrada");
    }

    [Fact]
    [Trait("Story", "HU-03")]
    public async Task GetPublic_EncuestaCerrada_NoEstaDisponible() // HU-03 Escenario: Encuesta cerrada
    {
        var e = Publicada();
        e.Cerrar(Ahora);

        var act = () => new GetPublicEncuestaHandler(_encuestas, _clock)
            .Handle(new GetPublicEncuestaQuery(e.Token!), CancellationToken.None);

        await act.Should().ThrowAsync<EncuestaNoDisponibleException>();
    }

    [Fact]
    [Trait("Story", "HU-03")]
    public async Task Submit_RespuestaValida_SeRegistra() // HU-03 Escenario: Enviar respuestas válidas
    {
        var e = Publicada();

        var response = await SubmitHandler().Handle(Submit(e), CancellationToken.None);

        response.Id.Should().NotBeEmpty();
        _respuestas.Received(1).Add(Arg.Is<RespuestaEncuesta>(r => r.EncuestaId == e.Id && r.Items.Count == 1));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    [Trait("Story", "HU-03")]
    public async Task Submit_ConRespuestaPrevia_EsConflicto() // HU-03 Escenario: Respuesta duplicada en encuesta de respuesta única
    {
        var e = Publicada(respuestaUnica: true);
        _respuestas.ExisteAsync(e.Id, Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);

        var act = () => SubmitHandler().Handle(Submit(e, participante: "navegador-1"), CancellationToken.None);

        (await act.Should().ThrowAsync<DomainConflictException>()).WithMessage("Ya has respondido esta encuesta");
        _respuestas.DidNotReceive().Add(Arg.Any<RespuestaEncuesta>());
    }

    [Fact]
    [Trait("Story", "HU-03")]
    public async Task Submit_RespuestaUnica_GuardaSoloElHashDelToken() // HU-03 Escenario: Encuesta anónima no guarda identidad
    {
        var e = Publicada(respuestaUnica: true);

        await SubmitHandler().Handle(Submit(e, participante: "navegador-1"), CancellationToken.None);

        _respuestas.Received(1).Add(Arg.Is<RespuestaEncuesta>(r =>
            r.Huella != null && r.Huella != "navegador-1" && r.Huella.Length == 64));
    }

    [Fact]
    [Trait("Story", "HU-03")]
    public async Task Submit_TokenInexistente_EsNotFound() // HU-03 Escenario: Enlace inexistente
    {
        var act = () => SubmitHandler().Handle(new SubmitRespuestaCommand("nada", null, []), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    [Trait("Story", "HU-03")]
    public void SubmitValidator_SinListaDeRespuestas_Falla()
    {
        var result = new SubmitRespuestaCommandValidator().Validate(new SubmitRespuestaCommand("t", null, null));

        result.IsValid.Should().BeFalse();
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
