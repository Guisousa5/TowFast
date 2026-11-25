using TowFast.Domain.Common;

namespace TowFast.Domain.Providers;

public enum ServiceTypeCode
{
    Tow,
    BatteryReplacement,
    TireChange
}

public sealed class Provider : Entity
{
    private Provider() { }

    public Provider(string legalName, string? tradeName, string document, string? email, string phone,
        decimal centerLatitude, decimal centerLongitude, decimal radiusKm)
    {
        if (centerLatitude is < -90 or > 90 || centerLongitude is < -180 or > 180)
        {
            throw new DomainException("Provider center coordinates are invalid.");
        }

        if (radiusKm <= 0)
        {
            throw new DomainException("Service radius must be positive.");
        }

        LegalName = Guard.Required(legalName, "LegalName", 200);
        TradeName = tradeName?.Trim();
        Document = Guard.CpfOrCnpj(document);
        Email = string.IsNullOrWhiteSpace(email) ? null : Guard.Email(email);
        Phone = Guard.Digits(phone, "Phone", 10, 11);
        ServiceCenterLatitude = centerLatitude;
        ServiceCenterLongitude = centerLongitude;
        ServiceRadiusKm = radiusKm;
    }

    public string LegalName { get; private set; } = string.Empty;
    public string? TradeName { get; private set; }
    public string Document { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string Phone { get; private set; } = string.Empty;
    public decimal ServiceCenterLatitude { get; private set; }
    public decimal ServiceCenterLongitude { get; private set; }
    public decimal ServiceRadiusKm { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void Update(string legalName, string? tradeName, string? email, string phone,
        decimal centerLatitude, decimal centerLongitude, decimal radiusKm, string? notes, DateTimeOffset now)
    {
        if (centerLatitude is < -90 or > 90 || centerLongitude is < -180 or > 180)
            throw new DomainException("Provider center coordinates are invalid.");
        if (radiusKm <= 0) throw new DomainException("Service radius must be positive.");
        LegalName = Guard.Required(legalName, "LegalName", 200);
        TradeName = tradeName?.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : Guard.Email(email);
        Phone = Guard.Digits(phone, "Phone", 10, 11);
        ServiceCenterLatitude = centerLatitude;
        ServiceCenterLongitude = centerLongitude;
        ServiceRadiusKm = radiusKm;
        Notes = notes?.Trim();
        Touch(now);
    }

    public void SetActive(bool active, DateTimeOffset now)
    {
        IsActive = active;
        Touch(now);
    }
}

public sealed class Driver : Entity
{
    private Driver() { }

    public Driver(Guid providerId, string fullName, string document, string phone,
        string licenseNumber, string licenseCategory, DateOnly licenseExpiresOn)
    {
        ProviderId = providerId;
        FullName = Guard.Required(fullName, "FullName", 200);
        Document = Guard.CpfOrCnpj(document, requireCpf: true);
        Phone = Guard.Digits(phone, "Phone", 10, 11);
        LicenseNumber = Guard.Required(licenseNumber, "LicenseNumber", 30).ToUpperInvariant();
        LicenseCategory = Guard.Required(licenseCategory, "LicenseCategory", 10).ToUpperInvariant();
        LicenseExpiresOn = licenseExpiresOn;
    }

    public Guid ProviderId { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string Document { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string LicenseNumber { get; private set; } = string.Empty;
    public string LicenseCategory { get; private set; } = string.Empty;
    public DateOnly LicenseExpiresOn { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void Update(string fullName, string phone, string licenseNumber, string licenseCategory,
        DateOnly licenseExpiresOn, bool isActive, DateTimeOffset now)
    {
        FullName = Guard.Required(fullName, "FullName", 200);
        Phone = Guard.Digits(phone, "Phone", 10, 11);
        LicenseNumber = Guard.Required(licenseNumber, "LicenseNumber", 30).ToUpperInvariant();
        LicenseCategory = Guard.Required(licenseCategory, "LicenseCategory", 10).ToUpperInvariant();
        LicenseExpiresOn = licenseExpiresOn;
        IsActive = isActive;
        Touch(now);
    }
}

public sealed class ProviderVehicle : Entity
{
    private ProviderVehicle() { }

    public ProviderVehicle(Guid providerId, string plate, string vehicleType, string? description, decimal? capacityKg)
    {
        if (capacityKg <= 0)
        {
            throw new DomainException("Capacity must be positive.");
        }

        ProviderId = providerId;
        LicensePlate = Guard.Plate(plate);
        VehicleType = Guard.Required(vehicleType, "VehicleType", 100);
        Description = description?.Trim();
        CapacityKg = capacityKg;
    }

    public Guid ProviderId { get; private set; }
    public string LicensePlate { get; private set; } = string.Empty;
    public string VehicleType { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal? CapacityKg { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void Update(string vehicleType, string? description, decimal? capacityKg, bool isActive, DateTimeOffset now)
    {
        if (capacityKg <= 0) throw new DomainException("Capacity must be positive.");
        VehicleType = Guard.Required(vehicleType, "VehicleType", 100);
        Description = description?.Trim();
        CapacityKg = capacityKg;
        IsActive = isActive;
        Touch(now);
    }
}

public sealed class ServiceType : Entity
{
    private ServiceType() { }

    public ServiceType(ServiceTypeCode code, string name, string? description, bool requiresDestination)
    {
        Code = code;
        Name = Guard.Required(name, "Name", 100);
        Description = description?.Trim();
        RequiresDestination = requiresDestination;
    }

    public ServiceTypeCode Code { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool RequiresDestination { get; private set; }
    public decimal? BasePrice { get; private set; }
    public decimal? PricePerKm { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void ConfigurePricing(decimal basePrice, decimal? pricePerKm, DateTimeOffset now)
    {
        if (basePrice <= 0 || decimal.Round(basePrice, 2) != basePrice)
            throw new DomainException("Base price must be positive with at most two decimal places.");
        if (Code == ServiceTypeCode.Tow)
        {
            if (pricePerKm is null or <= 0 || decimal.Round(pricePerKm.Value, 2) != pricePerKm)
                throw new DomainException("Towing price per kilometer must be positive with at most two decimal places.");
        }
        else if (pricePerKm is not null)
            throw new DomainException("Only towing can have a price per kilometer.");

        BasePrice = basePrice;
        PricePerKm = pricePerKm;
        Touch(now);
    }

    public void SetActive(bool active, DateTimeOffset now)
    {
        IsActive = active;
        Touch(now);
    }
}

public sealed class ProviderServiceType
{
    private ProviderServiceType() { }
    public ProviderServiceType(Guid providerId, Guid serviceTypeId)
    {
        ProviderId = providerId;
        ServiceTypeId = serviceTypeId;
    }

    public Guid ProviderId { get; private set; }
    public Guid ServiceTypeId { get; private set; }
    public bool IsActive { get; private set; } = true;
}
