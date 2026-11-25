using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using TowFast.Application.Contracts;
using TowFast.Domain.Customers;
using TowFast.Domain.Providers;
using TowFast.Infrastructure.Persistence;

namespace TowFast.IntegrationTests;

public sealed class PricingApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact]
    public async Task Client_confirms_quote_and_request_keeps_price_after_tariff_change()
    {
        var service = new ServiceType(ServiceTypeCode.TireChange, "Troca de pneu", null, false);
        service.ConfigurePricing(85m, null, DateTimeOffset.UtcNow);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TowFastDbContext>();
            db.ServiceTypes.Add(service);
            await db.SaveChangesAsync();
        }

        using var client = factory.CreateClient();
        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterClientCommand(
            "Pricing Client", DocumentType.Cpf, "52998224725", $"price-{Guid.NewGuid():N}@towfast.test",
            "11999999999", "StrongPass1"));
        register.EnsureSuccessStatusCode();
        var auth = await register.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.NotNull(auth?.User.ClientId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var unauthorizedPricing = await client.PatchAsJsonAsync(
            $"/api/v1/service-types/{service.Id}/pricing", new ConfigureServicePriceCommand(1m, null));
        Assert.Equal(HttpStatusCode.Forbidden, unauthorizedPricing.StatusCode);

        var vehicleResponse = await client.PostAsJsonAsync($"/api/v1/clients/{auth.User.ClientId}/vehicles",
            new VehicleCreateCommand("ABC1234", "Fiat", "Uno", 2020, null, null));
        Assert.True(vehicleResponse.IsSuccessStatusCode,
            $"Vehicle HTTP {(int)vehicleResponse.StatusCode}: {await vehicleResponse.Content.ReadAsStringAsync()}; " +
            $"Authentication: {string.Join(',', vehicleResponse.Headers.WwwAuthenticate)}");
        var vehicle = await vehicleResponse.Content.ReadFromJsonAsync<VehicleResponse>(JsonOptions);
        Assert.NotNull(vehicle);

        var origin = new LocationInput("Origem", "Rua A", "1", null, null, "São Paulo", "SP",
            null, null, -23.550520m, -46.633308m);
        var command = new CreateServiceRequestCommand(vehicle.Id, service.Id, origin, null, null);
        var quoteResponse = await client.PostAsJsonAsync("/api/v1/service-requests/quote", command);
        quoteResponse.EnsureSuccessStatusCode();
        var quote = await quoteResponse.Content.ReadFromJsonAsync<PriceQuoteResponse>(JsonOptions);
        Assert.NotNull(quote);
        Assert.Equal(85m, quote.Price);

        var stale = await client.PostAsJsonAsync("/api/v1/service-requests", command with { ExpectedPrice = 80m });
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);

        var create = await client.PostAsJsonAsync("/api/v1/service-requests", command with { ExpectedPrice = quote.Price });
        create.EnsureSuccessStatusCode();
        var request = await create.Content.ReadFromJsonAsync<ServiceRequestResponse>(JsonOptions);
        Assert.NotNull(request);
        Assert.Equal(85m, request.QuotedPrice);

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<TowFastDbContext>();
            var current = await db.ServiceTypes.FindAsync(service.Id);
            Assert.NotNull(current);
            current.ConfigurePricing(110m, null, DateTimeOffset.UtcNow);
            await db.SaveChangesAsync();
        }

        var detail = await client.GetFromJsonAsync<ServiceRequestDetailResponse>(
            $"/api/v1/service-requests/{request.PublicId}", JsonOptions);
        Assert.Equal(85m, detail?.Request.QuotedPrice);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
