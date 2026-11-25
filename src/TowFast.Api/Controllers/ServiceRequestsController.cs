using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TowFast.Api.Security;
using TowFast.Application.Common;
using TowFast.Application.Contracts;
using TowFast.Application.Services;
using TowFast.Domain.ServiceRequests;

namespace TowFast.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/service-requests")]
public sealed class ServiceRequestsController(ServiceRequestService service) : ControllerBase
{
    [HttpPost("quote")]
    [Authorize(Roles = "Admin,Operator,Client")]
    public Task<PriceQuoteResponse> Quote(CreateServiceRequestCommand command, CancellationToken ct) =>
        service.QuoteAsync(User.ToActor(), command, ct);

    [HttpPost]
    [Authorize(Roles = "Admin,Operator,Client")]
    [ProducesResponseType<ServiceRequestResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ServiceRequestResponse>> Create(CreateServiceRequestCommand command, CancellationToken ct)
    {
        var response = await service.CreateAsync(User.ToActor(), command, ct);
        return CreatedAtAction(nameof(Get), new { publicId = response.PublicId }, response);
    }

    [HttpGet]
    public Task<PageResult<ServiceRequestResponse>> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] ServiceRequestStatus? status = null, [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null, [FromQuery] Guid? serviceTypeId = null,
        [FromQuery] Guid? clientId = null, [FromQuery] Guid? providerId = null,
        [FromQuery] Guid? driverId = null, CancellationToken ct = default) =>
        service.ListAsync(User.ToActor(), page, pageSize,
            new ServiceRequestFilter(status, from, to, serviceTypeId, clientId, providerId, driverId), ct);

    [HttpGet("available")]
    [Authorize(Roles = "Driver")]
    public Task<PageResult<AvailableRequestResponse>> Available([FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        service.ListAvailableAsync(User.ToActor(), page, pageSize, ct);

    [HttpGet("{publicId}")]
    public Task<ServiceRequestDetailResponse> Get(string publicId, CancellationToken ct) =>
        service.GetAsync(User.ToActor(), publicId, ct);

    [HttpPost("{id:guid}/accept")]
    [Authorize(Roles = "Driver")]
    public Task<ServiceRequestResponse> Accept(Guid id, AcceptServiceRequestCommand command, CancellationToken ct) =>
        service.AcceptAsync(User.ToActor(), id, command, ct);

    [HttpPost("{id:guid}/release")]
    [Authorize(Roles = "Driver")]
    public Task<ServiceRequestResponse> Release(Guid id, ReasonCommand command, CancellationToken ct) =>
        service.ReleaseAsync(User.ToActor(), id, command, ct);

    [HttpPost("{id:guid}/en-route")]
    [Authorize(Roles = "Admin,Operator,Driver")]
    public Task<ServiceRequestResponse> EnRoute(Guid id, VersionCommand command, CancellationToken ct) =>
        service.EnRouteAsync(User.ToActor(), id, command, ct);

    [HttpPost("{id:guid}/start")]
    [Authorize(Roles = "Admin,Operator,Driver")]
    public Task<ServiceRequestResponse> Start(Guid id, VersionCommand command, CancellationToken ct) =>
        service.StartAsync(User.ToActor(), id, command, ct);

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "Admin,Operator,Driver")]
    public Task<ServiceRequestResponse> Complete(Guid id, CompleteServiceRequestCommand command, CancellationToken ct) =>
        service.CompleteAsync(User.ToActor(), id, command, ct);

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Admin,Operator,Client")]
    public Task<ServiceRequestResponse> Cancel(Guid id, ReasonCommand command, CancellationToken ct) =>
        service.CancelAsync(User.ToActor(), id, command, ct);
}
