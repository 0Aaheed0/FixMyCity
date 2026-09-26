using System;

namespace FixMyCity.Services.Common
{
    public interface IGeoLocationService
    {
        double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2);
        bool IsWithinRadius(double centerLat, double centerLon, double pointLat, double pointLon, double radiusKm);
    }

    public class GeoLocationService : IGeoLocationService
    {
        private const double EarthRadiusKm = 6371.0;

        public double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadiusKm * c;
        }

        public bool IsWithinRadius(double centerLat, double centerLon, double pointLat, double pointLon, double radiusKm)
        {
            return CalculateDistanceKm(centerLat, centerLon, pointLat, pointLon) <= radiusKm;
        }

        private static double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }
    }
}
