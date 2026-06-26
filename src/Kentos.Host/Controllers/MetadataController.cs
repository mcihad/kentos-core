using Asp.Versioning;
using Kentos.Infrastructure.Modules;
using Kentos.SharedKernel.Modules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Kentos.Host.Controllers;

/// <summary>Exposes the license-enabled module manifests.</summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/metadata")]
public sealed class MetadataController : ControllerBase
{
    private readonly ModuleRegistry _registry;

    public MetadataController(ModuleRegistry registry) => _registry = registry;

    [HttpGet]
    [AllowAnonymous]
    [EndpointSummary("Etkin modülleri listele")]
    public IReadOnlyList<ModuleManifest> Get() => _registry.GetManifests();

    [HttpGet("{slug}")]
    [AllowAnonymous]
    [EndpointSummary("Modül manifestini getir")]
    [ProducesResponseType<ModuleManifest>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ModuleManifest> GetBySlug(string slug)
    {
        var manifest = _registry.GetManifest(slug);
        return manifest is null ? NotFound() : manifest;
    }
}
