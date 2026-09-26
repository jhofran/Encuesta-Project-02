using Encuesta.Domain.Enums;
using Encuesta.Domain.Events;
using Encuesta.Domain.Exceptions;
using FluentAssertions;
using EncuestaAggregate = Encuesta.Domain.Entities.Encuesta;

namespace Encuesta.UnitTests;

/// <summary>
/// Cada test enlaza con un escenario Gherkin de docs/specs/functional/02-user-stories.md
/// mediante [Trait("Story", "HU-xx")] (filtrable: dotnet test --filter Story=HU-01)
/// y un comentario "HU-xx Escenario: ...".
/// </summary>
public class EncuestaTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Creador = Guid.NewGuid();

    private static EncuestaAggregate Borrador() =>
        EncuestaAggregate.Crear(Creador, "Satisfacción del cliente", null, Ahora);

    private static EncuestaAggregate BorradorConPregunta()
    {
        var encuesta = Borrador();
        encuesta.AgregarPregunta("¿Cómo calificas el servicio?", TipoPregunta.Escala1a5, true, null, Ahora);
        return encuesta;
    }

    private static EncuestaAggregate Publicada(TimeSpan duracion)
    {
        var encuesta = BorradorConPregunta();
        encuesta.Publicar(Ahora + duracion, esAnonima: false, respuestaUnica: false, Ahora);
        return encuesta;
    }

    // ---------------------------------------------------------------- HU-01 Crear encuesta

    [Fact]
    [Trait("Story", "HU-01")]
    public void Crear_ConPreguntasValidas_QuedaEnBorrador() // HU-01 Escenario: Crear encuesta en borrador con preguntas válidas
    {
        var encuesta = Borrador();
        encuesta.AgregarPregunta("¿Cómo calificas el servicio?", TipoPregunta.Escala1a5, false, null, Ahora);
        encuesta.AgregarPregunta("¿Qué mejorarías?", TipoPregunta.TextoLibre, false, null, Ahora);

        encuesta.Estado.Should().Be(EstadoEncuesta.Borrador);
        encuesta.Preguntas.Should().HaveCount(2);
        encuesta.Preguntas.Select(p => p.Orden).Should().Equal(0, 1);
        encuesta.DomainEvents.OfType<EncuestaCreada>().Should().ContainSingle();
    }

    [Theory]
    [Trait("Story", "HU-01")]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_SinTitulo_Falla(string titulo) // HU-01 Escenario: Rechazar título vacío
    {
        var act = () => EncuestaAggregate.Crear(Creador, titulo, null, Ahora);

        act.Should().Throw<DomainValidationException>().WithMessage("El título es obligatorio");
    }

    [Fact]
    [Trait("Story", "HU-01")]
    public void AgregarPregunta_OpcionUnicaConUnaOpcion_Falla() // HU-01 Escenario: Pregunta de opción con menos de dos opciones
    {
        var encuesta = Borrador();

        var act = () => encuesta.AgregarPregunta("¿Color?", TipoPregunta.OpcionUnica, false, ["Rojo"], Ahora);

        act.Should().Throw<DomainValidationException>().WithMessage("Se requieren al menos 2 opciones");
        encuesta.Preguntas.Should().BeEmpty();
    }

    [Fact]
    [Trait("Story", "HU-01")]
    public void AgregarPregunta_OpcionMultipleConDosOpciones_ConservaOrdenDeOpciones() // HU-01 Escenario: Editar una encuesta en borrador
    {
        var encuesta = Borrador();

        var pregunta = encuesta.AgregarPregunta("¿Color?", TipoPregunta.OpcionMultiple, false, ["Rojo", "Azul"], Ahora);

        pregunta.Opciones.Select(o => o.Texto).Should().Equal("Rojo", "Azul");
    }

    [Fact]
    [Trait("Story", "HU-01")]
    public void AgregarPregunta_TextoLibreConOpciones_Falla() // HU-01 (RN-05): solo los tipos de opción admiten opciones
    {
        var encuesta = Borrador();

        var act = () => encuesta.AgregarPregunta("¿Por qué?", TipoPregunta.TextoLibre, false, ["Sí", "No"], Ahora);

        act.Should().Throw<DomainValidationException>();
    }

    [Fact]
    [Trait("Story", "HU-01")]
    public void AgregarPregunta_EnEncuestaPublicada_FallaPorConflicto() // HU-01 Escenario: No editar preguntas de una encuesta publicada
    {
        var encuesta = Publicada(TimeSpan.FromDays(7));

        var act = () => encuesta.AgregarPregunta("Otra", TipoPregunta.TextoLibre, false, null, Ahora);

        act.Should().Throw<DomainConflictException>().WithMessage("La encuesta ya está publicada");
    }

    // ---------------------------------------------------------------- HU-02 Publicar encuesta

    [Fact]
    [Trait("Story", "HU-02")]
    public void Publicar_ConPreguntasYFechaFutura_PasaAPublicadaConToken() // HU-02 Escenario: Publicar una encuesta válida
    {
        var encuesta = BorradorConPregunta();
        var limite = Ahora.AddDays(30);

        encuesta.Publicar(limite, esAnonima: false, respuestaUnica: true, Ahora);

        encuesta.Estado.Should().Be(EstadoEncuesta.Publicada);
        encuesta.FechaLimite.Should().Be(limite);
        encuesta.Token.Should().NotBeNullOrWhiteSpace();
        encuesta.RespuestaUnica.Should().BeTrue();
        encuesta.DomainEvents.OfType<EncuestaPublicada>().Should().ContainSingle(e => e.Token == encuesta.Token);
    }

    [Fact]
    [Trait("Story", "HU-02")]
    public void Publicar_SinPreguntas_FallaYSigueEnBorrador() // HU-02 Escenario: No publicar una encuesta sin preguntas
    {
        var encuesta = Borrador();

        var act = () => encuesta.Publicar(Ahora.AddDays(1), false, false, Ahora);

        act.Should().Throw<DomainValidationException>()
            .WithMessage("La encuesta debe tener al menos una pregunta");
        encuesta.Estado.Should().Be(EstadoEncuesta.Borrador);
    }

    [Theory]
    [Trait("Story", "HU-02")]
    [InlineData(-60 * 24 * 365)]
    [InlineData(0)]
    public void Publicar_ConFechaNoFutura_Falla(int minutosDesdeAhora) // HU-02 Escenario: Fecha límite en el pasado
    {
        var encuesta = BorradorConPregunta();

        var act = () => encuesta.Publicar(Ahora.AddMinutes(minutosDesdeAhora), false, false, Ahora);

        act.Should().Throw<DomainValidationException>().WithMessage("La fecha límite debe ser futura");
        encuesta.Estado.Should().Be(EstadoEncuesta.Borrador);
    }

    [Fact]
    [Trait("Story", "HU-02")]
    public void Publicar_MarcadaComoAnonima_RegistraAnonimato() // HU-02 Escenario: Configurar encuesta anónima
    {
        var encuesta = BorradorConPregunta();

        encuesta.Publicar(Ahora.AddDays(1), esAnonima: true, respuestaUnica: false, Ahora);

        encuesta.EsAnonima.Should().BeTrue();
    }

    [Fact]
    [Trait("Story", "HU-02")]
    public void Publicar_DosVeces_FallaPorConflicto() // HU-02 (RN-02): una encuesta publicada no vuelve a publicarse
    {
        var encuesta = Publicada(TimeSpan.FromDays(1));

        var act = () => encuesta.Publicar(Ahora.AddDays(2), false, false, Ahora);

        act.Should().Throw<DomainConflictException>();
    }

    // ---------------------------------------------------------------- HU-03 Responder encuesta (regla de aceptación)

    [Fact]
    [Trait("Story", "HU-03")]
    public void AceptaRespuestas_PublicadaYVigente_EsTrue() // HU-03 Escenario: Enviar respuestas válidas
    {
        var encuesta = Publicada(TimeSpan.FromDays(7));

        encuesta.AceptaRespuestas(Ahora).Should().BeTrue();
    }

    [Fact]
    [Trait("Story", "HU-03")]
    public void AceptaRespuestas_EnBorrador_EsFalse() // HU-03 (RN-03): solo se responde una encuesta publicada
    {
        BorradorConPregunta().AceptaRespuestas(Ahora).Should().BeFalse();
    }

    [Fact]
    [Trait("Story", "HU-03")]
    public void AceptaRespuestas_Cerrada_EsFalse() // HU-03 Escenario: Encuesta cerrada
    {
        var encuesta = Publicada(TimeSpan.FromDays(7));
        encuesta.Cerrar(Ahora);

        encuesta.AceptaRespuestas(Ahora).Should().BeFalse();
    }

    [Fact]
    [Trait("Story", "HU-03")]
    public void AceptaRespuestas_PlazoVencido_EsFalse() // HU-03 Escenario: Encuesta con fecha límite vencida
    {
        var encuesta = Publicada(TimeSpan.FromDays(1));

        encuesta.AceptaRespuestas(Ahora.AddDays(1)).Should().BeFalse();
    }

    // ---------------------------------------------------------------- HU-04 Cerrar encuesta

    [Fact]
    [Trait("Story", "HU-04")]
    public void Cerrar_Publicada_PasaACerradaConMotivoManual() // HU-04 Escenario: Cierre manual
    {
        var encuesta = Publicada(TimeSpan.FromDays(7));

        encuesta.Cerrar(Ahora);

        encuesta.Estado.Should().Be(EstadoEncuesta.Cerrada);
        encuesta.DomainEvents.OfType<EncuestaCerrada>().Should().ContainSingle(e => e.Motivo == MotivoCierre.Manual);
    }

    [Fact]
    [Trait("Story", "HU-04")]
    public void CerrarSiVencida_ConPlazoVencido_CierraPorPlazo() // HU-04 Escenario: Cierre automático por fecha límite
    {
        var encuesta = Publicada(TimeSpan.FromDays(5));

        var cerrada = encuesta.CerrarSiVencida(Ahora.AddDays(5));

        cerrada.Should().BeTrue();
        encuesta.Estado.Should().Be(EstadoEncuesta.Cerrada);
        encuesta.DomainEvents.OfType<EncuestaCerrada>().Should()
            .ContainSingle(e => e.Motivo == MotivoCierre.PlazoVencido);
    }

    [Fact]
    [Trait("Story", "HU-04")]
    public void CerrarSiVencida_ConPlazoVigente_NoHaceNada() // HU-04 Escenario: Cierre automático (solo al llegar la fecha límite)
    {
        var encuesta = Publicada(TimeSpan.FromDays(5));

        var cerrada = encuesta.CerrarSiVencida(Ahora.AddDays(4));

        cerrada.Should().BeFalse();
        encuesta.Estado.Should().Be(EstadoEncuesta.Publicada);
    }

    [Fact]
    [Trait("Story", "HU-04")]
    public void CerrarSiVencida_EnBorrador_NoHaceNada() // HU-04 (RN-07): el cierre automático solo aplica a publicadas
    {
        var encuesta = BorradorConPregunta();

        encuesta.CerrarSiVencida(Ahora.AddYears(1)).Should().BeFalse();
        encuesta.Estado.Should().Be(EstadoEncuesta.Borrador);
    }

    [Fact]
    [Trait("Story", "HU-04")]
    public void Cerrar_EnBorrador_Falla() // HU-04 Escenario: No cerrar una encuesta en borrador
    {
        var encuesta = BorradorConPregunta();

        var act = () => encuesta.Cerrar(Ahora);

        act.Should().Throw<DomainConflictException>().WithMessage("Solo se pueden cerrar encuestas publicadas");
    }

    [Fact]
    [Trait("Story", "HU-04")]
    public void Cerrar_EncuestaYaCerrada_FallaYNoSeReabre() // HU-04 Escenario: No reabrir una encuesta cerrada (estado terminal, RN-07)
    {
        var encuesta = Publicada(TimeSpan.FromDays(7));
        encuesta.Cerrar(Ahora);

        var act = () => encuesta.Cerrar(Ahora);

        act.Should().Throw<DomainConflictException>();
        encuesta.Estado.Should().Be(EstadoEncuesta.Cerrada);
    }

    // ---------------------------------------------------------------- SLA (plazo de respuesta)

    [Fact]
    [Trait("Story", "HU-04")]
    public void EvaluarSla_EnBorrador_EsSinPlazo() // HU-04 SLA: sin fecha límite mientras es borrador
    {
        BorradorConPregunta().EvaluarSla(Ahora).Should().Be(SLAStatus.SinPlazo);
    }

    [Theory]
    [Trait("Story", "HU-04")]
    [InlineData(60 * 24 * 7, SLAStatus.Vigente)]   // faltan 7 días
    [InlineData(60 * 24 + 1, SLAStatus.Vigente)]   // 1 minuto por encima del umbral
    [InlineData(60 * 24, SLAStatus.PorVencer)]     // exactamente en el umbral de 24 h
    [InlineData(1, SLAStatus.PorVencer)]           // falta 1 minuto
    [InlineData(0, SLAStatus.Vencido)]             // exactamente la fecha límite
    [InlineData(-1, SLAStatus.Vencido)]            // ya pasó
    public void EvaluarSla_SegunTiempoRestante(int minutosHastaLimite, SLAStatus esperado) // HU-04 SLA: expiración del plazo
    {
        var encuesta = Publicada(TimeSpan.FromDays(30));
        var ahora = encuesta.FechaLimite!.Value.AddMinutes(-minutosHastaLimite);

        encuesta.EvaluarSla(ahora).Should().Be(esperado);
    }

    [Fact]
    [Trait("Story", "HU-04")]
    public void EvaluarSla_TrasCerrarPorVencimiento_SiguePreviendoVencido() // HU-04 SLA: el plazo se calcula, no depende del estado
    {
        var encuesta = Publicada(TimeSpan.FromDays(1));
        encuesta.CerrarSiVencida(Ahora.AddDays(2));

        encuesta.EvaluarSla(Ahora.AddDays(2)).Should().Be(SLAStatus.Vencido);
    }

    // ---------------------------------------------------------------- Asignación (contrato PUT /assign)

    [Fact]
    [Trait("Story", "API-assign")]
    public void Asignar_NuevoResponsable_ActualizaCreador() // Contrato Encuesta-v1.yaml: PUT /api/v1/Encuesta/{id}/assign
    {
        var encuesta = Borrador();
        var nuevo = Guid.NewGuid();

        encuesta.Asignar(nuevo);

        encuesta.CreadorId.Should().Be(nuevo);
    }

    [Fact]
    [Trait("Story", "API-assign")]
    public void Asignar_EncuestaCerrada_FallaPorConflicto() // Contrato Encuesta-v1.yaml: 409 al asignar una encuesta cerrada (RN-07)
    {
        var encuesta = Publicada(TimeSpan.FromDays(1));
        encuesta.Cerrar(Ahora);

        var act = () => encuesta.Asignar(Guid.NewGuid());

        act.Should().Throw<DomainConflictException>();
    }
}
