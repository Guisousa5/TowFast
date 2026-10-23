using System.Linq.Expressions;
using TowFast.Domain.Identity;
using TowFast.Domain.ServiceRequests;

namespace TowFast.Application.Abstractions;

public sealed record TrackingPolicy(TimeSpan StaleAfter, TimeSpan MaxSampleAge, TimeSpan FutureTolerance);
public sealed record PaginationPolicy(int MaxPageSize);

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}


public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string passwordHash, string password);
}

public sealed record IssuedTokens(
    string AccessToken,
    DateTimeOffset AccessExpiresAt,
    string RefreshToken,
    string RefreshTokenHash,
    DateTimeOffset RefreshExpiresAt);

public interface ITokenIssuer
{
    IssuedTokens Issue(User user, DateTimeOffset now);
    string HashRefreshToken(string token);
}

public interface ITowFastStore
{
    Task<T?> FindAsync<T>(Guid id, CancellationToken cancellationToken) where T : class;
    Task<T?> FirstAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken) where T : class;
    Task<bool> AnyAsync<T>(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken) where T : class;
    Task<List<T>> ListAsync<T>(Expression<Func<T, bool>>? predicate, CancellationToken cancellationToken) where T : class;
    Task AddAsync<T>(T entity, CancellationToken cancellationToken) where T : class;
    void Remove<T>(T entity) where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    Task<TResult> InTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken);
}

public sealed record ActorContext(
    Guid UserId,
    UserRole Role,
    Guid? ClientId,
    Guid? ProviderId,
    Guid? DriverId)
{
    public bool IsOperation => Role is UserRole.Admin or UserRole.Operator;
}
