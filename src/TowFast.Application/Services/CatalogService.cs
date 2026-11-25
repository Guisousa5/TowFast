using TowFast.Application.Abstractions;
using TowFast.Application.Common;
using TowFast.Application.Contracts;
using TowFast.Domain.Customers;
using TowFast.Domain.Identity;
using TowFast.Domain.Providers;

namespace TowFast.Application.Services;

public sealed class CatalogService(
    ITowFastStore store,
    IPasswordHasher passwordHasher,
    IClock clock)
{
    public async Task<ClientResponse> CreateClientAsync(ClientCreateCommand command, CancellationToken ct)
    {
        PasswordRules.Validate(command.Password);
        if (await store.AnyAsync<Client>(x => x.Document == Digits(command.Document) || x.Email == command.Email.Trim().ToLowerInvariant(), ct))
            throw new ConflictException("Email or document is already registered.");
        if (await store.AnyAsync<User>(x => x.Email == command.Email.Trim().ToLowerInvariant(), ct))
            throw new ConflictException("Email is already registered.");

        return await store.InTransactionAsync(async token =>
        {
            var client = new Client(command.Name, command.DocumentType, command.Document, command.Email, command.Phone);
            var user = new User(command.Email, passwordHasher.Hash(command.Password), UserRole.Client);
            user.BindClient(client.Id);
            await store.AddAsync(client, token);
            await store.AddAsync(user, token);
            await store.SaveChangesAsync(token);
            return Map(client);
        }, ct);
    }

    public async Task<IReadOnlyList<ClientResponse>> ListClientsAsync(ActorContext actor, CancellationToken ct)
    {
        if (!actor.IsOperation && actor.Role != UserRole.Client) throw new ForbiddenException("Access denied.");
        var clients = actor.Role == UserRole.Client
            ? await store.ListAsync<Client>(x => x.Id == actor.ClientId, ct)
            : await store.ListAsync<Client>(null, ct);
        return clients.OrderBy(x => x.Name).Select(Map).ToList();
    }

    public async Task<ClientResponse> UpdateClientAsync(ActorContext actor, Guid id, ClientUpdateCommand command, CancellationToken ct)
    {
        EnsureClientAccess(actor, id);
        var client = await RequiredAsync<Client>(id, ct);
        client.Update(command.Name, command.Phone, command.Notes, clock.UtcNow);
        if (command.IsActive.HasValue)
        {
            if (!actor.IsOperation) throw new ForbiddenException("Only operation can change active state.");
            client.SetActive(command.IsActive.Value, clock.UtcNow);
        }
        await store.SaveChangesAsync(ct);
        return Map(client);
    }

    public async Task<VehicleResponse> AddVehicleAsync(ActorContext actor, Guid clientId, VehicleCreateCommand command, CancellationToken ct)
    {
        EnsureClientAccess(actor, clientId);
        _ = await RequiredAsync<Client>(clientId, ct);
        var plate = new string(command.LicensePlate.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (await store.AnyAsync<ClientVehicle>(x => x.LicensePlate == plate, ct)) throw new ConflictException("License plate is already registered.");
        var vehicle = new ClientVehicle(clientId, command.LicensePlate, command.Manufacturer, command.Model,
            command.ModelYear, command.Color, command.Notes);
        await store.AddAsync(vehicle, ct);
        await store.SaveChangesAsync(ct);
        return Map(vehicle);
    }

    public async Task<IReadOnlyList<VehicleResponse>> ListVehiclesAsync(ActorContext actor, Guid clientId, CancellationToken ct)
    {
        EnsureClientAccess(actor, clientId);
        return (await store.ListAsync<ClientVehicle>(x => x.ClientId == clientId, ct)).Select(Map).ToList();
    }

    public async Task<VehicleResponse> SetClientVehicleActiveAsync(ActorContext actor, Guid clientId, Guid vehicleId,
        bool isActive, CancellationToken ct)
    {
        EnsureClientAccess(actor, clientId);
        var vehicle = await RequiredAsync<ClientVehicle>(vehicleId, ct);
        if (vehicle.ClientId != clientId) throw new NotFoundException("Client vehicle was not found.");
        vehicle.SetActive(isActive, clock.UtcNow);
        await store.SaveChangesAsync(ct);
        return Map(vehicle);
    }

    public async Task<LocationResponse> AddLocationAsync(ActorContext actor, Guid clientId, LocationInput input, CancellationToken ct)
    {
        EnsureClientAccess(actor, clientId);
        var location = NewLocation(clientId, input, true);
        await store.AddAsync(location, ct);
        await store.SaveChangesAsync(ct);
        return Map(location);
    }

    public async Task<IReadOnlyList<LocationResponse>> ListLocationsAsync(ActorContext actor, Guid clientId, CancellationToken ct)
    {
        EnsureClientAccess(actor, clientId);
        return (await store.ListAsync<Location>(x => x.ClientId == clientId && x.IsSaved, ct)).Select(Map).ToList();
    }

    public async Task<ProviderResponse> CreateProviderAsync(ProviderCreateCommand command, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(command.Password)) PasswordRules.Validate(command.Password);
        var document = Digits(command.Document);
        if (await store.AnyAsync<Provider>(x => x.Document == document, ct)) throw new ConflictException("Provider document is already registered.");
        if (!string.IsNullOrWhiteSpace(command.Email) && await store.AnyAsync<User>(x => x.Email == command.Email.Trim().ToLowerInvariant(), ct))
            throw new ConflictException("Provider email is already registered.");
        return await store.InTransactionAsync(async token =>
        {
            var provider = new Provider(command.LegalName, command.TradeName, command.Document, command.Email,
                command.Phone, command.ServiceCenterLatitude, command.ServiceCenterLongitude, command.ServiceRadiusKm);
            await store.AddAsync(provider, token);
            if (!string.IsNullOrWhiteSpace(command.Email) && !string.IsNullOrWhiteSpace(command.Password))
            {
                var user = new User(command.Email, passwordHasher.Hash(command.Password), UserRole.Provider);
                user.BindProvider(provider.Id);
                await store.AddAsync(user, token);
            }
            await store.SaveChangesAsync(token);
            return Map(provider);
        }, ct);
    }

    public async Task<IReadOnlyList<ProviderResponse>> ListProvidersAsync(CancellationToken ct) =>
        (await store.ListAsync<Provider>(null, ct)).OrderBy(x => x.LegalName).Select(Map).ToList();

    public async Task<ProviderResponse> SetProviderActiveAsync(Guid id, bool isActive, CancellationToken ct)
    {
        var provider = await RequiredAsync<Provider>(id, ct);
        provider.SetActive(isActive, clock.UtcNow);
        await store.SaveChangesAsync(ct);
        return Map(provider);
    }

    public async Task<ProviderResponse> UpdateProviderAsync(Guid id, ProviderUpdateCommand command, CancellationToken ct)
    {
        var provider = await RequiredAsync<Provider>(id, ct);
        provider.Update(command.LegalName, command.TradeName, command.Email, command.Phone,
            command.ServiceCenterLatitude, command.ServiceCenterLongitude, command.ServiceRadiusKm,
            command.Notes, clock.UtcNow);
        if (command.IsActive.HasValue) provider.SetActive(command.IsActive.Value, clock.UtcNow);
        await store.SaveChangesAsync(ct);
        return Map(provider);
    }

    public async Task<DriverResponse> AddDriverAsync(Guid providerId, DriverCreateCommand command, CancellationToken ct)
    {
        PasswordRules.Validate(command.Password);
        _ = await RequiredAsync<Provider>(providerId, ct);
        var email = command.Email.Trim().ToLowerInvariant();
        if (await store.AnyAsync<User>(x => x.Email == email, ct) ||
            await store.AnyAsync<Driver>(x => x.Document == Digits(command.Document) || x.LicenseNumber == command.LicenseNumber.ToUpperInvariant(), ct))
            throw new ConflictException("Driver credentials, document or license are already registered.");

        return await store.InTransactionAsync(async token =>
        {
            var driver = new Driver(providerId, command.FullName, command.Document, command.Phone,
                command.LicenseNumber, command.LicenseCategory, command.LicenseExpiresOn);
            var user = new User(command.Email, passwordHasher.Hash(command.Password), UserRole.Driver);
            user.BindDriver(providerId, driver.Id);
            await store.AddAsync(driver, token);
            await store.AddAsync(user, token);
            await store.SaveChangesAsync(token);
            return Map(driver);
        }, ct);
    }

    public async Task<IReadOnlyList<DriverResponse>> ListDriversAsync(Guid providerId, CancellationToken ct) =>
        (await store.ListAsync<Driver>(x => x.ProviderId == providerId, ct)).Select(Map).ToList();

    public async Task<DriverResponse> UpdateDriverAsync(Guid providerId, Guid driverId, DriverUpdateCommand command, CancellationToken ct)
    {
        var driver = await RequiredAsync<Driver>(driverId, ct);
        if (driver.ProviderId != providerId) throw new NotFoundException("Driver was not found.");
        var license = command.LicenseNumber.Trim().ToUpperInvariant();
        if (await store.AnyAsync<Driver>(x => x.Id != driverId && x.LicenseNumber == license, ct))
            throw new ConflictException("Driver license is already registered.");
        driver.Update(command.FullName, command.Phone, command.LicenseNumber, command.LicenseCategory,
            command.LicenseExpiresOn, command.IsActive, clock.UtcNow);
        await store.SaveChangesAsync(ct);
        return Map(driver);
    }

    public async Task<ProviderVehicleResponse> AddProviderVehicleAsync(Guid providerId, ProviderVehicleCreateCommand command, CancellationToken ct)
    {
        _ = await RequiredAsync<Provider>(providerId, ct);
        var plate = new string(command.LicensePlate.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (await store.AnyAsync<ProviderVehicle>(x => x.LicensePlate == plate, ct)) throw new ConflictException("License plate is already registered.");
        var vehicle = new ProviderVehicle(providerId, command.LicensePlate, command.VehicleType, command.Description, command.CapacityKg);
        await store.AddAsync(vehicle, ct);
        await store.SaveChangesAsync(ct);
        return Map(vehicle);
    }

    public async Task<IReadOnlyList<ProviderVehicleResponse>> ListProviderVehiclesAsync(Guid providerId, CancellationToken ct) =>
        (await store.ListAsync<ProviderVehicle>(x => x.ProviderId == providerId, ct)).Select(Map).ToList();

    public async Task<ProviderVehicleResponse> UpdateProviderVehicleAsync(Guid providerId, Guid vehicleId,
        ProviderVehicleUpdateCommand command, CancellationToken ct)
    {
        var vehicle = await RequiredAsync<ProviderVehicle>(vehicleId, ct);
        if (vehicle.ProviderId != providerId) throw new NotFoundException("Provider vehicle was not found.");
        vehicle.Update(command.VehicleType, command.Description, command.CapacityKg, command.IsActive, clock.UtcNow);
        await store.SaveChangesAsync(ct);
        return Map(vehicle);
    }

    public async Task<IReadOnlyList<ServiceTypeResponse>> ListServiceTypesAsync(CancellationToken ct) =>
        (await store.ListAsync<ServiceType>(null, ct)).OrderBy(x => x.Code).Select(Map).ToList();

    public async Task<ServiceTypeResponse> SetServiceTypeActiveAsync(Guid id, bool isActive, CancellationToken ct)
    {
        var serviceType = await RequiredAsync<ServiceType>(id, ct);
        serviceType.SetActive(isActive, clock.UtcNow);
        await store.SaveChangesAsync(ct);
        return Map(serviceType);
    }

    public async Task<ServiceTypeResponse> ConfigureServicePriceAsync(Guid id,
        ConfigureServicePriceCommand command, CancellationToken ct)
    {
        var serviceType = await RequiredAsync<ServiceType>(id, ct);
        serviceType.ConfigurePricing(command.BasePrice, command.PricePerKm, clock.UtcNow);
        await store.SaveChangesAsync(ct);
        return Map(serviceType);
    }

    public async Task SetProviderServicesAsync(Guid providerId, SetProviderServicesCommand command, CancellationToken ct)
    {
        _ = await RequiredAsync<Provider>(providerId, ct);
        var valid = await store.ListAsync<ServiceType>(x => command.ServiceTypeIds.Contains(x.Id) && x.IsActive, ct);
        if (valid.Count != command.ServiceTypeIds.Distinct().Count()) throw new ValidationException("One or more service types are invalid.");
        var current = await store.ListAsync<ProviderServiceType>(x => x.ProviderId == providerId, ct);
        foreach (var item in current) store.Remove(item);
        foreach (var id in command.ServiceTypeIds.Distinct()) await store.AddAsync(new ProviderServiceType(providerId, id), ct);
        await store.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ServiceTypeResponse>> ListProviderServicesAsync(Guid providerId, CancellationToken ct)
    {
        var links = await store.ListAsync<ProviderServiceType>(x => x.ProviderId == providerId && x.IsActive, ct);
        var ids = links.Select(x => x.ServiceTypeId).ToHashSet();
        return (await store.ListAsync<ServiceType>(x => ids.Contains(x.Id), ct)).Select(Map).ToList();
    }

    internal static Location NewLocation(Guid? clientId, LocationInput x, bool saved) => new(clientId, x.Label, x.Street,
        x.Number, x.Complement, x.District, x.City, x.State, x.PostalCode, x.Reference, x.Latitude, x.Longitude, saved);

    private async Task<T> RequiredAsync<T>(Guid id, CancellationToken ct) where T : class =>
        await store.FindAsync<T>(id, ct) ?? throw new NotFoundException($"{typeof(T).Name} was not found.");

    private static string Digits(string value) => new(value.Where(char.IsDigit).ToArray());
    private static void EnsureClientAccess(ActorContext actor, Guid clientId)
    {
        if (!actor.IsOperation && (actor.Role != UserRole.Client || actor.ClientId != clientId)) throw new ForbiddenException("Access denied.");
    }

    private static ClientResponse Map(Client x) => new(x.Id, x.Name, x.DocumentType, x.Document, x.Email, x.Phone, x.Notes, x.IsActive);
    private static VehicleResponse Map(ClientVehicle x) => new(x.Id, x.ClientId, x.LicensePlate, x.Manufacturer, x.Model, x.ModelYear, x.Color, x.Notes, x.IsActive);
    private static LocationResponse Map(Location x) => new(x.Id, x.ClientId, x.Label, x.Street, x.Number, x.Complement, x.District, x.City, x.State, x.PostalCode, x.Reference, x.Latitude, x.Longitude, x.IsSaved);
    private static ProviderResponse Map(Provider x) => new(x.Id, x.LegalName, x.TradeName, x.Document, x.Email, x.Phone, x.ServiceCenterLatitude, x.ServiceCenterLongitude, x.ServiceRadiusKm, x.IsActive);
    private static DriverResponse Map(Driver x) => new(x.Id, x.ProviderId, x.FullName, x.Document, x.Phone, x.LicenseNumber, x.LicenseCategory, x.LicenseExpiresOn, x.IsActive);
    private static ProviderVehicleResponse Map(ProviderVehicle x) => new(x.Id, x.ProviderId, x.LicensePlate, x.VehicleType, x.Description, x.CapacityKg, x.IsActive);
    private static ServiceTypeResponse Map(ServiceType x) => new(x.Id, x.Code, x.Name, x.Description,
        x.RequiresDestination, x.IsActive, x.BasePrice, x.PricePerKm);
}
