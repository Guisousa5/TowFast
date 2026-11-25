using TowFast.Application.Common;
using TowFast.Application.Services;
using TowFast.Domain.Customers;
using TowFast.Domain.Common;
using TowFast.Domain.Providers;

namespace TowFast.Application.Tests;

public sealed class PricingCalculatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Fixed_service_returns_configured_amount()
    {
        var service = new ServiceType(ServiceTypeCode.TireChange, "Troca de pneu", null, false);
        service.ConfigurePricing(90m, null, Now);

        var quote = PricingCalculator.Quote(service, Point(0m, 0m), null);

        Assert.Equal(90m, quote.Price);
        Assert.Equal("BRL", quote.Currency);
        Assert.Null(quote.EstimatedDistanceKm);
    }

    [Fact]
    public void Tow_uses_approximate_distance_and_rounds_total()
    {
        var service = new ServiceType(ServiceTypeCode.Tow, "Reboque", null, true);
        service.ConfigurePricing(100m, 3m, Now);

        var quote = PricingCalculator.Quote(service, Point(0m, 0m), Point(0m, 1m));

        Assert.Equal(111.19m, quote.EstimatedDistanceKm);
        Assert.Equal(433.57m, quote.Price);
    }

    [Fact]
    public void Missing_pricing_rejects_quote()
    {
        var service = new ServiceType(ServiceTypeCode.TireChange, "Troca de pneu", null, false);
        Assert.Throws<ValidationException>(() => PricingCalculator.Quote(service, Point(0m, 0m), null));
    }

    [Fact]
    public void Pricing_configuration_rejects_per_kilometer_rate_on_fixed_service()
    {
        var service = new ServiceType(ServiceTypeCode.BatteryReplacement, "Troca de bateria", null, false);
        Assert.Throws<DomainException>(() => service.ConfigurePricing(80m, 2m, Now));
    }

    private static Location Point(decimal latitude, decimal longitude) =>
        new(null, "Local", "Rua", null, null, null, "São Paulo", "SP", null, null,
            latitude, longitude, false);
}
