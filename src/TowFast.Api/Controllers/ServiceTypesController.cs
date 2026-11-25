using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TowFast.Application.Contracts;
using TowFast.Application.Services;

namespace TowFast.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/service-types")]
public sealed class ServiceTypesController(CatalogService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ServiceTypeResponse>> List(CancellationToken ct) => service.ListServiceTypesAsync(ct);

    [HttpPatch("{id:guid}/active")]
    [Authorize(Roles = "Admin,Operator")]
    public Task<ServiceTypeResponse> SetActive(Guid id, ActiveCommand command, CancellationToken ct) =>
        service.SetServiceTypeActiveAsync(id, command.IsActive, ct);

    [HttpPatch("{id:guid}/pricing")]
    [Authorize(Roles = "Admin,Operator")]
    public Task<ServiceTypeResponse> ConfigurePricing(Guid id, ConfigureServicePriceCommand command,
        CancellationToken ct) => service.ConfigureServicePriceAsync(id, command, ct);
}
