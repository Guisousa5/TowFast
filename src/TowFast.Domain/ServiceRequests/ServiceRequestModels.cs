using TowFast.Domain.Common;

namespace TowFast.Domain.ServiceRequests;

public enum ServiceRequestStatus
{
    Pending,
    Accepted,
    EnRoute,
    InProgress,
    Completed,
    Cancelled
}

public enum AssignmentStatus
{
    Accepted,
    Released,
    Cancelled
}

public sealed class ServiceRequest : Entity
{
    private readonly List<ServiceRequestItem> _items = [];
    private readonly List<ServiceRequestStatusHistory> _history = [];
    private readonly List<ServiceRequestAssignment> _assignments = [];
    private ServiceRequest() { }

    public ServiceRequest(Guid clientId, Guid clientVehicleId, Guid serviceTypeId,
        Guid originLocationId, Guid? destinationLocationId, bool requiresDestination,
        string? customerNotes, Guid createdByUserId, DateTimeOffset now)
    {
        if (requiresDestination && destinationLocationId is null)
        {
            throw new DomainException("Destination is required for towing.");
        }

        ClientId = clientId;
        ClientVehicleId = clientVehicleId;
        ServiceTypeId = serviceTypeId;
        OriginLocationId = originLocationId;
        DestinationLocationId = requiresDestination ? destinationLocationId : null;
        CustomerNotes = customerNotes?.Trim();
        PublicId = $"TF-{now:yyyyMMdd}-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
        Status = ServiceRequestStatus.Pending;
        CreatedAt = now;
        UpdatedAt = now;
        _history.Add(new ServiceRequestStatusHistory(Id, null, Status, createdByUserId, null, now));
    }

    public string PublicId { get; private set; } = string.Empty;
    public Guid ClientId { get; private set; }
    public Guid ClientVehicleId { get; private set; }
    public Guid ServiceTypeId { get; private set; }
    public Guid OriginLocationId { get; private set; }
    public Guid? DestinationLocationId { get; private set; }
    public Guid? ProviderId { get; private set; }
    public Guid? DriverId { get; private set; }
    public Guid? ProviderVehicleId { get; private set; }
    public ServiceRequestStatus Status { get; private set; }
    public string? CustomerNotes { get; private set; }
    public string? OperatorNotes { get; private set; }
    public string? CompletionNotes { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public DateTimeOffset? EnRouteAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<ServiceRequestItem> Items => _items;
    public IReadOnlyCollection<ServiceRequestStatusHistory> History => _history;
    public IReadOnlyCollection<ServiceRequestAssignment> Assignments => _assignments;

    public void Accept(Guid providerId, Guid driverId, Guid providerVehicleId, Guid userId, DateTimeOffset now)
    {
        EnsureStatus(ServiceRequestStatus.Pending);
        ProviderId = providerId;
        DriverId = driverId;
        ProviderVehicleId = providerVehicleId;
        AcceptedAt = now;
        var assignment = new ServiceRequestAssignment(Id, providerId, driverId, providerVehicleId, userId, now);
        _assignments.Add(assignment);
        Transition(ServiceRequestStatus.Accepted, userId, null, now);
    }

    public void Release(Guid userId, string reason, DateTimeOffset now)
    {
        EnsureStatus(ServiceRequestStatus.Accepted);
        var assignment = _assignments.Last(x => x.Status == AssignmentStatus.Accepted);
        assignment.Release(reason, now);
        ProviderId = null;
        DriverId = null;
        ProviderVehicleId = null;
        AcceptedAt = null;
        Transition(ServiceRequestStatus.Pending, userId, reason, now);
    }

    public void MarkEnRoute(Guid userId, DateTimeOffset now)
    {
        EnsureStatus(ServiceRequestStatus.Accepted);
        EnRouteAt = now;
        Transition(ServiceRequestStatus.EnRoute, userId, null, now);
    }

    public void Start(Guid userId, DateTimeOffset now)
    {
        EnsureStatus(ServiceRequestStatus.EnRoute);
        StartedAt = now;
        Transition(ServiceRequestStatus.InProgress, userId, null, now);
    }

    public void Complete(Guid userId, string? notes, IEnumerable<ServiceRequestItem> items, DateTimeOffset now)
    {
        EnsureStatus(ServiceRequestStatus.InProgress);
        CompletionNotes = notes?.Trim();
        _items.AddRange(items);
        CompletedAt = now;
        Transition(ServiceRequestStatus.Completed, userId, null, now);
    }

    public void Cancel(Guid userId, string reason, DateTimeOffset now)
    {
        if (Status is ServiceRequestStatus.Completed or ServiceRequestStatus.Cancelled)
        {
            throw new DomainException("A terminal request cannot be cancelled.");
        }

        var assignment = _assignments.LastOrDefault(x => x.Status == AssignmentStatus.Accepted);
        assignment?.Cancel(now);
        CancelledAt = now;
        Transition(ServiceRequestStatus.Cancelled, userId, Guard.Required(reason, "Reason", 500), now);
    }

    private void EnsureStatus(ServiceRequestStatus expected)
    {
        if (Status != expected)
        {
            throw new DomainException($"Request must be {expected} but is {Status}.");
        }
    }

    private void Transition(ServiceRequestStatus next, Guid userId, string? reason, DateTimeOffset now)
    {
        var previous = Status;
        Status = next;
        Version++;
        Touch(now);
        _history.Add(new ServiceRequestStatusHistory(Id, previous, next, userId, reason, now));
    }
}

public sealed class ServiceRequestItem : Entity
{
    private ServiceRequestItem() { }
    public ServiceRequestItem(Guid requestId, string name, decimal quantity, string? unit, string? description, string? notes)
    {
        if (quantity <= 0) throw new DomainException("Item quantity must be positive.");
        ServiceRequestId = requestId;
        Name = Guard.Required(name, "ItemName", 150);
        Quantity = quantity;
        Unit = unit?.Trim();
        Description = description?.Trim();
        Notes = notes?.Trim();
    }

