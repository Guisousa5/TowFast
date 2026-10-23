using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TowFast.Api.Security;
using TowFast.Application.Common;
using TowFast.Application.Contracts;
using TowFast.Application.Services;
using TowFast.Domain.Identity;

namespace TowFast.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/providers")]
public sealed class ProvidersController(CatalogService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Admin,Operator")]
    public Task<IReadOnlyList<ProviderResponse>> List(CancellationToken ct) => service.ListProvidersAsync(ct);

    [HttpPost]
    [Authorize(Roles = "Admin,Operator")]
    [ProducesResponseType<ProviderResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProviderResponse>> Create(ProviderCreateCommand command, CancellationToken ct)
    {
        var response = await service.CreateProviderAsync(command, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPatch("{id:guid}/active")]
    [Authorize(Roles = "Admin,Operator")]
    public Task<ProviderResponse> SetActive(Guid id, [FromBody] ActiveCommand command, CancellationToken ct) =>
        service.SetProviderActiveAsync(id, command.IsActive, ct);

    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Admin,Operator")]
    public Task<ProviderResponse> Update(Guid id, ProviderUpdateCommand command, CancellationToken ct) =>
        service.UpdateProviderAsync(id, command, ct);

    [HttpGet("{providerId:guid}/drivers")]
    public Task<IReadOnlyList<DriverResponse>> ListDrivers(Guid providerId, CancellationToken ct)
    {
        EnsureProviderScope(providerId, write: false);
        return service.ListDriversAsync(providerId, ct);
    }

    [HttpPost("{providerId:guid}/drivers")]
    [ProducesResponseType<DriverResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<DriverResponse>> AddDriver(Guid providerId, DriverCreateCommand command, CancellationToken ct)
    {
        EnsureProviderScope(providerId, write: true);
        var response = await service.AddDriverAsync(providerId, command, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPatch("{providerId:guid}/drivers/{driverId:guid}")]
    public Task<DriverResponse> UpdateDriver(Guid providerId, Guid driverId, DriverUpdateCommand command, CancellationToken ct)
    {
        EnsureProviderScope(providerId, write: true);
        return service.UpdateDriverAsync(providerId, driverId, command, ct);
    }

    [HttpGet("{providerId:guid}/vehicles")]
    public Task<IReadOnlyList<ProviderVehicleResponse>> ListVehicles(Guid providerId, CancellationToken ct)
    {
        EnsureProviderScope(providerId, write: false);
        return service.ListProviderVehiclesAsync(providerId, ct);
    }

    [HttpPost("{providerId:guid}/vehicles")]
    [ProducesResponseType<ProviderVehicleResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProviderVehicleResponse>> AddVehicle(Guid providerId, ProviderVehicleCreateCommand command, CancellationToken ct)
    {
        EnsureProviderScope(providerId, write: true);
        var response = await service.AddProviderVehicleAsync(providerId, command, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPatch("{providerId:guid}/vehicles/{vehicleId:guid}")]
    public Task<ProviderVehicleResponse> UpdateVehicle(Guid providerId, Guid vehicleId,
        ProviderVehicleUpdateCommand command, CancellationToken ct)
    {
        EnsureProviderScope(providerId, write: true);
        return service.UpdateProviderVehicleAsync(providerId, vehicleId, command, ct);
    }

    [HttpGet("{providerId:guid}/service-types")]
    public Task<IReadOnlyList<ServiceTypeResponse>> ListServices(Guid providerId, CancellationToken ct)
    {
        EnsureProviderScope(providerId, write: false);
        return service.ListProviderServicesAsync(providerId, ct);
    }

    [HttpPut("{providerId:guid}/service-types")]
    public async Task<IActionResult> SetServices(Guid providerId, SetProviderServicesCommand command, CancellationToken ct)
    {
        EnsureProviderScope(providerId, write: true);
        await service.SetProviderServicesAsync(providerId, command, ct);
        return NoContent();
    }

    private void EnsureProviderScope(Guid providerId, bool write)
    {
        var actor = User.ToActor();
        if (actor.IsOperation) return;
        var ownsProvider = actor.ProviderId == providerId &&
                           (actor.Role == UserRole.Provider || (!write && actor.Role == UserRole.Driver));
        if (!ownsProvider) throw new ForbiddenException("Access denied.");
    }
}

public sealed record ActiveCommand(bool IsActive);
