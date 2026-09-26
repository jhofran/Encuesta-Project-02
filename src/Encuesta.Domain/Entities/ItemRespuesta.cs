namespace Encuesta.Domain.Entities;

/// <summary>Un valor contestado. Las preguntas de opción múltiple generan un ítem por opción marcada.</summary>
public sealed class ItemRespuesta(Guid id, Guid preguntaId, string valor)
{
    public Guid Id { get; private set; } = id;
    public Guid PreguntaId { get; private set; } = preguntaId;

    /// <summary>Id de la opción (tipos de opción), texto libre o número 1-5 (escala).</summary>
    public string Valor { get; private set; } = valor;
}
