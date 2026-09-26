using System.Security.Claims;
using Encuesta.Application.Abstractions;

namespace Encuesta.Api;

internal sealed class HttpContextCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public Guid UserId =>
        Guid.TryParse(Principal?.FindFirstValue("sub") ?? Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : Guid.Empty;

    public bool IsAdmin => Principal?.IsInRole("admin") ?? false;
}
