
using System;

namespace Cognitics.CoordinateSystems
{
    static public class WGS84
    {
        // Defining Parameters
        public const double EquatorialRadius = 6378137.0;                           // semi-major axis (a) (meters)
        public const double Flattening = 1 / 298.257223563;                         // flattening (f)
        public const double AngularVelocity = 7292115.1467 * 10e-11;                // radians / second
        public const double GravitationalConstant = 3986004.418 * 10e8;             // GM (meters^3 / second^2)

        // Derived Parameters
        public const double PolarRadius = EquatorialRadius * (1 - Flattening);      // semi-minor axis (meters)
        public const double SquaredEccentricity = Flattening * (2 - Flattening);

        public const double EquatorialCircumference = 2 * Math.PI * EquatorialRadius;

    }

}