    public Guid ServiceRequestId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public string? Unit { get; private set; }
    public string? Description { get; private set; }
    public string? Notes { get; private set; }
}

public sealed class ServiceRequestStatusHistory : Entity
{
    private ServiceRequestStatusHistory() { }
    public ServiceRequestStatusHistory(Guid requestId, ServiceRequestStatus? from, ServiceRequestStatus to,
        Guid changedByUserId, string? reason, DateTimeOffset now)
    {
        ServiceRequestId = requestId;
        FromStatus = from;
        ToStatus = to;
        ChangedByUserId = changedByUserId;
        Reason = reason;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid ServiceRequestId { get; private set; }
    public ServiceRequestStatus? FromStatus { get; private set; }
    public ServiceRequestStatus ToStatus { get; private set; }
    public Guid ChangedByUserId { get; private set; }
    public string? Reason { get; private set; }
}

public sealed class ServiceRequestAssignment : Entity
{
    private ServiceRequestAssignment() { }
    public ServiceRequestAssignment(Guid requestId, Guid providerId, Guid driverId, Guid vehicleId, Guid acceptedBy, DateTimeOffset now)
    {
        ServiceRequestId = requestId;
        ProviderId = providerId;
        DriverId = driverId;
        ProviderVehicleId = vehicleId;
        AcceptedByUserId = acceptedBy;
        AcceptedAt = now;
        Status = AssignmentStatus.Accepted;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid ServiceRequestId { get; private set; }
    public Guid ProviderId { get; private set; }
    public Guid DriverId { get; private set; }
    public Guid ProviderVehicleId { get; private set; }
    public AssignmentStatus Status { get; private set; }
    public Guid AcceptedByUserId { get; private set; }
    public DateTimeOffset AcceptedAt { get; private set; }
    public DateTimeOffset? ReleasedAt { get; private set; }
    public string? ReleaseReason { get; private set; }

    public void Release(string reason, DateTimeOffset now)
    {
        Status = AssignmentStatus.Released;
        ReleasedAt = now;
        ReleaseReason = Guard.Required(reason, "ReleaseReason", 500);
        Touch(now);
    }

    public void Cancel(DateTimeOffset now)
    {
        Status = AssignmentStatus.Cancelled;
        Touch(now);
    }
}

public sealed class ServiceRequestTracking
{
    private ServiceRequestTracking() { }
    public ServiceRequestTracking(Guid requestId, Guid eventId, Guid assignmentId, Guid driverId,
        decimal latitude, decimal longitude, DateTimeOffset capturedAt, DateTimeOffset receivedAt,
        decimal? accuracyMeters, decimal? speedKmh, decimal? headingDegrees)
    {
        Set(eventId, assignmentId, driverId, latitude, longitude, capturedAt, receivedAt,
            accuracyMeters, speedKmh, headingDegrees);
        ServiceRequestId = requestId;
    }

    public Guid ServiceRequestId { get; private set; }
    public Guid SourceEventId { get; private set; }
    public Guid AssignmentId { get; private set; }
    public Guid DriverId { get; private set; }
    public decimal Latitude { get; private set; }
    public decimal Longitude { get; private set; }
    public decimal? AccuracyMeters { get; private set; }
    public decimal? SpeedKmh { get; private set; }
    public decimal? HeadingDegrees { get; private set; }
    public DateTimeOffset CapturedAt { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }

    public bool TryUpdate(Guid eventId, Guid assignmentId, Guid driverId, decimal latitude, decimal longitude,
        DateTimeOffset capturedAt, DateTimeOffset receivedAt, decimal? accuracy, decimal? speed, decimal? heading)
    {
        if (eventId == SourceEventId || capturedAt <= CapturedAt) return false;
        Set(eventId, assignmentId, driverId, latitude, longitude, capturedAt, receivedAt, accuracy, speed, heading);
        return true;
    }

    private void Set(Guid eventId, Guid assignmentId, Guid driverId, decimal latitude, decimal longitude,
        DateTimeOffset capturedAt, DateTimeOffset receivedAt, decimal? accuracy, decimal? speed, decimal? heading)
    {
        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
            throw new DomainException("Coordinates are invalid.");
        if (accuracy < 0 || speed < 0 || heading is < 0 or >= 360)
            throw new DomainException("Tracking metadata is invalid.");
        SourceEventId = eventId;
        AssignmentId = assignmentId;
        DriverId = driverId;
        Latitude = latitude;
        Longitude = longitude;
        CapturedAt = capturedAt;
        ReceivedAt = receivedAt;
        AccuracyMeters = accuracy;
        SpeedKmh = speed;
        HeadingDegrees = heading;
    }
}
