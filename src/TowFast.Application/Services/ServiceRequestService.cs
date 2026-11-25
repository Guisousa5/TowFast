using TowFast.Application.Abstractions;
using TowFast.Application.Common;
using TowFast.Application.Contracts;
using TowFast.Domain.Customers;
using TowFast.Domain.Identity;
using TowFast.Domain.Providers;
using TowFast.Domain.ServiceRequests;

namespace TowFast.Application.Services;

public sealed class ServiceRequestService(ITowFastStore store, IClock clock, PaginationPolicy pagination)
{
    private static readonly ServiceRequestStatus[] ActiveStatuses =
        [ServiceRequestStatus.Accepted, ServiceRequestStatus.EnRoute, ServiceRequestStatus.InProgress];

    public async Task<PriceQuoteResponse> QuoteAsync(ActorContext actor, CreateServiceRequestCommand command, CancellationToken ct)
    {
        var context = await ResolveCreationContextAsync(actor, command, ct);
        return PricingCalculator.Quote(context.ServiceType, context.Origin, context.Destination);
    }

    public Task<ServiceRequestResponse> CreateAsync(ActorContext actor, CreateServiceRequestCommand command, CancellationToken ct) =>
        store.InTransactionAsync(async token =>
        {
            var context = await ResolveCreationContextAsync(actor, command, token);
            var quote = PricingCalculator.Quote(context.ServiceType, context.Origin, context.Destination);
            if (command.ExpectedPrice is null || command.ExpectedPrice != quote.Price)
                throw new ConflictException("O preço mudou. Consulte novamente antes de confirmar.");

            if (context.Destination is not null) await store.AddAsync(context.Destination, token);
            await store.AddAsync(context.Origin, token);
            var request = new ServiceRequest(context.Client.Id, context.Vehicle.Id, context.ServiceType.Id,
                context.Origin.Id, context.Destination?.Id, context.ServiceType.RequiresDestination,
                command.CustomerNotes, actor.UserId, clock.UtcNow, quote.Price, quote.EstimatedDistanceKm);
            await store.AddAsync(request, token);
            await store.SaveChangesAsync(token);
            return Map(request);
        }, ct);

    private async Task<CreationContext> ResolveCreationContextAsync(ActorContext actor,
        CreateServiceRequestCommand command, CancellationToken ct)
    {
        var vehicle = await RequiredAsync<ClientVehicle>(command.ClientVehicleId, ct);
        if (!vehicle.IsActive) throw new ConflictException("Client vehicle is inactive.");
        if (actor.Role == UserRole.Client && actor.ClientId != vehicle.ClientId) throw new ForbiddenException("Access denied.");
        if (!actor.IsOperation && actor.Role != UserRole.Client) throw new ForbiddenException("Access denied.");
        var client = await RequiredAsync<Client>(vehicle.ClientId, ct);
        if (!client.IsActive) throw new ConflictException("Client is inactive.");
        var serviceType = await RequiredAsync<ServiceType>(command.ServiceTypeId, ct);
        if (!serviceType.IsActive) throw new ConflictException("Service type is inactive.");
        var origin = await ResolveSnapshotAsync(client.Id, command.OriginLocationId, command.Origin, "Origin", ct);
        var destination = serviceType.RequiresDestination
            ? await ResolveSnapshotAsync(client.Id, command.DestinationLocationId, command.Destination, "Destination", ct)
            : null;
        return new CreationContext(client, vehicle, serviceType, origin, destination);
    }

    private sealed record CreationContext(Client Client, ClientVehicle Vehicle, ServiceType ServiceType,
        Location Origin, Location? Destination);

    public async Task<PageResult<ServiceRequestResponse>> ListAsync(ActorContext actor, int page, int pageSize,
        ServiceRequestFilter filter, CancellationToken ct)
    {
        NormalizePage(ref page, ref pageSize);
        var requests = await store.ListAsync<ServiceRequest>(x =>
            (!filter.Status.HasValue || x.Status == filter.Status) &&
            (!filter.From.HasValue || x.CreatedAt >= filter.From) &&
            (!filter.To.HasValue || x.CreatedAt <= filter.To) &&
            (!filter.ServiceTypeId.HasValue || x.ServiceTypeId == filter.ServiceTypeId) &&
            (!filter.ClientId.HasValue || x.ClientId == filter.ClientId) &&
            (!filter.ProviderId.HasValue || x.ProviderId == filter.ProviderId) &&
            (!filter.DriverId.HasValue || x.DriverId == filter.DriverId), ct);
        requests = requests.Where(x => CanView(actor, x)).OrderByDescending(x => x.CreatedAt).ThenBy(x => x.PublicId).ToList();
        return new PageResult<ServiceRequestResponse>(requests.Skip((page - 1) * pageSize).Take(pageSize).Select(Map).ToList(), page, pageSize, requests.Count);
    }

