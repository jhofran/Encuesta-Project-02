using Encuesta.Domain.Enums;
using EncuestaAggregate = Encuesta.Domain.Entities.Encuesta;

namespace Encuesta.Application.Encuestas;

public sealed record OpcionResponse(Guid Id, string Texto, int Orden);

public sealed record PreguntaResponse(
    Guid Id, string Texto, TipoPregunta Tipo, bool EsObligatoria, int Orden, IReadOnlyList<OpcionResponse> Opciones);

public sealed record EncuestaResponse(
    Guid Id,
    Guid CreadorId,
    string Titulo,
    string? Descripcion,
    EstadoEncuesta Estado,
    SLAStatus SlaStatus,
    bool EsAnonima,
    bool RespuestaUnica,
    string? Token,
    DateTimeOffset? FechaLimite,
    IReadOnlyList<PreguntaResponse> Preguntas,
    DateTimeOffset CreadaEn)
{
    public static EncuestaResponse From(EncuestaAggregate e, DateTimeOffset ahora) => new(
        e.Id,
        e.CreadorId,
        e.Titulo,
        e.Descripcion,
        e.Estado,
        e.EvaluarSla(ahora),
        e.EsAnonima,
        e.RespuestaUnica,
        e.Token,
        e.FechaLimite,
        PreguntasOrdenadas(e),
        e.CreadaEn);

    /// <summary>Las claves son GUID: el orden de la base de datos no es el de la encuesta, se ordena por `Orden`.</summary>
    internal static IReadOnlyList<PreguntaResponse> PreguntasOrdenadas(EncuestaAggregate e) =>
    [
        .. e.Preguntas.OrderBy(p => p.Orden).Select(p => new PreguntaResponse(
            p.Id, p.Texto, p.Tipo, p.EsObligatoria, p.Orden,
            [.. p.Opciones.OrderBy(o => o.Orden).Select(o => new OpcionResponse(o.Id, o.Texto, o.Orden))]))
    ];
}
