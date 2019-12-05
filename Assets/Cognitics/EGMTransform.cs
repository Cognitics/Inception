
namespace Cognitics.CoordinateSystems
{
    public class EGMTransform : IGeodeticTransform
    {
        EGM EGM;
        IGeodeticTransform GeodeticTransform;

        public EGMTransform(EGM egm, IGeodeticTransform geodeticTransform)
        {
            EGM = egm;
            GeodeticTransform = geodeticTransform;
        }

        public void GeodeticToECEF(double latitude, double longitude, double altitude, out double x, out double y, out double z)
        {
            altitude -= EGM.Height(latitude, longitude);
            GeodeticTransform.GeodeticToECEF(latitude, longitude, altitude, out x, out y, out z);
        }

        public void ECEFtoGeodetic(double x, double y, double z, out double latitude, out double longitude, out double altitude)
        {
            GeodeticTransform.ECEFtoGeodetic(x, y, z, out latitude, out longitude, out altitude);
            altitude += EGM.Height(latitude, longitude);
        }
    }

}


