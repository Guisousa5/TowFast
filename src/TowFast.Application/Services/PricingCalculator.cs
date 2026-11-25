using TowFast.Application.Common;
using TowFast.Application.Contracts;
using TowFast.Domain.Customers;
using TowFast.Domain.Providers;

namespace TowFast.Application.Services;

public static class PricingCalculator
{
    public static PriceQuoteResponse Quote(ServiceType serviceType, Location origin, Location? destination)
    {
        if (serviceType.BasePrice is null)
            throw new ValidationException("O preço deste serviço ainda não foi configurado.");

        if (serviceType.Code != ServiceTypeCode.Tow)
            return new PriceQuoteResponse(serviceType.BasePrice.Value, "BRL", null);

        if (serviceType.PricePerKm is null)
            throw new ValidationException("O valor por quilômetro do reboque ainda não foi configurado.");
        if (destination is null || !origin.Latitude.HasValue || !origin.Longitude.HasValue ||
            !destination.Latitude.HasValue || !destination.Longitude.HasValue)
            throw new ValidationException("Origem e destino precisam ter coordenadas para calcular o reboque.");

        var distanceKm = Math.Round(DistanceKm(origin.Latitude.Value, origin.Longitude.Value,
            destination.Latitude.Value, destination.Longitude.Value), 2, MidpointRounding.AwayFromZero);
        var price = Math.Round(serviceType.BasePrice.Value + distanceKm * serviceType.PricePerKm.Value,
            2, MidpointRounding.AwayFromZero);
        return new PriceQuoteResponse(price, "BRL", distanceKm);
    }

    private static decimal DistanceKm(decimal latitude1, decimal longitude1, decimal latitude2, decimal longitude2)
    {
        const double radiusKm = 6371d;
        var lat1 = ToRadians((double)latitude1);
        var lat2 = ToRadians((double)latitude2);
        var deltaLat = ToRadians((double)(latitude2 - latitude1));
        var deltaLon = ToRadians((double)(longitude2 - longitude1));
        var a = Math.Pow(Math.Sin(deltaLat / 2), 2) +
                Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(deltaLon / 2), 2);
        a = Math.Clamp(a, 0d, 1d);
        return (decimal)(radiusKm * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180d;
}
