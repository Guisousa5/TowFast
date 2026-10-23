using Microsoft.Extensions.DependencyInjection;
using TowFast.Application.Services;

namespace TowFast.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services) => services
        .AddScoped<AuthService>()
        .AddScoped<CatalogService>()
        .AddScoped<ServiceRequestService>()
        .AddScoped<TrackingService>();
}
