using System;
using System.IO;

namespace Cognitics.CoordinateSystems
{
    public struct GeographicCoordinates : IEquatable<GeographicCoordinates>
    {
        static public GeographicCoordinates MinValue => new GeographicCoordinates(Latitude.MinValue, Longitude.MinValue);
        static public GeographicCoordinates MaxValue => new GeographicCoordinates(Latitude.MaxValue, Longitude.MaxValue);

        static public bool operator ==(GeographicCoordinates a, GeographicCoordinates b)
        {
            return (a.Latitude == b.Latitude) && (a.Longitude == b.Longitude);
        }
        static public bool operator !=(GeographicCoordinates a, GeographicCoordinates b) => !(a == b);

        ////////////////////////////////////////////////////////////

        public override int GetHashCode() => System.Tuple.Create(Latitude, Longitude).GetHashCode();
        public override bool Equals(object obj) => (obj is GeographicCoordinates) && (this == (GeographicCoordinates)obj);
        public bool Equals(GeographicCoordinates obj) => this == obj;


        public Latitude Latitude;
        public Longitude Longitude;


        public GeographicCoordinates(Latitude latitude, Longitude longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        public int TileLatitude => (int)Math.Floor(Latitude);

        public int TileLongitude
        {
            get
            {
                int width = Latitude.TileWidth;
                int ilon = (int)Math.Floor(Longitude);
                ilon /= width;
                ilon *= width;
                return ilon;
            }
        }

        public string String => string.Format("({0},{1})", (double)Latitude, (double)Longitude);
        public string TileLatitudeString => string.Format("{0}{1:00}", (TileLatitude < 0) ? "S" : "N", Math.Abs(TileLatitude));
        public string TileLongitudeString => string.Format("{0}{1:000}", (TileLongitude < 0) ? "W" : "E", Math.Abs(TileLongitude));
        public string TileString => string.Format("{0}{1}", TileLatitudeString, TileLongitudeString);
        public string TileFilename => TileString;
        public string TileLatitudeSubdirectory => TileLatitudeString;
        public string TileLongitudeSubdirectory => TileLongitudeString;
        public string TileSubdirectory => Path.Combine(TileLatitudeSubdirectory, TileLongitudeSubdirectory);

        public CartesianCoordinates TransformedWith<T>(ICoordinateTransform<T> transform) => transform.To(this);



    }
}
