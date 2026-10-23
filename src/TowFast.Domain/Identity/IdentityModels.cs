using TowFast.Domain.Common;

namespace TowFast.Domain.Identity;

public enum UserRole
{
    Admin,
    Operator,
    Client,
    Provider,
    Driver
}

public sealed class User : Entity
{
    private User() { }

    public User(string email, string passwordHash, UserRole role)
    {
        Email = Guard.Email(email);
        PasswordHash = Guard.Required(passwordHash, "PasswordHash", 1000);
        Role = role;
    }

    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public Guid? ClientId { get; private set; }
    public Guid? ProviderId { get; private set; }
    public Guid? DriverId { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void BindClient(Guid clientId) => ClientId = clientId;
    public void BindProvider(Guid providerId) => ProviderId = providerId;
    public void BindDriver(Guid providerId, Guid driverId)
    {
        ProviderId = providerId;
        DriverId = driverId;
    }

    public void SetActive(bool active, DateTimeOffset now)
    {
        IsActive = active;
        Touch(now);
    }
}

public sealed class RefreshToken : Entity
{
    private RefreshToken() { }

    public RefreshToken(Guid userId, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset now)
    {
        UserId = userId;
        TokenHash = Guard.Required(tokenHash, "TokenHash", 128);
        ExpiresAt = expiresAt;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }
    public bool IsValid(DateTimeOffset now) => RevokedAt is null && ExpiresAt > now;

    public void Revoke(DateTimeOffset now, Guid? replacementId = null)
    {
        if (RevokedAt is not null)
        {
            throw new DomainException("Refresh token is already revoked.");
        }

        RevokedAt = now;
        ReplacedByTokenId = replacementId;
        Touch(now);
    }
}
