using SparkWork2.Models;

namespace SparkWork2.Services;

public class DistanceService
{
    private const double EarthRadiusKm = 6371.0;

    public bool CanCalculateDistance(CandidateProfile candidate, JobOffer jobOffer)
    {
        return HasCoordinates(candidate.Latitude, candidate.Longitude) &&
               HasCoordinates(jobOffer.Latitude, jobOffer.Longitude);
    }

    public double CalculateDistanceKm(CandidateProfile candidate, JobOffer jobOffer)
    {
        return CalculateDistanceKm(
            candidate.Latitude,
            candidate.Longitude,
            jobOffer.Latitude,
            jobOffer.Longitude);
    }

    public string GetDistanceDisplay(CandidateProfile candidate, JobOffer jobOffer)
    {
        if (!CanCalculateDistance(candidate, jobOffer))
            return string.Empty;

        double distance = CalculateDistanceKm(candidate, jobOffer);

        if (distance < 1)
            return "À moins de 1 km de toi";

        return $"À environ {Math.Round(distance)} km de toi";
    }

    private double CalculateDistanceKm(
        double latitude1,
        double longitude1,
        double latitude2,
        double longitude2)
    {
        double dLat = DegreesToRadians(latitude2 - latitude1);
        double dLon = DegreesToRadians(longitude2 - longitude1);

        double lat1 = DegreesToRadians(latitude1);
        double lat2 = DegreesToRadians(latitude2);

        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2) *
            Math.Cos(lat1) * Math.Cos(lat2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    private double DegreesToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    private bool HasCoordinates(double latitude, double longitude)
    {
        return latitude != 0 || longitude != 0;
    }
}
