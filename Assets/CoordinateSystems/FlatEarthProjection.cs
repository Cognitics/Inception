
using System;

namespace Cognitics.CoordinateSystems
{
    public struct FlatEarthProjection : ICoordinateTransform<double>
    {
        public readonly GeographicCoordinates Origin;

        private readonly double scaleAtOriginLatitudeInMeters;

        //const double metersPerNauticalMile = 1852.0f;
        //const double nauticalMilesForSphericalEarthCircumference = 21600.0f;
        //const double nauticalMilesPerDegree = nauticalMilesForSphericalEarthCircumference / 360.0f;   // 21,600 / 360 = 60
        //const double metersPerDegree = nauticalMilesPerDegree * metersPerNauticalMile;        // 60 * 1852 = 111,120
        private const double metersPerDegree = 111120.0f;

        public FlatEarthProjection(GeographicCoordinates origin)
        {
            Origin = origin;
            scaleAtOriginLatitudeInMeters = GetScaleForLatitudeInMeters(Origin.Latitude);
        }

        static private double GetScaleForLatitudeInMeters(double latitude)
        {
            double originLatitudeInRadians = latitude * (Math.PI / 180.0f);
            double scaleAtOriginLatitude = Math.Cos(originLatitudeInRadians);
            double scaleAtOriginLatitudeInMeters = scaleAtOriginLatitude * metersPerDegree;
            return scaleAtOriginLatitudeInMeters;
        }

        public GeographicCoordinates From(CartesianCoordinates cartesianCoordinates)
        {
            double Latitude = Origin.Latitude + (cartesianCoordinates.Y / metersPerDegree);
            double Longitude = Origin.Longitude + (cartesianCoordinates.X / scaleAtOriginLatitudeInMeters);
            return new GeographicCoordinates(Latitude, Longitude);
        }

        public CartesianCoordinates To(GeographicCoordinates geographicCoordinates)
        {
            double y = (geographicCoordinates.Latitude - Origin.Latitude) * metersPerDegree;
            double x = (geographicCoordinates.Longitude - Origin.Longitude) * scaleAtOriginLatitudeInMeters;
            return new CartesianCoordinates(x, y);
        }

        public void TransformToCartesian(double latitude, double longitude, out double x, out double y)
        {
            x = (longitude - Origin.Longitude) * scaleAtOriginLatitudeInMeters;
            y = (latitude - Origin.Latitude) * metersPerDegree;
        }

        public void TransformToGeographic(double x, double y, out double latitude, out double longitude)
        {
            latitude = Origin.Latitude + (y / metersPerDegree);
            longitude = Origin.Longitude + (x / scaleAtOriginLatitudeInMeters);
        }


        public void Transform(Latitude latitude, Longitude longitude, out double x, out double y)
        {
            x = (longitude - Origin.Longitude) * scaleAtOriginLatitudeInMeters;
            y = (latitude - Origin.Latitude) * metersPerDegree;
        }

        public void Transform(double x, double y, out Latitude latitude, out Longitude longitude)
        {
            latitude = Origin.Latitude + (y / metersPerDegree);
            longitude = Origin.Longitude + (x / scaleAtOriginLatitudeInMeters);
        }

        public void Transform(GeographicCoordinates geographicCoordinates, out CartesianCoordinates cartesianCoordinates)
        {
            cartesianCoordinates.X = (geographicCoordinates.Longitude - Origin.Longitude) * scaleAtOriginLatitudeInMeters;
            cartesianCoordinates.Y = (geographicCoordinates.Latitude - Origin.Latitude) * metersPerDegree;
        }

        public void Transform(CartesianCoordinates cartesianCoordinates, out GeographicCoordinates geographicCoordinates)
        {
            geographicCoordinates.Latitude = Origin.Latitude + (cartesianCoordinates.Y / metersPerDegree);
            geographicCoordinates.Longitude = Origin.Longitude + (cartesianCoordinates.X / scaleAtOriginLatitudeInMeters);
        }


    }
}


