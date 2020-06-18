
using System;

namespace Cognitics.CoordinateSystems
{
    public class SphereTangentPlane : ILocalTangentPlane
    {
        public readonly double OriginLatitude;
        public readonly double OriginLongitude;
        public readonly double OriginAltitude;

        //const double metersPerNauticalMile = 1852.0f;
        //const double nauticalMilesForSphericalEarthCircumference = 21600.0f;
        //const double nauticalMilesPerDegree = nauticalMilesForSphericalEarthCircumference / 360.0f;   // 21,600 / 360 = 60
        //const double metersPerDegree = nauticalMilesPerDegree * metersPerNauticalMile;        // 60 * 1852 = 111,120
        private const double metersPerDegreeLongitude = 111120.0f;
        private readonly double metersPerDegreeLatitude;

        public SphereTangentPlane(double originLatitude, double originLongitude, double originAltitude = 0.0)
        {
            OriginLatitude = originLatitude;
            OriginLongitude = originLongitude;
            OriginAltitude = originAltitude;
            metersPerDegreeLatitude = Math.Cos(OriginLatitude * (Math.PI / 180f)) * metersPerDegreeLongitude;
        }

        public double Latitude(double y) => (OriginLatitude + (y / metersPerDegreeLongitude));
        public double Longitude(double x) => (OriginLongitude + (x / metersPerDegreeLatitude));
        public double X(double longitude) => (longitude - OriginLongitude) * metersPerDegreeLatitude;
        public double Y(double latitude) => (latitude - OriginLatitude) * metersPerDegreeLongitude;

        public void GeodeticToLocal(double latitude, double longitude, double altitude, out double east, out double north, out double up)
        {
            east = X(longitude);
            north = Y(latitude);
            up = altitude - OriginAltitude;
        }

        public void LocalToGeodetic(double east, double north, double up, out double latitude, out double longitude, out double altitude)
        {
            latitude = Latitude(north);
            longitude = Longitude(east);
            altitude = up + OriginAltitude;
        }

    }
}
