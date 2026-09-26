using Encuesta.Application.Common;
using Encuesta.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Encuesta.Api;

/// <summary>Traduce excepciones a respuestas RFC 7807 (application/problem+json).</summary>
internal sealed class GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    private const string BaseType = "https://encuesta.local/problems/";

    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var (status, slug, title) = exception switch
        {
            ValidationException => (422, "validation-error", "Error de validación"),
            DomainValidationException => (422, "business-rule-violation", "Regla de negocio incumplida"),
            DomainConflictException => (409, "conflict", "Conflicto de estado"),
            NotFoundException => (404, "not-found", "Recurso no encontrado"),
            ForbiddenException => (403, "forbidden", "Acceso denegado"),
            BadHttpRequestException => (400, "bad-request", "Solicitud mal formada"),
            _ => (500, "internal-error", "Error interno")
        };

        if (status == 500)
            logger.LogError(exception, "Error no controlado");

        var problem = new ProblemDetails
        {
            Type = BaseType + slug,
            Title = title,
            Status = status,
            Detail = status == 500 ? null : exception is ValidationException ? null : exception.Message,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;

        if (exception is ValidationException ve)
            problem.Extensions["errors"] = ve.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
