namespace Encuesta.Domain.Enums;

public enum EstadoEncuesta
{
    Borrador,
    Publicada,
    Cerrada
}

public enum TipoPregunta
{
    OpcionUnica,
    OpcionMultiple,
    TextoLibre,
    Escala1a5
}

public enum MotivoCierre
{
    Manual,
    PlazoVencido
}

/// <summary>Estado del plazo de respuesta. Se calcula, no se persiste.</summary>
public enum SLAStatus
{
    SinPlazo,
    Vigente,
    PorVencer,
    Vencido
}
