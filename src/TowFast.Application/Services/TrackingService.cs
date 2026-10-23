using TowFast.Application.Abstractions;
using TowFast.Application.Common;
using TowFast.Application.Contracts;
using TowFast.Domain.Identity;
using TowFast.Domain.ServiceRequests;

namespace TowFast.Application.Services;

public sealed class TrackingService(ITowFastStore store, IClock clock, TrackingPolicy policy)
{
    public Task<TrackingResponse> PublishAsync(ActorContext actor, Guid requestId, PublishPositionCommand command, CancellationToken ct)
    {
        if (actor.Role != UserRole.Driver || !actor.DriverId.HasValue) throw new ForbiddenException("Driver access is required.");
        return store.InTransactionAsync(async token =>
        {
            var request = await store.FindAsync<ServiceRequest>(requestId, token) ?? throw new NotFoundException("Service request was not found.");
            if (request.DriverId != actor.DriverId) throw new ForbiddenException("Only the assigned driver can publish a position.");
            if (request.Status is not (ServiceRequestStatus.EnRoute or ServiceRequestStatus.InProgress))
                throw new ConflictException("Tracking is not active for this request.");
            if (command.CapturedAt > clock.UtcNow.Add(policy.FutureTolerance) ||
                command.CapturedAt < clock.UtcNow.Subtract(policy.MaxSampleAge))
                throw new ValidationException("CapturedAt is outside the accepted window.");
            var assignment = request.Assignments.LastOrDefault(x => x.Status == AssignmentStatus.Accepted)
                ?? throw new ConflictException("The request has no active assignment.");
            var tracking = await store.FindAsync<ServiceRequestTracking>(requestId, token);
            if (tracking is null)
            {
                tracking = new ServiceRequestTracking(request.Id, command.SourceEventId, assignment.Id, actor.DriverId.Value,
                    command.Latitude, command.Longitude, command.CapturedAt, clock.UtcNow, command.AccuracyMeters,
                    command.SpeedKmh, command.HeadingDegrees);
                await store.AddAsync(tracking, token);
            }
            else
            {
                tracking.TryUpdate(command.SourceEventId, assignment.Id, actor.DriverId.Value, command.Latitude,
                    command.Longitude, command.CapturedAt, clock.UtcNow, command.AccuracyMeters,
                    command.SpeedKmh, command.HeadingDegrees);
            }
            await store.SaveChangesAsync(token);
            return Map(tracking);
        }, ct);
    }

    public async Task<TrackingResponse> GetLatestAsync(ActorContext actor, Guid requestId, CancellationToken ct)
    {
        var request = await store.FindAsync<ServiceRequest>(requestId, ct) ?? throw new NotFoundException("Service request was not found.");
        if (request.Status is not (ServiceRequestStatus.EnRoute or ServiceRequestStatus.InProgress))
            throw new ConflictException("Tracking is not active for this request.");
        var allowed = actor.IsOperation ||
                      (actor.Role == UserRole.Client && actor.ClientId == request.ClientId) ||
                      (actor.Role == UserRole.Provider && actor.ProviderId == request.ProviderId) ||
                      (actor.Role == UserRole.Driver && actor.DriverId == request.DriverId);
        if (!allowed) throw new NotFoundException("Service request was not found.");
        var tracking = await store.FindAsync<ServiceRequestTracking>(requestId, ct) ?? throw new NotFoundException("No position is available.");
        return Map(tracking);
    }

    private TrackingResponse Map(ServiceRequestTracking x) => new(x.ServiceRequestId, x.Latitude, x.Longitude,
        x.CapturedAt, x.ReceivedAt, clock.UtcNow - x.CapturedAt > policy.StaleAfter, x.AccuracyMeters,
        x.SpeedKmh, x.HeadingDegrees);
}