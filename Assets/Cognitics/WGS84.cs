using System;

namespace Cognitics
{
    static public class WGS84
    {
        public const double EquatorialRadius = 6378137.0;                          // semi-major axis (meters)
        public const double Flattening = 1 / 298.257223563;
        public const double PolarRadius = EquatorialRadius * (1 - Flattening);     // semi-minor axis (meters)
        public const double SquaredEccentricity = Flattening * (2 - Flattening);

        public const double GravitationalConstant = 3986004.418 * 10e8;            // meters^3 / second^2
        public const double AngularVelocity = 7.2921151467 * 10e-5;                // radians / second

        public const double EquatorialCircumference = 2 * Math.PI * EquatorialRadius;

        static public void ConvertToECEF(double latitude, double longitude, double altitude, out double x, out double y, out double z)
        {
            var lambda = latitude * Math.PI / 180.0;
            var sin_lambda = Math.Sin(lambda);
            var cos_lambda = Math.Cos(lambda);
            var phi = longitude * Math.PI / 180.0;
            var sin_phi = Math.Sin(phi);
            var cos_phi = Math.Cos(phi);
            var PrimeVerticalOfCurvature = EquatorialRadius / Math.Sqrt(1.0 - (SquaredEccentricity * sin_lambda * sin_lambda));
            x = (altitude + PrimeVerticalOfCurvature) * cos_lambda * cos_phi;
            y = (altitude + PrimeVerticalOfCurvature) * cos_lambda * sin_phi;
            z = (altitude + ((1.0 - SquaredEccentricity) * PrimeVerticalOfCurvature)) * sin_lambda;
        }

        static public void ConvertFromECEF(double x, double y, double z, out double latitude, out double longitude, out double altitude)
        {
            var eps = SquaredEccentricity / (1.0 - SquaredEccentricity);
            var p = Math.Sqrt((x * x) + (y * y));
            var q = Math.Atan2(z * EquatorialRadius, p * PolarRadius);
            var sin_q = Math.Sin(q);
            var cos_q = Math.Cos(q);
            var sin_q3 = sin_q * sin_q * sin_q;
            var cos_q3 = cos_q * cos_q * cos_q;
            var phi = Math.Atan2(z + (eps * PolarRadius * sin_q3), p - (SquaredEccentricity * EquatorialRadius * cos_q3));
            var lambda = Math.Atan2(y, x);
            var v = EquatorialRadius / Math.Sqrt(1.0 - (SquaredEccentricity * Math.Sin(phi) * Math.Sin(phi)));
            latitude = phi * 180.0 / Math.PI;
            longitude = lambda * 180.0 / Math.PI;
            altitude = (p / Math.Cos(phi)) - v;
        }
    }

}
