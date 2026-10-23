namespace TowFast.Domain.Common;

public sealed class DomainException(string message) : Exception(message);

public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; protected set; } = DateTimeOffset.UtcNow;

    protected void Touch(DateTimeOffset now) => UpdatedAt = now;
}

public static class Guard
{
    public static string Required(string? value, string name, int maxLength = 500)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new DomainException($"{name} is required.");
        }

        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{name} exceeds {maxLength} characters.");
        }

        return normalized;
    }

    public static string Email(string value)
    {
        var normalized = Required(value, "Email", 254).ToLowerInvariant();
        if (!System.Net.Mail.MailAddress.TryCreate(normalized, out _))
        {
            throw new DomainException("Email is invalid.");
        }

        return normalized;
    }

    public static string Digits(string value, string name, params int[] lengths)
    {
        var normalized = new string(Required(value, name).Where(char.IsDigit).ToArray());
        if (!lengths.Contains(normalized.Length))
        {
            throw new DomainException($"{name} has an invalid length.");
        }

        return normalized;
    }

    public static string Plate(string value)
    {
        var normalized = new string(Required(value, "LicensePlate", 8)
            .Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        if (normalized.Length != 7)
        {
            throw new DomainException("License plate must contain 7 characters.");
        }

        return normalized;
    }

    public static string CpfOrCnpj(string value, bool requireCpf = false, bool requireCnpj = false)
    {
        var digits = Digits(value, "Document", requireCpf ? [11] : requireCnpj ? [14] : [11, 14]);
        var valid = digits.Length == 11 ? IsValidCpf(digits) : IsValidCnpj(digits);
        if (!valid) throw new DomainException("Document is invalid.");
        return digits;
    }

    private static bool IsValidCpf(string value)
    {
        if (value.Distinct().Count() == 1) return false;
        var first = CalculateDigit(value[..9], [10, 9, 8, 7, 6, 5, 4, 3, 2]);
        var second = CalculateDigit(value[..9] + first, [11, 10, 9, 8, 7, 6, 5, 4, 3, 2]);
        return value.EndsWith($"{first}{second}", StringComparison.Ordinal);
    }

    private static bool IsValidCnpj(string value)
    {
        if (value.Distinct().Count() == 1) return false;
        var first = CalculateDigit(value[..12], [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
        var second = CalculateDigit(value[..12] + first, [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
        return value.EndsWith($"{first}{second}", StringComparison.Ordinal);
    }

    private static int CalculateDigit(string value, int[] weights)
    {
        var sum = value.Select((character, index) => (character - '0') * weights[index]).Sum();
        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }
}
