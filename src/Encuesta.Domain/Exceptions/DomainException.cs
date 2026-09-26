namespace Encuesta.Domain.Exceptions;

public abstract class DomainException(string message) : Exception(message);

/// <summary>Regla de negocio o dato inválido (HTTP 422).</summary>
public sealed class DomainValidationException(string message) : DomainException(message);

/// <summary>Estado del agregado incompatible con la operación (HTTP 409).</summary>
public sealed class DomainConflictException(string message) : DomainException(message);

/// <summary>La encuesta existe pero ya no acepta respuestas: cerrada o con plazo vencido (HTTP 410).</summary>
public sealed class EncuestaNoDisponibleException() : DomainException("Esta encuesta ya no acepta respuestas");
