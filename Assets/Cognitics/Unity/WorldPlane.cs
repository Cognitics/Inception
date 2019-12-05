
using UnityEngine;
using Cognitics.CoordinateSystems;

namespace Cognitics.Unity
{
    public enum LocalTangentPlaneModel { FlatEarth, Ellipsoid }

    public class WorldPlane : MonoBehaviour
    {
        public LocalTangentPlaneModel Model = LocalTangentPlaneModel.FlatEarth;
        public double OriginLatitude = 0.0;
        public double OriginLongitude = 0.0;
        public double Scale = 0.1;

        public ILocalTangentPlane LocalTangentPlane;

        public void SetOrigin(double latitude, double longitude)
        {
            OriginLatitude = latitude;
            OriginLongitude = longitude;
            if(Model == LocalTangentPlaneModel.FlatEarth)
                LocalTangentPlane = new SphereTangentPlane(OriginLatitude, OriginLongitude);
            if(Model == LocalTangentPlaneModel.Ellipsoid)
                LocalTangentPlane = new EllipsoidTangentPlane(OriginLatitude, OriginLongitude);
        }


        public void ConvertFromGeodetic(double latitude, double longitude, double altitude, out Vector3 position)
        {
            LocalTangentPlane.GeodeticToLocal(latitude, longitude, altitude, out double east, out double north, out double up);
            position.x = (float)(east * Scale);
            position.y = (float)(up * Scale);
            position.z = (float)(north * Scale);
        }

        public void ConvertToGeodetic(Vector3 position, out double latitude, out double longitude, out double altitude)
        {
            LocalTangentPlane.LocalToGeodetic(position.x / Scale, position.z / Scale, position.y / Scale, out latitude, out longitude, out altitude);
        }

    }

}
