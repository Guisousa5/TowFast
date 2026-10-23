using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TowFast.Application.Abstractions;
using TowFast.Domain.Identity;

namespace TowFast.Infrastructure.Security;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int Iterations = 120_000;
    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string passwordHash, string password)
    {
        var parts = passwordHash.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], CultureInfo.InvariantCulture, out var iterations)) return false;
        try
        {
            var salt = Convert.FromBase64String(parts[1]);
            var expected = Convert.FromBase64String(parts[2]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(expected, actual);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";
    public string Issuer { get; init; } = "TowFast";
    public string Audience { get; init; } = "TowFast.Clients";
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 15;
    public int RefreshTokenDays { get; init; } = 30;
}

public sealed class JwtTokenIssuer(IOptions<JwtSettings> options) : ITokenIssuer
{
    private readonly JwtSettings _settings = options.Value;

    public IssuedTokens Issue(User user, DateTimeOffset now)
    {
        var expires = now.AddMinutes(_settings.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };
        Add(claims, "client_id", user.ClientId);
        Add(claims, "provider_id", user.ProviderId);
        Add(claims, "driver_id", user.DriverId);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_settings.Issuer, _settings.Audience, claims,
            notBefore: now.UtcDateTime, expires: expires.UtcDateTime, signingCredentials: credentials);
        var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return new IssuedTokens(new JwtSecurityTokenHandler().WriteToken(token), expires, refresh,
            HashRefreshToken(refresh), now.AddDays(_settings.RefreshTokenDays));
    }

    public string HashRefreshToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static void Add(ICollection<Claim> claims, string type, Guid? value)
    {
        if (value.HasValue) claims.Add(new Claim(type, value.Value.ToString()));
    }
}
