
namespace Cognitics.CoordinateSystems
{
    public interface ILocalTangentPlane
    {
        void GeodeticToLocal(double latitude, double longitude, double altitude, out double east, out double north, out double up);
        void LocalToGeodetic(double east, double north, double up, out double latitude, out double longitude, out double altitude);
    }

}

