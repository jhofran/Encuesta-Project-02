using Encuesta.Domain.Common;
using Encuesta.Domain.Enums;
using Encuesta.Domain.Events;
using Encuesta.Domain.Exceptions;

namespace Encuesta.Domain.Entities;

/// <summary>Valores enviados por el participante para una pregunta.</summary>
public sealed record RespuestaPregunta(Guid PreguntaId, IReadOnlyList<string> Valores);

/// <summary>
/// Agregado independiente de <see cref="Encuesta"/> (referencia solo por id) para no bloquear
/// la encuesta ante escrituras concurrentes.
/// </summary>
public sealed class RespuestaEncuesta(Guid id, Guid encuestaId, Guid? participanteId, string? huella, DateTimeOffset enviadaEn)
    : AggregateRoot
{
    public const int MaxTextoLibre = 2000;

    private readonly List<ItemRespuesta> _items = [];

    public Guid Id { get; private set; } = id;
    public Guid EncuestaId { get; private set; } = encuestaId;

    /// <summary>Solo se guarda en encuestas no anónimas y con participante autenticado (RN-06).</summary>
    public Guid? ParticipanteId { get; private set; } = participanteId;

    /// <summary>Hash de un token aleatorio del navegador; solo en encuestas de respuesta única (RF-05).</summary>
    public string? Huella { get; private set; } = huella;

    public DateTimeOffset EnviadaEn { get; private set; } = enviadaEn;
    public IReadOnlyList<ItemRespuesta> Items => _items;

    public static RespuestaEncuesta Registrar(
        Encuesta encuesta,
        IReadOnlyCollection<RespuestaPregunta> respuestas,
        Guid? participanteId,
        string? huella,
        DateTimeOffset ahora)
    {
        if (!encuesta.AceptaRespuestas(ahora))
            throw new EncuestaNoDisponibleException();
        if (encuesta.RespuestaUnica && string.IsNullOrWhiteSpace(huella))
            throw new DomainValidationException("Se requiere identificar al participante");

        var porPregunta = IndexarRespuestas(encuesta, respuestas);

        var respuesta = new RespuestaEncuesta(
            Guid.NewGuid(),
            encuesta.Id,
            encuesta.EsAnonima ? null : participanteId,
            encuesta.RespuestaUnica ? huella : null,
            ahora);

        foreach (var pregunta in encuesta.Preguntas)
        {
            var valores = porPregunta.GetValueOrDefault(pregunta.Id, []);
            if (valores.Count == 0)
            {
                if (pregunta.EsObligatoria)
                    throw new DomainValidationException("Responde las preguntas obligatorias");
                continue;
            }

            ValidarValores(pregunta, valores);
            respuesta._items.AddRange(valores.Select(v => new ItemRespuesta(Guid.NewGuid(), pregunta.Id, v)));
        }

        respuesta.Raise(new RespuestaRegistrada(respuesta.Id, encuesta.Id, ahora));
        return respuesta;
    }

    /// <summary>Descarta valores en blanco y rechaza preguntas repetidas o ajenas a la encuesta.</summary>
    private static Dictionary<Guid, List<string>> IndexarRespuestas(
        Encuesta encuesta, IReadOnlyCollection<RespuestaPregunta> respuestas)
    {
        var validas = encuesta.Preguntas.Select(p => p.Id).ToHashSet();
        var resultado = new Dictionary<Guid, List<string>>();

        foreach (var r in respuestas)
        {
            if (!validas.Contains(r.PreguntaId))
                throw new DomainValidationException("La respuesta incluye una pregunta que no pertenece a la encuesta");
            if (!resultado.TryAdd(r.PreguntaId, [.. r.Valores.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim())]))
                throw new DomainValidationException("La respuesta repite una pregunta");
        }

        return resultado;
    }

    private static void ValidarValores(Pregunta pregunta, List<string> valores)
    {
        var valido = pregunta.Tipo switch
        {
            TipoPregunta.OpcionUnica => valores.Count == 1 && EsOpcion(pregunta, valores[0]),
            TipoPregunta.OpcionMultiple =>
                valores.Distinct().Count() == valores.Count && valores.All(v => EsOpcion(pregunta, v)),
            TipoPregunta.TextoLibre => valores.Count == 1 && valores[0].Length <= MaxTextoLibre,
            TipoPregunta.Escala1a5 => valores.Count == 1 && int.TryParse(valores[0], out var n) && n is >= 1 and <= 5,
            _ => false,
        };

        if (!valido)
            throw new DomainValidationException($"Respuesta inválida para la pregunta \"{pregunta.Texto}\"");
    }

    private static bool EsOpcion(Pregunta pregunta, string valor) =>
        Guid.TryParse(valor, out var id) && pregunta.Opciones.Any(o => o.Id == id);
}
