namespace Encuesta.Domain.Entities;

public sealed class OpcionPregunta(Guid id, string texto, int orden)
{
    public Guid Id { get; private set; } = id;
    public string Texto { get; private set; } = texto;
    public int Orden { get; private set; } = orden;
}
