using Encuesta.Domain.Enums;

namespace Encuesta.Domain.Entities;

public sealed class Pregunta(Guid id, string texto, TipoPregunta tipo, bool esObligatoria, int orden)
{
    private readonly List<OpcionPregunta> _opciones = [];

    public Guid Id { get; private set; } = id;
    public string Texto { get; private set; } = texto;
    public TipoPregunta Tipo { get; private set; } = tipo;
    public bool EsObligatoria { get; private set; } = esObligatoria;
    public int Orden { get; private set; } = orden;
    public IReadOnlyList<OpcionPregunta> Opciones => _opciones;

    internal void AgregarOpcion(string texto) =>
        _opciones.Add(new OpcionPregunta(Guid.NewGuid(), texto, _opciones.Count));
}
