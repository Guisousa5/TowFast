using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TowFast.Api.Security;
using TowFast.Application.Contracts;
using TowFast.Application.Services;

namespace TowFast.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/clients")]
public sealed class ClientsController(CatalogService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<ClientResponse>> List(CancellationToken ct) => service.ListClientsAsync(User.ToActor(), ct);

    [HttpPost]
    [Authorize(Roles = "Admin,Operator")]
    [ProducesResponseType<ClientResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ClientResponse>> Create(ClientCreateCommand command, CancellationToken ct)
    {
        var response = await service.CreateClientAsync(command, ct);
        return CreatedAtAction(nameof(List), response);
    }

    [HttpPatch("{id:guid}")]
    public Task<ClientResponse> Update(Guid id, ClientUpdateCommand command, CancellationToken ct) =>
        service.UpdateClientAsync(User.ToActor(), id, command, ct);

    [HttpGet("{clientId:guid}/vehicles")]
    public Task<IReadOnlyList<VehicleResponse>> ListVehicles(Guid clientId, CancellationToken ct) =>
        service.ListVehiclesAsync(User.ToActor(), clientId, ct);

    [HttpPost("{clientId:guid}/vehicles")]
    [ProducesResponseType<VehicleResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<VehicleResponse>> AddVehicle(Guid clientId, VehicleCreateCommand command, CancellationToken ct)
    {
        var response = await service.AddVehicleAsync(User.ToActor(), clientId, command, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPatch("{clientId:guid}/vehicles/{vehicleId:guid}/active")]
    public Task<VehicleResponse> SetVehicleActive(Guid clientId, Guid vehicleId, VehicleActiveCommand command, CancellationToken ct) =>
        service.SetClientVehicleActiveAsync(User.ToActor(), clientId, vehicleId, command.IsActive, ct);

    [HttpGet("{clientId:guid}/locations")]
    public Task<IReadOnlyList<LocationResponse>> ListLocations(Guid clientId, CancellationToken ct) =>
        service.ListLocationsAsync(User.ToActor(), clientId, ct);

    [HttpPost("{clientId:guid}/locations")]
    [ProducesResponseType<LocationResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<LocationResponse>> AddLocation(Guid clientId, LocationInput command, CancellationToken ct)
    {
        var response = await service.AddLocationAsync(User.ToActor(), clientId, command, ct);
        return StatusCode(StatusCodes.Status201Created, response);
    }
}
