using System.Security.Claims;

namespace FabronService.Controller.Routes;

public static class ClaimsPrincipalExt
{
    public static string? GetClientId(this ClaimsPrincipal principal) => principal.FindFirst("client_id")?.Value;

}
