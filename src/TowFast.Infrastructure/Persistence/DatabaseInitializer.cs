using Microsoft.EntityFrameworkCore;
using TowFast.Application.Abstractions;
using TowFast.Domain.Identity;
using TowFast.Domain.Providers;
using TowFast.Infrastructure.Security;

namespace TowFast.Infrastructure.Persistence;

public sealed class DatabaseInitializer(
    TowFastDbContext db,
    IPasswordHasher passwordHasher)
{
    public async Task InitializeAsync(string? adminEmail, string? adminPassword, CancellationToken cancellationToken)
    {
        if (db.Database.IsRelational()) await db.Database.MigrateAsync(cancellationToken);
        else await db.Database.EnsureCreatedAsync(cancellationToken);

        if (!await db.ServiceTypes.AnyAsync(cancellationToken))
        {
            db.ServiceTypes.AddRange(
                new ServiceType(ServiceTypeCode.Tow, "Reboque", "Remocao do veiculo", true),
                new ServiceType(ServiceTypeCode.BatteryReplacement, "Troca de bateria", null, false),
                new ServiceType(ServiceTypeCode.TireChange, "Troca de pneu", null, false));
        }

        if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword) &&
            !await db.Users.AnyAsync(x => x.Role == UserRole.Admin, cancellationToken))
        {
            db.Users.Add(new User(adminEmail, passwordHasher.Hash(adminPassword), UserRole.Admin));
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
