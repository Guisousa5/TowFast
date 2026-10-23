using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TowFast.Infrastructure.Persistence;

namespace TowFast.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"towfast-tests-{Guid.NewGuid():N}";

    public ApiFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "test-only-signing-key-with-at-least-thirty-two-bytes-123456");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "TowFast.Tests",
            ["Jwt:Audience"] = "TowFast.Tests",
            ["Jwt:SigningKey"] = "test-only-signing-key-with-at-least-thirty-two-bytes-123456",
            ["Database:AutoMigrate"] = "false",
            ["Swagger:Enabled"] = "true"
        }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<TowFastDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<TowFastDbContext>>();
            services.RemoveAll<TowFastDbContext>();
            services.AddDbContext<TowFastDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}
