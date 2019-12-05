
using System;

namespace Cognitics.CoordinateSystems
{
    public class EllipsoidTangentPlane : ILocalTangentPlane
    {
        public readonly double OriginLatitude;
        public readonly double OriginLongitude;
        public readonly double OriginAltitude;

        readonly double lambda;
        readonly double phi;
        readonly double sin_lambda;
        readonly double cos_lambda;
        readonly double sin_phi;
        readonly double cos_phi;
        readonly double surface_radius;
        readonly double origin_x;
        readonly double origin_y;
        readonly double origin_z;

        WGS84Transform WGS84Transform = new WGS84Transform();

        public EllipsoidTangentPlane(double origin_latitude, double origin_longitude, double origin_altitude = 0.0)
        {
            OriginLatitude = origin_latitude;
            OriginLongitude = origin_longitude;
            OriginAltitude = origin_altitude;
            lambda = OriginLatitude * Math.PI / 180.0;
            phi = OriginLongitude * Math.PI / 180.0;
            sin_lambda = Math.Sin(lambda);
            cos_lambda = Math.Cos(lambda);
            sin_phi = Math.Sin(phi);
            cos_phi = Math.Cos(phi);
            surface_radius = WGS84.EquatorialRadius / Math.Sqrt(1.0 - (WGS84.SquaredEccentricity * sin_lambda * sin_lambda));
            origin_x = (OriginAltitude + surface_radius) * cos_lambda * cos_phi;
            origin_y = (OriginAltitude + surface_radius) * cos_lambda * sin_phi;
            origin_z = (OriginAltitude + ((1.0 - WGS84.SquaredEccentricity) * surface_radius)) * sin_lambda;
        }

        public void ECEFtoLocal(double x, double y, double z, out double east, out double north, out double up)
        {
            double dx = x - origin_x;
            double dy = y - origin_y;
            double dz = z - origin_z;
            east = (-sin_phi * dx) + (cos_phi * dy);
            north = (sin_lambda * -cos_phi * dx) - (sin_lambda * sin_phi * dy) + (cos_lambda * dz);
            up = (cos_lambda * cos_phi * dx) + (cos_lambda * sin_phi * dy) + (sin_lambda * dz);
        }

        public void LocalToECEF(double east, double north, double up, out double x, out double y, out double z)
        {
            x = origin_x + (-sin_phi * east) - (sin_lambda * cos_phi * north) + (cos_lambda * cos_phi * up);
            y = origin_y + (cos_phi * east) - (sin_lambda * sin_phi * north) + (cos_lambda * sin_phi * up);
            z = origin_z + (cos_lambda * north) + (sin_lambda * up);
        }

        public virtual void GeodeticToLocal(double latitude, double longitude, double altitude, out double east, out double north, out double up)
        {
            WGS84Transform.GeodeticToECEF(latitude, longitude, altitude, out double x, out double y, out double z);
            ECEFtoLocal(x, y, z, out east, out north, out up);
        }

        public virtual void LocalToGeodetic(double east, double north, double up, out double latitude, out double longitude, out double altitude)
        {
            LocalToECEF(east, north, up, out double x, out double y, out double z);
            WGS84Transform.ECEFtoGeodetic(x, y, z, out latitude, out longitude, out altitude);
        }


    }

}


