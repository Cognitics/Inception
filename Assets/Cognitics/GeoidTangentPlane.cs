
namespace Cognitics.CoordinateSystems
{
    public class GeoidTangentPlane : EllipsoidTangentPlane
    {
        public readonly EGM EGM;
        EGMTransform Transform;

        public GeoidTangentPlane(EGM egm, double origin_latitude, double origin_longitude, double origin_altitude = 0.0) : base(origin_latitude, origin_longitude, origin_altitude)
        {
            EGM = egm;
            Transform = new EGMTransform(egm, new WGS84Transform());
        }

        public override void GeodeticToLocal(double latitude, double longitude, double altitude, out double east, out double north, out double up)
        {
            Transform.GeodeticToECEF(latitude, longitude, altitude, out double x, out double y, out double z);
            ECEFtoLocal(x, y, z, out east, out north, out up);
        }

        public override void LocalToGeodetic(double east, double north, double up, out double latitude, out double longitude, out double altitude)
        {
            LocalToECEF(east, north, up, out double x, out double y, out double z);
            Transform.ECEFtoGeodetic(x, y, z, out latitude, out longitude, out altitude);
        }
    }

}



