using Encuesta.Application.Abstractions;
using Encuesta.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Encuesta.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<EncuestaDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("SqlServer")));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<EncuestaDbContext>());
        services.AddScoped<IEncuestaRepository, EncuestaRepository>();
        services.AddScoped<IRespuestaRepository, RespuestaRepository>();

        // Redis es solo caché (nunca fuente de verdad); sin cadena de conexión se usa memoria local.
        if (configuration.GetConnectionString("Redis") is { Length: > 0 } redis)
            services.AddStackExchangeRedisCache(o => o.Configuration = redis);
        else
            services.AddDistributedMemoryCache();

        return services;
    }
}
