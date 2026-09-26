using System.Security.Cryptography;
using Encuesta.Domain.Common;
using Encuesta.Domain.Enums;
using Encuesta.Domain.Events;
using Encuesta.Domain.Exceptions;

namespace Encuesta.Domain.Entities;

/// <summary>Agregado raíz. Toda modificación de preguntas y de estado pasa por esta clase.</summary>
public sealed class Encuesta(Guid id, Guid creadorId, string titulo, string? descripcion, DateTimeOffset creadaEn)
    : AggregateRoot
{
    public const int MaxTitulo = 200;
    public const int MaxDescripcion = 2000;
    public const int MaxPreguntas = 100;
    public const int MaxTextoPregunta = 500;
    public const int MaxOpciones = 20;
    public const int MaxTextoOpcion = 200;
    private const int TokenBytes = 16;
    public static readonly TimeSpan UmbralPorVencer = TimeSpan.FromHours(24);

    private readonly List<Pregunta> _preguntas = [];

    public Guid Id { get; private set; } = id;
    public Guid CreadorId { get; private set; } = creadorId;
    public string Titulo { get; private set; } = titulo;
    public string? Descripcion { get; private set; } = descripcion;
    public EstadoEncuesta Estado { get; private set; } = EstadoEncuesta.Borrador;
    public bool EsAnonima { get; private set; }
    public bool RespuestaUnica { get; private set; }
    public string? Token { get; private set; }
    public DateTimeOffset? FechaLimite { get; private set; }
    public DateTimeOffset CreadaEn { get; private set; } = creadaEn;
    public IReadOnlyList<Pregunta> Preguntas => _preguntas;

    public static Encuesta Crear(Guid creadorId, string titulo, string? descripcion, DateTimeOffset ahora)
    {
        if (creadorId == Guid.Empty)
            throw new DomainValidationException("El creador es obligatorio");
        if (string.IsNullOrWhiteSpace(titulo))
            throw new DomainValidationException("El título es obligatorio");
        if (titulo.Length > MaxTitulo)
            throw new DomainValidationException($"El título no puede superar {MaxTitulo} caracteres");
        if (descripcion is { Length: > MaxDescripcion })
            throw new DomainValidationException($"La descripción no puede superar {MaxDescripcion} caracteres");

        var encuesta = new Encuesta(Guid.NewGuid(), creadorId, titulo.Trim(), descripcion, ahora);
        encuesta.Raise(new EncuestaCreada(encuesta.Id, creadorId, ahora));
        return encuesta;
    }

    public Pregunta AgregarPregunta(
        string texto, TipoPregunta tipo, bool esObligatoria, IReadOnlyCollection<string>? opciones, DateTimeOffset ahora)
    {
        ExigirEstado(EstadoEncuesta.Borrador, "La encuesta ya está publicada");
        if (_preguntas.Count >= MaxPreguntas)
            throw new DomainValidationException($"Máximo {MaxPreguntas} preguntas por encuesta");
        if (string.IsNullOrWhiteSpace(texto) || texto.Length > MaxTextoPregunta)
            throw new DomainValidationException($"El texto de la pregunta es obligatorio (máx. {MaxTextoPregunta})");

        var lista = opciones ?? [];
        ValidarOpciones(tipo, lista);

        var pregunta = new Pregunta(Guid.NewGuid(), texto.Trim(), tipo, esObligatoria, _preguntas.Count);
        foreach (var opcion in lista)
            pregunta.AgregarOpcion(opcion.Trim());

        _preguntas.Add(pregunta);
        Raise(new PreguntaAgregada(Id, pregunta.Id, ahora));
        return pregunta;
    }

    public void Publicar(DateTimeOffset fechaLimite, bool esAnonima, bool respuestaUnica, DateTimeOffset ahora)
    {
        ExigirEstado(EstadoEncuesta.Borrador, "La encuesta ya está publicada");
        if (_preguntas.Count == 0)
            throw new DomainValidationException("La encuesta debe tener al menos una pregunta");
        if (fechaLimite <= ahora)
            throw new DomainValidationException("La fecha límite debe ser futura");

        Estado = EstadoEncuesta.Publicada;
        FechaLimite = fechaLimite;
        EsAnonima = esAnonima;
        RespuestaUnica = respuestaUnica;
        Token = Convert.ToHexString(RandomNumberGenerator.GetBytes(TokenBytes));
        Raise(new EncuestaPublicada(Id, Token, fechaLimite, ahora));
    }

    public void Cerrar(DateTimeOffset ahora) => CerrarPor(MotivoCierre.Manual, ahora);

    /// <summary>Cierre automático (HU-04). Devuelve true si la encuesta se cerró.</summary>
    public bool CerrarSiVencida(DateTimeOffset ahora)
    {
        if (Estado != EstadoEncuesta.Publicada || EvaluarSla(ahora) != SLAStatus.Vencido)
            return false;

        CerrarPor(MotivoCierre.PlazoVencido, ahora);
        return true;
    }

    public bool AceptaRespuestas(DateTimeOffset ahora) =>
        Estado == EstadoEncuesta.Publicada && EvaluarSla(ahora) != SLAStatus.Vencido;

    public void Asignar(Guid nuevoResponsableId)
    {
        if (nuevoResponsableId == Guid.Empty)
            throw new DomainValidationException("El responsable es obligatorio");
        if (Estado == EstadoEncuesta.Cerrada)
            throw new DomainConflictException("Una encuesta cerrada no puede reasignarse");

        CreadorId = nuevoResponsableId;
    }

    public SLAStatus EvaluarSla(DateTimeOffset ahora)
    {
        if (Estado == EstadoEncuesta.Borrador || FechaLimite is not { } limite)
            return SLAStatus.SinPlazo;
        if (ahora >= limite)
            return SLAStatus.Vencido;
        return limite - ahora <= UmbralPorVencer ? SLAStatus.PorVencer : SLAStatus.Vigente;
    }

    private void CerrarPor(MotivoCierre motivo, DateTimeOffset ahora)
    {
        ExigirEstado(EstadoEncuesta.Publicada, "Solo se pueden cerrar encuestas publicadas");

        Estado = EstadoEncuesta.Cerrada;
        Raise(new EncuestaCerrada(Id, motivo, ahora));
    }

    private void ExigirEstado(EstadoEncuesta esperado, string mensaje)
    {
        if (Estado != esperado)
            throw new DomainConflictException(mensaje);
    }

    private static void ValidarOpciones(TipoPregunta tipo, IReadOnlyCollection<string> opciones)
    {
        var conOpciones = tipo is TipoPregunta.OpcionUnica or TipoPregunta.OpcionMultiple;
        if (conOpciones && opciones.Count < 2)
            throw new DomainValidationException("Se requieren al menos 2 opciones");
        if (!conOpciones && opciones.Count > 0)
            throw new DomainValidationException($"El tipo {tipo} no admite opciones");
        if (opciones.Count > MaxOpciones || opciones.Any(o => string.IsNullOrWhiteSpace(o) || o.Length > MaxTextoOpcion))
            throw new DomainValidationException($"Opciones inválidas (máx. {MaxOpciones}, texto máx. {MaxTextoOpcion})");
    }
}