    public async Task<ServiceRequestDetailResponse> GetAsync(ActorContext actor, string publicId, CancellationToken ct)
    {
        var normalized = publicId.Trim().ToUpperInvariant();
        var request = await store.FirstAsync<ServiceRequest>(x => x.PublicId == normalized, ct)
            ?? throw new NotFoundException("Service request was not found.");
        if (!CanView(actor, request)) throw new NotFoundException("Service request was not found.");
        var client = await RequiredAsync<Client>(request.ClientId, ct);
        var vehicle = await RequiredAsync<ClientVehicle>(request.ClientVehicleId, ct);
        var origin = await RequiredAsync<Location>(request.OriginLocationId, ct);
        var destination = request.DestinationLocationId.HasValue
            ? await RequiredAsync<Location>(request.DestinationLocationId.Value, ct)
            : null;
        var serviceType = await RequiredAsync<ServiceType>(request.ServiceTypeId, ct);
        var maySeeContact = actor.IsOperation || actor.DriverId == request.DriverId;
        return new ServiceRequestDetailResponse(Map(request),
            new RequestClientResponse(client.Id, client.Name, maySeeContact ? client.Email : null,
                maySeeContact ? client.Phone : null),
            Map(vehicle), Map(origin), destination is null ? null : Map(destination), Map(serviceType));
    }

    public async Task<PageResult<AvailableRequestResponse>> ListAvailableAsync(ActorContext actor, int page, int pageSize, CancellationToken ct)
    {
        EnsureDriver(actor);
        NormalizePage(ref page, ref pageSize);
        var provider = await RequiredAsync<Provider>(actor.ProviderId!.Value, ct);
        var driver = await RequiredAsync<Driver>(actor.DriverId!.Value, ct);
        if (!provider.IsActive || !driver.IsActive) return new PageResult<AvailableRequestResponse>([], page, pageSize, 0);

        if (await store.AnyAsync<ServiceRequest>(x => x.ProviderId == provider.Id && ActiveStatuses.Contains(x.Status), ct))
            return new PageResult<AvailableRequestResponse>([], page, pageSize, 0);

        var links = await store.ListAsync<ProviderServiceType>(x => x.ProviderId == provider.Id && x.IsActive, ct);
        var serviceIds = links.Select(x => x.ServiceTypeId).ToHashSet();
        var requests = await store.ListAsync<ServiceRequest>(x => x.Status == ServiceRequestStatus.Pending && serviceIds.Contains(x.ServiceTypeId), ct);
        var locations = (await store.ListAsync<Location>(x => requests.Select(r => r.OriginLocationId).Contains(x.Id), ct)).ToDictionary(x => x.Id);
        var vehicles = (await store.ListAsync<ClientVehicle>(x => requests.Select(r => r.ClientVehicleId).Contains(x.Id), ct)).ToDictionary(x => x.Id);
        var services = (await store.ListAsync<ServiceType>(x => serviceIds.Contains(x.Id), ct)).ToDictionary(x => x.Id);

        var eligible = requests.Where(x => locations.TryGetValue(x.OriginLocationId, out var location) &&
                location.Latitude.HasValue && location.Longitude.HasValue &&
                DistanceKm(provider.ServiceCenterLatitude, provider.ServiceCenterLongitude,
                    location.Latitude.Value, location.Longitude.Value) <= provider.ServiceRadiusKm)
            .OrderBy(x => x.CreatedAt).ThenBy(x => x.PublicId)
            .Select(x =>
            {
                var location = locations[x.OriginLocationId];
                return new AvailableRequestResponse(x.PublicId, x.Id, services[x.ServiceTypeId].Name,
                    vehicles[x.ClientVehicleId].Model, FormatLocation(location), location.Latitude!.Value,
                    location.Longitude!.Value, x.CreatedAt, x.Version, x.QuotedPrice);
            }).ToList();
        return new PageResult<AvailableRequestResponse>(eligible.Skip((page - 1) * pageSize).Take(pageSize).ToList(), page, pageSize, eligible.Count);
    }

