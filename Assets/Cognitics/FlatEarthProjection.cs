using System;
using UnityEngine;

namespace Cognitics.Unity
{
    public class FlatEarthProjection
    {
        public readonly double OriginLatitude;
        public readonly double OriginLongitude;
        public readonly double Scale;

        //const double metersPerNauticalMile = 1852.0f;
        //const double nauticalMilesForSphericalEarthCircumference = 21600.0f;
        //const double nauticalMilesPerDegree = nauticalMilesForSphericalEarthCircumference / 360.0f;   // 21,600 / 360 = 60
        //const double metersPerDegree = nauticalMilesPerDegree * metersPerNauticalMile;        // 60 * 1852 = 111,120
        private const double metersPerDegreeLongitude = 111120.0f;
        private readonly double metersPerDegreeLatitude;


        public FlatEarthProjection(double originLatitude, double originLongitude, double scale = 1.0)
        {
            OriginLatitude = originLatitude;
            OriginLongitude = originLongitude;
            Scale = scale;
            metersPerDegreeLatitude = Math.Cos(OriginLatitude * (Math.PI / 180f)) * metersPerDegreeLongitude;
        }

        public double Latitude(double y) => (OriginLatitude + (y / metersPerDegreeLongitude)) / Scale;
        public double Longitude(double x) => (OriginLongitude + (x / metersPerDegreeLatitude)) / Scale;
        public double X(double longitude) => (longitude - OriginLongitude) * metersPerDegreeLatitude * Scale;
        public double Y(double latitude) => (latitude - OriginLatitude) * metersPerDegreeLongitude * Scale;


    }
}
