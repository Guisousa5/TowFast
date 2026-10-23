using TowFast.Application.Abstractions;
using TowFast.Application.Common;
using TowFast.Application.Contracts;
using TowFast.Domain.Customers;
using TowFast.Domain.Identity;

namespace TowFast.Application.Services;

public sealed class AuthService(ITowFastStore store, IPasswordHasher passwordHasher, ITokenIssuer tokenIssuer, IClock clock)
{
    public Task<AuthResponse> RegisterAsync(RegisterClientCommand command, CancellationToken cancellationToken) =>
        RegisterClientAsync(command.Name, command.DocumentType, command.Document, command.Email,
            command.Phone, command.Password, cancellationToken);

    public async Task<AuthResponse> RegisterClientAsync(string name, DocumentType documentType, string document,
        string email, string phone, string password, CancellationToken cancellationToken)
    {
        PasswordRules.Validate(password);
        return await store.InTransactionAsync(async ct =>
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var normalizedDocument = new string(document.Where(char.IsDigit).ToArray());
            if (await store.AnyAsync<User>(x => x.Email == normalizedEmail, ct) ||
                await store.AnyAsync<Client>(x => x.Email == normalizedEmail || x.Document == normalizedDocument, ct))
            {
                throw new ConflictException("Email or document is already registered.");
            }

            var client = new Client(name, documentType, document, email, phone);
            var user = new User(email, passwordHasher.Hash(password), UserRole.Client);
            user.BindClient(client.Id);
            await store.AddAsync(client, ct);
            await store.AddAsync(user, ct);

            var issued = tokenIssuer.Issue(user, clock.UtcNow);
            await store.AddAsync(new RefreshToken(user.Id, issued.RefreshTokenHash, issued.RefreshExpiresAt, clock.UtcNow), ct);
            await store.SaveChangesAsync(ct);
            return MapAuth(user, issued);
        }, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var email = command.Email.Trim().ToLowerInvariant();
        var user = await store.FirstAsync<User>(x => x.Email == email, cancellationToken);
        if (user is null || !user.IsActive || !passwordHasher.Verify(user.PasswordHash, command.Password))
        {
            throw new UnauthorizedException("Invalid credentials.");
        }

        var issued = tokenIssuer.Issue(user, clock.UtcNow);
        await store.AddAsync(new RefreshToken(user.Id, issued.RefreshTokenHash, issued.RefreshExpiresAt, clock.UtcNow), cancellationToken);
        await store.SaveChangesAsync(cancellationToken);
        return MapAuth(user, issued);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await store.InTransactionAsync<AuthResponse?>(async ct =>
            {
                var now = clock.UtcNow;
                var hash = tokenIssuer.HashRefreshToken(command.RefreshToken);
                var current = await store.FirstAsync<RefreshToken>(x => x.TokenHash == hash, ct);
                if (current is null || current.ExpiresAt <= now) return null;

                if (current.RevokedAt is not null)
                {
                    var activeTokens = await store.ListAsync<RefreshToken>(x => x.UserId == current.UserId &&
                        x.RevokedAt == null && x.ExpiresAt > now, ct);
                    foreach (var activeToken in activeTokens) activeToken.Revoke(now);
                    await store.SaveChangesAsync(ct);
                    return null;
                }

                var user = await store.FindAsync<User>(current.UserId, ct);
                if (user is null || !user.IsActive) return null;

                var issued = tokenIssuer.Issue(user, now);
                var replacement = new RefreshToken(user.Id, issued.RefreshTokenHash, issued.RefreshExpiresAt, now);
                current.Revoke(now, replacement.Id);
                await store.AddAsync(replacement, ct);
                await store.SaveChangesAsync(ct);
                return MapAuth(user, issued);
            }, cancellationToken);
            return result ?? throw new UnauthorizedException("Refresh token is invalid, expired or reused.");
        }
        catch (Exception exception) when (exception.GetType().Name.Contains("Concurrency", StringComparison.Ordinal))
        {
            throw new UnauthorizedException("Refresh token was already used.");
        }
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var hash = tokenIssuer.HashRefreshToken(refreshToken);
        var token = await store.FirstAsync<RefreshToken>(x => x.TokenHash == hash, cancellationToken);
        if (token is not null && token.IsValid(clock.UtcNow))
        {
            token.Revoke(clock.UtcNow);
            await store.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> IsActiveAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await store.FindAsync<User>(userId, cancellationToken);
        return user?.IsActive == true;
    }

    private static AuthResponse MapAuth(User user, IssuedTokens tokens) => new(
        tokens.AccessToken, tokens.AccessExpiresAt, tokens.RefreshToken, tokens.RefreshExpiresAt,
        new UserResponse(user.Id, user.Email, user.Role, user.ClientId, user.ProviderId, user.DriverId));
}
