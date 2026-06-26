using Asp.Versioning;
using Kentos.Modules.Hesap.Application.Me;
using Kentos.SharedKernel.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kentos.Modules.Hesap.Api;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/hesap/me")]
[Authorize] // any authenticated user may read their own context (no specific permission)
public sealed class MeController(ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Mevcut kullanıcı bağlamı (roller + modüle göre yetkiler)")]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentUserResponse> Get()
    {
        // Group the resolved permission keys by their module slug (the first key segment),
        // e.g. "settlement.province.list" → module "settlement".
        var permissions = currentUser.Permissions
            .GroupBy(key => key.Split('.', 2)[0], StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group.OrderBy(k => k, StringComparer.Ordinal).ToList(),
                StringComparer.Ordinal);

        return new CurrentUserResponse(
            currentUser.UserId,
            currentUser.UserName,
            currentUser.Roles.ToList(),
            permissions);
    }
}
