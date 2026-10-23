using System.Security.Claims;
using TowFast.Application.Abstractions;
using TowFast.Application.Common;
using TowFast.Domain.Identity;

namespace TowFast.Api.Security;

public static class ClaimsPrincipalExtensions
{
    public static ActorContext ToActor(this ClaimsPrincipal principal)
    {
        var subject = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleValue = principal.FindFirstValue(ClaimTypes.Role) ?? principal.FindFirstValue("role");
        if (!Guid.TryParse(subject, out var userId) || !Enum.TryParse<UserRole>(roleValue, true, out var role))
            throw new UnauthorizedException("Authentication context is invalid.");
        return new ActorContext(userId, role, ReadGuid(principal, "client_id"), ReadGuid(principal, "provider_id"), ReadGuid(principal, "driver_id"));
    }

    private static Guid? ReadGuid(ClaimsPrincipal principal, string claim) =>
        Guid.TryParse(principal.FindFirstValue(claim), out var value) ? value : null;
}
