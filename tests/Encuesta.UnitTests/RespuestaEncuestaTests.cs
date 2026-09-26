using Encuesta.Domain.Entities;
using Encuesta.Domain.Enums;
using Encuesta.Domain.Events;
using Encuesta.Domain.Exceptions;
using FluentAssertions;
using EncuestaAggregate = Encuesta.Domain.Entities.Encuesta;

namespace Encuesta.UnitTests;

/// <summary>Escenarios de HU-03 (Responder encuesta) sobre el agregado RespuestaEncuesta.</summary>
public class RespuestaEncuestaTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);

    private sealed record Escenario(EncuestaAggregate Encuesta, Pregunta Escala, Pregunta Unica, Pregunta Multiple, Pregunta Libre);

    private static Escenario Publicada(bool anonima = false, bool unica = false, bool obligatoriaLibre = false)
    {
        var e = EncuestaAggregate.Crear(Guid.NewGuid(), "Satisfacción", null, Ahora);
        var escala = e.AgregarPregunta("¿Cómo calificas?", TipoPregunta.Escala1a5, true, null, Ahora);
        var unicaP = e.AgregarPregunta("¿Color?", TipoPregunta.OpcionUnica, false, ["Rojo", "Azul"], Ahora);
        var multiple = e.AgregarPregunta("¿Frutas?", TipoPregunta.OpcionMultiple, false, ["Pera", "Uva", "Kiwi"], Ahora);
        var libre = e.AgregarPregunta("¿Qué mejorarías?", TipoPregunta.TextoLibre, obligatoriaLibre, null, Ahora);
        e.Publicar(Ahora.AddDays(7), anonima, unica, Ahora);
        return new Escenario(e, escala, unicaP, multiple, libre);
    }

    private static RespuestaPregunta R(Pregunta p, params string[] valores) => new(p.Id, valores);

    // HU-03 Escenario: Enviar respuestas válidas
    [Fact]
    [Trait("Story", "HU-03")]
    public void Registrar_ConRespuestasValidas_GuardaUnItemPorValor()
    {
        var s = Publicada();

        var respuesta = RespuestaEncuesta.Registrar(
            s.Encuesta,
            [R(s.Escala, "4"), R(s.Unica, s.Unica.Opciones[1].Id.ToString()),
             R(s.Multiple, s.Multiple.Opciones[0].Id.ToString(), s.Multiple.Opciones[2].Id.ToString()),
             R(s.Libre, "  Más rápido  ")],
            null, null, Ahora);

        respuesta.Items.Should().HaveCount(5);
        respuesta.Items.Single(i => i.PreguntaId == s.Libre.Id).Valor.Should().Be("Más rápido");
        respuesta.EncuestaId.Should().Be(s.Encuesta.Id);
        respuesta.DomainEvents.OfType<RespuestaRegistrada>().Should().ContainSingle();
    }

    // HU-03 Escenario: Faltan preguntas obligatorias
    [Theory]
    [Trait("Story", "HU-03")]
    [InlineData(false)]
    [InlineData(true)]
    public void Registrar_SinResponderObligatoria_Falla(bool enviarVacia)
    {
        var s = Publicada();
        RespuestaPregunta[] respuestas = enviarVacia ? [R(s.Escala, "  ")] : [];

        var act = () => RespuestaEncuesta.Registrar(s.Encuesta, respuestas, null, null, Ahora);

        act.Should().Throw<DomainValidationException>().WithMessage("Responde las preguntas obligatorias");
    }

    // HU-03 Escenario: las preguntas opcionales pueden omitirse
    [Fact]
    [Trait("Story", "HU-03")]
    public void Registrar_OmitiendoOpcionales_Funciona()
    {
        var s = Publicada();

        var respuesta = RespuestaEncuesta.Registrar(s.Encuesta, [R(s.Escala, "5")], null, null, Ahora);

        respuesta.Items.Should().ContainSingle();
    }

    // HU-03 Escenario: Encuesta cerrada
    [Fact]
    [Trait("Story", "HU-03")]
    public void Registrar_EnEncuestaCerrada_FallaConMensajeDeNoDisponible()
    {
        var s = Publicada();
        s.Encuesta.Cerrar(Ahora);

        var act = () => RespuestaEncuesta.Registrar(s.Encuesta, [R(s.Escala, "3")], null, null, Ahora);

        act.Should().Throw<EncuestaNoDisponibleException>().WithMessage("Esta encuesta ya no acepta respuestas");
    }

    // HU-03 Escenario: Encuesta con fecha límite vencida
    [Fact]
    [Trait("Story", "HU-03")]
    public void Registrar_ConPlazoVencido_Falla()
    {
        var s = Publicada();

        var act = () => RespuestaEncuesta.Registrar(s.Encuesta, [R(s.Escala, "3")], null, null, Ahora.AddDays(7));

        act.Should().Throw<EncuestaNoDisponibleException>();
    }

    // HU-03 Escenario: Encuesta anónima no guarda identidad
    [Fact]
    [Trait("Story", "HU-03")]
    public void Registrar_EnEncuestaAnonima_NoGuardaParticipante()
    {
        var s = Publicada(anonima: true);

        var respuesta = RespuestaEncuesta.Registrar(s.Encuesta, [R(s.Escala, "3")], Guid.NewGuid(), null, Ahora);

        respuesta.ParticipanteId.Should().BeNull();
    }

    [Fact]
    [Trait("Story", "HU-03")]
    public void Registrar_EnEncuestaIdentificada_GuardaParticipante()
    {
        var s = Publicada(anonima: false);
        var participante = Guid.NewGuid();

        var respuesta = RespuestaEncuesta.Registrar(s.Encuesta, [R(s.Escala, "3")], participante, null, Ahora);

        respuesta.ParticipanteId.Should().Be(participante);
    }

    // HU-03 Escenario: Respuesta duplicada en encuesta de respuesta única (identificación del participante)
    [Fact]
    [Trait("Story", "HU-03")]
    public void Registrar_ConRespuestaUnicaSinHuella_Falla()
    {
        var s = Publicada(unica: true);

        var act = () => RespuestaEncuesta.Registrar(s.Encuesta, [R(s.Escala, "3")], null, null, Ahora);

        act.Should().Throw<DomainValidationException>().WithMessage("Se requiere identificar al participante");
    }

    [Fact]
    [Trait("Story", "HU-03")]
    public void Registrar_ConRespuestaUnica_ConservaLaHuellaYSinRespuestaUnicaLaDescarta()
    {
        var unica = Publicada(unica: true);
        var libre = Publicada(unica: false);

        RespuestaEncuesta.Registrar(unica.Encuesta, [R(unica.Escala, "3")], null, "abc", Ahora).Huella.Should().Be("abc");
        RespuestaEncuesta.Registrar(libre.Encuesta, [R(libre.Escala, "3")], null, "abc", Ahora).Huella.Should().BeNull();
    }

    // HU-03: validación de valores según el tipo de pregunta
    [Theory]
    [Trait("Story", "HU-03")]
    [InlineData("0")]
    [InlineData("6")]
    [InlineData("tres")]
    public void Registrar_EscalaFueraDeRango_Falla(string valor)
    {
        var s = Publicada();

        var act = () => RespuestaEncuesta.Registrar(s.Encuesta, [R(s.Escala, valor)], null, null, Ahora);

        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    [Trait("Story", "HU-03")]
    public void Registrar_OpcionUnicaConOpcionAjena_Falla()
    {
        var s = Publicada();

        var act = () => RespuestaEncuesta.Registrar(
            s.Encuesta, [R(s.Escala, "3"), R(s.Unica, Guid.NewGuid().ToString())], null, null, Ahora);

        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    [Trait("Story", "HU-03")]
    public void Registrar_OpcionUnicaConDosValores_Falla()
    {
        var s = Publicada();
        var ids = s.Unica.Opciones.Select(o => o.Id.ToString()).ToArray();

        var act = () => RespuestaEncuesta.Registrar(s.Encuesta, [R(s.Escala, "3"), R(s.Unica, ids)], null, null, Ahora);

        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    [Trait("Story", "HU-03")]
    public void Registrar_PreguntaRepetidaOAjena_Falla()
    {
        var s = Publicada();

        var repetida = () => RespuestaEncuesta.Registrar(s.Encuesta, [R(s.Escala, "3"), R(s.Escala, "4")], null, null, Ahora);
        var ajena = () => RespuestaEncuesta.Registrar(
            s.Encuesta, [R(s.Escala, "3"), new RespuestaPregunta(Guid.NewGuid(), ["x"])], null, null, Ahora);

        repetida.Should().Throw<DomainValidationException>();
        ajena.Should().Throw<DomainValidationException>();
    }
}
