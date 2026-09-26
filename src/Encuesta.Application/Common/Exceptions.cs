namespace Encuesta.Application.Common;

public sealed class NotFoundException(string message) : Exception(message);

public sealed class ForbiddenException(string message) : Exception(message);