    public Task<ServiceRequestResponse> AcceptAsync(ActorContext actor, Guid requestId, AcceptServiceRequestCommand command, CancellationToken ct)
    {
        EnsureDriver(actor);
        return store.InTransactionAsync(async token =>
        {
            var request = await RequiredAsync<ServiceRequest>(requestId, token);
            EnsureVersion(request, command.Version);
            var provider = await RequiredAsync<Provider>(actor.ProviderId!.Value, token);
            var driver = await RequiredAsync<Driver>(actor.DriverId!.Value, token);
            var vehicle = await RequiredAsync<ProviderVehicle>(command.ProviderVehicleId, token);
            if (!provider.IsActive || !driver.IsActive || !vehicle.IsActive ||
                driver.ProviderId != provider.Id || vehicle.ProviderId != provider.Id)
                throw new ConflictException("Provider, driver or vehicle is not eligible.");
            if (await store.AnyAsync<ServiceRequest>(x => x.ProviderId == provider.Id && ActiveStatuses.Contains(x.Status), token))
                throw new ConflictException("Provider already has an active service request.");
            if (!await store.AnyAsync<ProviderServiceType>(x => x.ProviderId == provider.Id && x.ServiceTypeId == request.ServiceTypeId && x.IsActive, token))
                throw new ConflictException("Provider does not offer this service.");
            var origin = await RequiredAsync<Location>(request.OriginLocationId, token);
            if (!origin.Latitude.HasValue || !origin.Longitude.HasValue ||
                DistanceKm(provider.ServiceCenterLatitude, provider.ServiceCenterLongitude, origin.Latitude.Value, origin.Longitude.Value) > provider.ServiceRadiusKm)
                throw new ConflictException("Request is outside the provider service radius.");

            request.Accept(provider.Id, driver.Id, vehicle.Id, actor.UserId, clock.UtcNow);
            try
            {
                await store.SaveChangesAsync(token);
            }
            catch (Exception exception) when (exception.GetType().Name == "DbUpdateConcurrencyException")
            {
                throw new ConflictException("Request was accepted by another driver. Refresh the available requests.");
            }
            catch (Exception exception) when (exception.GetType().Name == "DbUpdateException")
            {
                throw new ConflictException("Provider already has an active service request or capacity changed.");
            }
            return Map(request);
        }, ct);
    }

    public Task<ServiceRequestResponse> ReleaseAsync(ActorContext actor, Guid id, ReasonCommand command, CancellationToken ct)
    {
        EnsureDriver(actor);
        return ChangeAsync(id, command.Version, actor, request =>
        {
            EnsureAssignedDriver(actor, request);
            request.Release(actor.UserId, command.Reason, clock.UtcNow);
        }, ct);
    }

    public Task<ServiceRequestResponse> EnRouteAsync(ActorContext actor, Guid id, VersionCommand command, CancellationToken ct) =>
        ChangeAsync(id, command.Version, actor, request =>
        {
            EnsureDriverOrOperation(actor, request);
            request.MarkEnRoute(actor.UserId, clock.UtcNow);
        }, ct);

    public Task<ServiceRequestResponse> StartAsync(ActorContext actor, Guid id, VersionCommand command, CancellationToken ct) =>
        ChangeAsync(id, command.Version, actor, request =>
        {
            EnsureDriverOrOperation(actor, request);
            request.Start(actor.UserId, clock.UtcNow);
        }, ct);

    public Task<ServiceRequestResponse> CompleteAsync(ActorContext actor, Guid id, CompleteServiceRequestCommand command, CancellationToken ct) =>
        ChangeAsync(id, command.Version, actor, request =>
        {
            EnsureDriverOrOperation(actor, request);
            var items = command.Items.Select(x => new ServiceRequestItem(request.Id, x.Name, x.Quantity, x.Unit, x.Description, x.Notes));
            request.Complete(actor.UserId, command.CompletionNotes, items, clock.UtcNow);
        }, ct);

    public Task<ServiceRequestResponse> CancelAsync(ActorContext actor, Guid id, ReasonCommand command, CancellationToken ct) =>
        ChangeAsync(id, command.Version, actor, request =>
        {
            var clientMayCancel = actor.Role == UserRole.Client && actor.ClientId == request.ClientId &&
                                  request.Status is ServiceRequestStatus.Pending or ServiceRequestStatus.Accepted or ServiceRequestStatus.EnRoute;
            if (!actor.IsOperation && !clientMayCancel) throw new ForbiddenException("Cancellation is not allowed.");
            request.Cancel(actor.UserId, command.Reason, clock.UtcNow);
        }, ct);

    private Task<ServiceRequestResponse> ChangeAsync(Guid id, long version, ActorContext actor,
        Action<ServiceRequest> change, CancellationToken ct) => store.InTransactionAsync(async token =>
    {
        var request = await RequiredAsync<ServiceRequest>(id, token);
        EnsureVersion(request, version);
        change(request);
        await store.SaveChangesAsync(token);
        return Map(request);
    }, ct);

