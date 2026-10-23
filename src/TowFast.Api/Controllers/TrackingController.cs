using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TowFast.Api.Security;
using TowFast.Application.Contracts;
using TowFast.Application.Services;

namespace TowFast.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/service-requests/{requestId:guid}/tracking")]
public sealed class TrackingController(TrackingService service) : ControllerBase
{
    [HttpPost("positions")]
    [Authorize(Roles = "Driver")]
    public Task<TrackingResponse> Publish(Guid requestId, PublishPositionCommand command, CancellationToken ct) =>
        service.PublishAsync(User.ToActor(), requestId, command, ct);

    [HttpGet("latest")]
    public Task<TrackingResponse> Latest(Guid requestId, CancellationToken ct) =>
        service.GetLatestAsync(User.ToActor(), requestId, ct);
}
