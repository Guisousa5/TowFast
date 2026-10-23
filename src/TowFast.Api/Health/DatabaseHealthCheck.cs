using Microsoft.Extensions.Diagnostics.HealthChecks;
using TowFast.Infrastructure.Persistence;

namespace TowFast.Api.Health;

public sealed class DatabaseHealthCheck(TowFastDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Database is unavailable.");
        }
        catch
        {
            return HealthCheckResult.Unhealthy("Database is unavailable.");
        }
    }
}