    private async Task<T> RequiredAsync<T>(Guid id, CancellationToken ct) where T : class =>
        await store.FindAsync<T>(id, ct) ?? throw new NotFoundException($"{typeof(T).Name} was not found.");

    private async Task<Location> ResolveSnapshotAsync(Guid clientId, Guid? savedId, LocationInput? inline,
        string label, CancellationToken ct)
    {
        if (savedId.HasValue)
        {
            var saved = await RequiredAsync<Location>(savedId.Value, ct);
            if (!saved.IsSaved || saved.ClientId != clientId) throw new ForbiddenException("Saved location does not belong to the client.");
            return saved.Snapshot(label);
        }
        if (inline is null) throw new ValidationException($"{label} location is required.");
        var location = CatalogService.NewLocation(null, inline with { Label = label }, false);
        if (!location.Latitude.HasValue || !location.Longitude.HasValue)
            throw new ValidationException($"{label} coordinates are required.");
        return location;
    }

    private static void EnsureVersion(ServiceRequest request, long version)
    {
        if (request.Version != version) throw new ConflictException("The request version is outdated.");
    }

    private static void EnsureDriver(ActorContext actor)
    {
        if (actor.Role != UserRole.Driver || !actor.ProviderId.HasValue || !actor.DriverId.HasValue)
            throw new ForbiddenException("Driver access is required.");
    }

    private static void EnsureAssignedDriver(ActorContext actor, ServiceRequest request)
    {
        EnsureDriver(actor);
        if (request.DriverId != actor.DriverId) throw new ForbiddenException("Only the assigned driver can perform this action.");
    }

    private static void EnsureDriverOrOperation(ActorContext actor, ServiceRequest request)
    {
        if (!actor.IsOperation) EnsureAssignedDriver(actor, request);
    }

    private static bool CanView(ActorContext actor, ServiceRequest request) => actor.IsOperation ||
        (actor.Role == UserRole.Client && actor.ClientId == request.ClientId) ||
        (actor.Role == UserRole.Provider && actor.ProviderId == request.ProviderId) ||
        (actor.Role == UserRole.Driver && actor.DriverId == request.DriverId);

    private void NormalizePage(ref int page, ref int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, pagination.MaxPageSize);
    }

    private static decimal DistanceKm(decimal latitude1, decimal longitude1, decimal latitude2, decimal longitude2)
    {
        const double radius = 6371d;
        var lat1 = DegreesToRadians((double)latitude1);
        var lat2 = DegreesToRadians((double)latitude2);
        var deltaLat = DegreesToRadians((double)(latitude2 - latitude1));
        var deltaLon = DegreesToRadians((double)(longitude2 - longitude1));
        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) * Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
        return (decimal)(radius * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)));
    }

    private static double DegreesToRadians(double value) => value * Math.PI / 180d;
    private static string FormatLocation(Location x) => $"{x.Street}, {x.Number ?? "s/n"} - {x.City}/{x.State}";

    private static ServiceRequestResponse Map(ServiceRequest x) => new(x.Id, x.PublicId, x.ClientId,
        x.ClientVehicleId, x.ServiceTypeId, x.OriginLocationId, x.DestinationLocationId, x.ProviderId,
        x.DriverId, x.ProviderVehicleId, x.Status, x.CustomerNotes, x.CompletionNotes, x.CreatedAt,
        x.AcceptedAt, x.EnRouteAt, x.StartedAt, x.CompletedAt, x.CancelledAt, x.Version,
        x.QuotedPrice, x.EstimatedDistanceKm,
        x.Items.Select(i => new RequestItemResponse(i.Name, i.Quantity, i.Unit, i.Description, i.Notes)).ToList(),
        x.History.OrderBy(h => h.CreatedAt).Select(h => new StatusHistoryResponse(h.FromStatus, h.ToStatus,
            h.ChangedByUserId, h.Reason, h.CreatedAt)).ToList());

    private static VehicleResponse Map(ClientVehicle x) => new(x.Id, x.ClientId, x.LicensePlate,
        x.Manufacturer, x.Model, x.ModelYear, x.Color, x.Notes, x.IsActive);
    private static LocationResponse Map(Location x) => new(x.Id, x.ClientId, x.Label, x.Street, x.Number,
        x.Complement, x.District, x.City, x.State, x.PostalCode, x.Reference, x.Latitude, x.Longitude, x.IsSaved);
    private static ServiceTypeResponse Map(ServiceType x) => new(x.Id, x.Code, x.Name, x.Description,
        x.RequiresDestination, x.IsActive, x.BasePrice, x.PricePerKm);
}
