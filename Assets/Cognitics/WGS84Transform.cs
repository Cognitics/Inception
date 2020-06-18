
using System;

namespace Cognitics.CoordinateSystems
{
    public class WGS84Transform : IGeodeticTransform
    {
        public void GeodeticToECEF(double latitude, double longitude, double altitude, out double x, out double y, out double z)
        {
            var lambda = latitude * Math.PI / 180.0;
            var sin_lambda = Math.Sin(lambda);
            var cos_lambda = Math.Cos(lambda);
            var phi = longitude * Math.PI / 180.0;
            var sin_phi = Math.Sin(phi);
            var cos_phi = Math.Cos(phi);
            var PrimeVerticalOfCurvature = WGS84.EquatorialRadius / Math.Sqrt(1.0 - (WGS84.SquaredEccentricity * sin_lambda * sin_lambda));
            x = (altitude + PrimeVerticalOfCurvature) * cos_lambda * cos_phi;
            y = (altitude + PrimeVerticalOfCurvature) * cos_lambda * sin_phi;
            z = (altitude + ((1.0 - WGS84.SquaredEccentricity) * PrimeVerticalOfCurvature)) * sin_lambda;
        }

        public void ECEFtoGeodetic(double x, double y, double z, out double latitude, out double longitude, out double altitude)
        {
            var eps = WGS84.SquaredEccentricity / (1.0 - WGS84.SquaredEccentricity);
            var p = Math.Sqrt((x * x) + (y * y));
            var q = Math.Atan2(z * WGS84.EquatorialRadius, p * WGS84.PolarRadius);
            var sin_q = Math.Sin(q);
            var cos_q = Math.Cos(q);
            var sin_q3 = sin_q * sin_q * sin_q;
            var cos_q3 = cos_q * cos_q * cos_q;
            var phi = Math.Atan2(z + (eps * WGS84.PolarRadius * sin_q3), p - (WGS84.SquaredEccentricity * WGS84.EquatorialRadius * cos_q3));
            var lambda = Math.Atan2(y, x);
            var v = WGS84.EquatorialRadius / Math.Sqrt(1.0 - (WGS84.SquaredEccentricity * Math.Sin(phi) * Math.Sin(phi)));
            latitude = phi * 180.0 / Math.PI;
            longitude = lambda * 180.0 / Math.PI;
            altitude = (p / Math.Cos(phi)) - v;
        }
    }

}


