using TowFast.Domain.Common;

namespace TowFast.Domain.Customers;

public enum DocumentType
{
    Cpf,
    Cnpj
}

public sealed class Client : Entity
{
    private Client() { }

    public Client(string name, DocumentType documentType, string document, string email, string phone)
    {
        Name = Guard.Required(name, "Name", 200);
        DocumentType = documentType;
        Document = Guard.CpfOrCnpj(document, requireCpf: documentType == DocumentType.Cpf,
            requireCnpj: documentType == DocumentType.Cnpj);
        Email = Guard.Email(email);
        Phone = Guard.Digits(phone, "Phone", 10, 11);
    }

    public string Name { get; private set; } = string.Empty;
    public DocumentType DocumentType { get; private set; }
    public string Document { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void Update(string name, string phone, string? notes, DateTimeOffset now)
    {
        Name = Guard.Required(name, "Name", 200);
        Phone = Guard.Digits(phone, "Phone", 10, 11);
        Notes = notes?.Trim();
        Touch(now);
    }

    public void SetActive(bool active, DateTimeOffset now)
    {
        IsActive = active;
        Touch(now);
    }
}

public sealed class ClientVehicle : Entity
{
    private ClientVehicle() { }

    public ClientVehicle(Guid clientId, string plate, string manufacturer, string model, int? modelYear, string? color, string? notes)
    {
        ClientId = clientId;
        LicensePlate = Guard.Plate(plate);
        Manufacturer = Guard.Required(manufacturer, "Manufacturer", 100);
        Model = Guard.Required(model, "Model", 100);
        if (modelYear is < 1900 or > 2200)
        {
            throw new DomainException("Model year is invalid.");
        }

        ModelYear = modelYear;
        Color = color?.Trim();
        Notes = notes?.Trim();
    }

    public Guid ClientId { get; private set; }
    public string LicensePlate { get; private set; } = string.Empty;
    public string Manufacturer { get; private set; } = string.Empty;
    public string Model { get; private set; } = string.Empty;
    public int? ModelYear { get; private set; }
    public string? Color { get; private set; }
    public string? Notes { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void SetActive(bool active, DateTimeOffset now)
    {
        IsActive = active;
        Touch(now);
    }
}

public sealed class Location : Entity
{
    private Location() { }

    public Location(
        Guid? clientId,
        string label,
        string street,
        string? number,
        string? complement,
        string? district,
        string city,
        string state,
        string? postalCode,
        string? reference,
        decimal? latitude,
        decimal? longitude,
        bool isSaved)
    {
        if (latitude.HasValue != longitude.HasValue)
        {
            throw new DomainException("Latitude and longitude must be provided together.");
        }

        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            throw new DomainException("Coordinates are invalid.");
        }

        ClientId = clientId;
        Label = Guard.Required(label, "Label", 80);
        Street = Guard.Required(street, "Street", 200);
        Number = number?.Trim();
        Complement = complement?.Trim();
        District = district?.Trim();
        City = Guard.Required(city, "City", 120);
        State = Guard.Required(state, "State", 2).ToUpperInvariant();
        PostalCode = postalCode?.Trim();
        Reference = reference?.Trim();
        Latitude = latitude;
        Longitude = longitude;
        IsSaved = isSaved;
    }

    public Guid? ClientId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public string Street { get; private set; } = string.Empty;
    public string? Number { get; private set; }
    public string? Complement { get; private set; }
    public string? District { get; private set; }
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string? PostalCode { get; private set; }
    public string? Reference { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public bool IsSaved { get; private set; }

    public Location Snapshot(string label)
    {
        if (Latitude is null || Longitude is null)
        {
            throw new DomainException("Service request locations require coordinates.");
        }

        return new Location(null, label, Street, Number, Complement, District, City, State,
            PostalCode, Reference, Latitude, Longitude, false);
    }
}
