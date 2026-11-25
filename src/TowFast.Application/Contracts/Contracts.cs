using TowFast.Domain.Customers;
using TowFast.Domain.Identity;
using TowFast.Domain.Providers;
using TowFast.Domain.ServiceRequests;

namespace TowFast.Application.Contracts;

public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshExpiresAt,
    UserResponse User);

public sealed record UserResponse(Guid Id, string Email, UserRole Role, Guid? ClientId, Guid? ProviderId, Guid? DriverId);
public sealed record RegisterClientCommand(string Name, DocumentType DocumentType, string Document, string Email, string Phone, string Password);
public sealed record LoginCommand(string Email, string Password);
public sealed record RefreshCommand(string RefreshToken);

public sealed record ClientCreateCommand(string Name, DocumentType DocumentType, string Document, string Email, string Phone, string Password);
public sealed record ClientUpdateCommand(string Name, string Phone, string? Notes, bool? IsActive);
public sealed record ClientResponse(Guid Id, string Name, DocumentType DocumentType, string Document, string Email, string Phone, string? Notes, bool IsActive);

public sealed record VehicleCreateCommand(string LicensePlate, string Manufacturer, string Model, int? ModelYear, string? Color, string? Notes);
public sealed record VehicleActiveCommand(bool IsActive);
public sealed record VehicleResponse(Guid Id, Guid ClientId, string LicensePlate, string Manufacturer, string Model, int? ModelYear, string? Color, string? Notes, bool IsActive);

public sealed record LocationInput(string Label, string Street, string? Number, string? Complement, string? District,
    string City, string State, string? PostalCode, string? Reference, decimal? Latitude, decimal? Longitude);
public sealed record LocationResponse(Guid Id, Guid? ClientId, string Label, string Street, string? Number,
    string? Complement, string? District, string City, string State, string? PostalCode, string? Reference,
    decimal? Latitude, decimal? Longitude, bool IsSaved);

public sealed record ProviderCreateCommand(string LegalName, string? TradeName, string Document, string? Email,
    string Phone, decimal ServiceCenterLatitude, decimal ServiceCenterLongitude, decimal ServiceRadiusKm, string? Password);
public sealed record ProviderUpdateCommand(string LegalName, string? TradeName, string? Email, string Phone,
    decimal ServiceCenterLatitude, decimal ServiceCenterLongitude, decimal ServiceRadiusKm, string? Notes, bool? IsActive);
public sealed record ProviderResponse(Guid Id, string LegalName, string? TradeName, string Document, string? Email,
    string Phone, decimal ServiceCenterLatitude, decimal ServiceCenterLongitude, decimal ServiceRadiusKm, bool IsActive);

public sealed record DriverCreateCommand(string FullName, string Document, string Phone, string LicenseNumber,
    string LicenseCategory, DateOnly LicenseExpiresOn, string Email, string Password);
public sealed record DriverUpdateCommand(string FullName, string Phone, string LicenseNumber,
    string LicenseCategory, DateOnly LicenseExpiresOn, bool IsActive);
public sealed record DriverResponse(Guid Id, Guid ProviderId, string FullName, string Document, string Phone,
    string LicenseNumber, string LicenseCategory, DateOnly LicenseExpiresOn, bool IsActive);

public sealed record ProviderVehicleCreateCommand(string LicensePlate, string VehicleType, string? Description, decimal? CapacityKg);
public sealed record ProviderVehicleUpdateCommand(string VehicleType, string? Description, decimal? CapacityKg, bool IsActive);
public sealed record ProviderVehicleResponse(Guid Id, Guid ProviderId, string LicensePlate, string VehicleType,
    string? Description, decimal? CapacityKg, bool IsActive);

public sealed record ServiceTypeResponse(Guid Id, ServiceTypeCode Code, string Name, string? Description,
    bool RequiresDestination, bool IsActive, decimal? BasePrice, decimal? PricePerKm);
public sealed record ConfigureServicePriceCommand(decimal BasePrice, decimal? PricePerKm);
public sealed record SetProviderServicesCommand(IReadOnlyCollection<Guid> ServiceTypeIds);

public sealed record ServiceRequestFilter(ServiceRequestStatus? Status, DateTimeOffset? From, DateTimeOffset? To,
    Guid? ServiceTypeId, Guid? ClientId, Guid? ProviderId, Guid? DriverId);

public sealed record CreateServiceRequestCommand(Guid ClientVehicleId, Guid ServiceTypeId,
    LocationInput? Origin, LocationInput? Destination, string? CustomerNotes,
    Guid? OriginLocationId = null, Guid? DestinationLocationId = null, decimal? ExpectedPrice = null);
public sealed record PriceQuoteResponse(decimal Price, string Currency, decimal? EstimatedDistanceKm);
public sealed record AcceptServiceRequestCommand(Guid ProviderVehicleId, long Version);
public sealed record VersionCommand(long Version);
public sealed record ReasonCommand(string Reason, long Version);
public sealed record CompleteItemCommand(string Name, decimal Quantity, string? Unit, string? Description, string? Notes);
public sealed record CompleteServiceRequestCommand(string? CompletionNotes, IReadOnlyCollection<CompleteItemCommand> Items, long Version);

public sealed record AvailableRequestResponse(string PublicId, Guid Id, string ServiceType, string VehicleModel,
    string Origin, decimal Latitude, decimal Longitude, DateTimeOffset OpenedAt, long Version,
    decimal? QuotedPrice);
public sealed record RequestItemResponse(string Name, decimal Quantity, string? Unit, string? Description, string? Notes);
public sealed record StatusHistoryResponse(ServiceRequestStatus? From, ServiceRequestStatus To, Guid ChangedBy, string? Reason, DateTimeOffset At);
public sealed record ServiceRequestResponse(Guid Id, string PublicId, Guid ClientId, Guid ClientVehicleId,
    Guid ServiceTypeId, Guid OriginLocationId, Guid? DestinationLocationId, Guid? ProviderId, Guid? DriverId,
    Guid? ProviderVehicleId, ServiceRequestStatus Status, string? CustomerNotes, string? CompletionNotes,
    DateTimeOffset OpenedAt, DateTimeOffset? AcceptedAt, DateTimeOffset? EnRouteAt, DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt, DateTimeOffset? CancelledAt, long Version,
    decimal? QuotedPrice, decimal? EstimatedDistanceKm,
    IReadOnlyCollection<RequestItemResponse> Items, IReadOnlyCollection<StatusHistoryResponse> History);
public sealed record RequestClientResponse(Guid Id, string Name, string? Email, string? Phone);
public sealed record ServiceRequestDetailResponse(ServiceRequestResponse Request, RequestClientResponse Client,
    VehicleResponse Vehicle, LocationResponse Origin, LocationResponse? Destination, ServiceTypeResponse ServiceType);

public sealed record PublishPositionCommand(Guid SourceEventId, decimal Latitude, decimal Longitude,
    DateTimeOffset CapturedAt, decimal? AccuracyMeters, decimal? SpeedKmh, decimal? HeadingDegrees);
public sealed record TrackingResponse(Guid ServiceRequestId, decimal Latitude, decimal Longitude,
    DateTimeOffset CapturedAt, DateTimeOffset ReceivedAt, bool IsStale, decimal? AccuracyMeters,
    decimal? SpeedKmh, decimal? HeadingDegrees);
