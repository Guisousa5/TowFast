using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TowFast.Application.Abstractions;
using TowFast.Infrastructure.Persistence;
using TowFast.Infrastructure.Security;

namespace TowFast.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TowFast")
            ?? throw new InvalidOperationException("ConnectionStrings:TowFast is required.");
        services.AddDbContext<TowFastDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<ITowFastStore, EfTowFastStore>();
        services.AddScoped<DatabaseInitializer>();

        services.AddSingleton<IClock, SystemClock>();
        var staleSeconds = configuration.GetValue("Tracking:StaleAfterSeconds", 30);
        var maxAgeSeconds = configuration.GetValue("Tracking:MaxSampleAgeSeconds", 3600);
        var futureToleranceSeconds = configuration.GetValue("Tracking:FutureToleranceSeconds", 60);
        if (staleSeconds <= 0 || maxAgeSeconds <= 0 || futureToleranceSeconds < 0)
            throw new InvalidOperationException("Tracking time settings are invalid.");
        services.AddSingleton(new TrackingPolicy(TimeSpan.FromSeconds(staleSeconds),
            TimeSpan.FromSeconds(maxAgeSeconds), TimeSpan.FromSeconds(futureToleranceSeconds)));
        var maxPageSize = configuration.GetValue("Pagination:MaxPageSize", 100);
        if (maxPageSize is < 1 or > 1000) throw new InvalidOperationException("Pagination:MaxPageSize must be between 1 and 1000.");
        services.AddSingleton(new PaginationPolicy(maxPageSize));
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
        return services;
    }
}
