using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Encuesta.Api;

/// <summary>
/// Autenticación SOLO para desarrollo local: valida JWT firmados con una clave simétrica
/// y expone POST /dev/token para emitirlos. Nunca se registra fuera de Development.
/// </summary>
internal static class DevAuth
{
    public sealed record DevTokenRequest(Guid? UserId, bool Admin = false);

    public static TokenValidationParameters ValidationParameters(IConfiguration config) => new()
    {
        IssuerSigningKey = Key(config),
        ValidIssuer = config["DevAuth:Issuer"],
        ValidAudience = config["DevAuth:Audience"],
        RoleClaimType = "role",
        ClockSkew = TimeSpan.FromMinutes(1),
    };

    public static IEndpointRouteBuilder MapDevTokenEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/dev/token", (DevTokenRequest? request, IConfiguration config) =>
        {
            var userId = request?.UserId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");
            var claims = new List<Claim> { new("sub", userId.ToString()) };
            if (request?.Admin == true)
                claims.Add(new Claim("role", "admin"));

            var token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = config["DevAuth:Issuer"],
                Audience = config["DevAuth:Audience"],
                Expires = DateTime.UtcNow.AddHours(8),
                SigningCredentials = new SigningCredentials(Key(config), SecurityAlgorithms.HmacSha256),
            });

            return Results.Ok(new { token, userId });
        }).AllowAnonymous().ExcludeFromDescription();

        return app;
    }

    private static SymmetricSecurityKey Key(IConfiguration config) =>
        new(Encoding.UTF8.GetBytes(config["DevAuth:SigningKey"]
            ?? throw new InvalidOperationException("Falta DevAuth:SigningKey")));
}
