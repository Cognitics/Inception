
using UnityEngine;

namespace Cognitics.Unity
{
    public class WorldPlane
    {
        LocalTangentPlane TangentPlane;

        public double OriginLatitude => TangentPlane.OriginLatitude;
        public double OriginLongitude => TangentPlane.OriginLongitude;
        public readonly double Scale;

        public WorldPlane(double origin_latitude, double origin_longitude, double scale)
        {
            TangentPlane = new LocalTangentPlane(origin_latitude, origin_longitude, 0.0);
            Scale = scale;
        }

        public void ConvertFromGeodetic(double latitude, double longitude, double altitude, out Vector3 position)
        {
            TangentPlane.ConvertFromGeodetic(latitude, longitude, altitude, out double east, out double north, out double up);
            position.x = (float)(east * Scale);
            position.y = (float)(up * Scale);
            position.z = (float)(north * Scale);
        }

        public void ConvertToGeodetic(Vector3 position, out double latitude, out double longitude, out double altitude)
        {
            TangentPlane.ConvertToGeodetic(position.x / Scale, position.z / Scale, position.y / Scale, out latitude, out longitude, out altitude);
        }


    }

}
