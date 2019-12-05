
namespace Cognitics.CoordinateSystems
{
    public interface IGeodeticTransform
    {
        void GeodeticToECEF(double latitude, double longitude, double altitude, out double x, out double y, out double z);
        void ECEFtoGeodetic(double x, double y, double z, out double latitude, out double longitude, out double altitude);
    }

}
