namespace TowFast.Application.Common;

public abstract class AppException(string message) : Exception(message);
public sealed class ValidationException(string message) : AppException(message);
public sealed class UnauthorizedException(string message) : AppException(message);
public sealed class ForbiddenException(string message) : AppException(message);
public sealed class NotFoundException(string message) : AppException(message);
public sealed class ConflictException(string message) : AppException(message);

public static class PasswordRules
{
    public static void Validate(string password)
    {
        if (password.Length < 8 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
            throw new ValidationException("Password must contain at least 8 characters, upper, lower and digit.");
    }
}

public sealed record PageResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total)
{
    public int TotalPages => Total == 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}
