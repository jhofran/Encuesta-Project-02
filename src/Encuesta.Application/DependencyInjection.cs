using Encuesta.Application.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Encuesta.Application;

public static class DependencyInjection
{
    /// <param name="mediatRLicenseKey">Clave de licencia de MediatR (v13+); opcional en desarrollo.</param>
    public static IServiceCollection AddApplication(this IServiceCollection services, string? mediatRLicenseKey = null)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = mediatRLicenseKey;
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        services.AddSingleton(TimeProvider.System);

        return services;
    }
}
