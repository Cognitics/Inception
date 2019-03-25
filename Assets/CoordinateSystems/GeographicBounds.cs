
namespace Cognitics.CoordinateSystems
{
    public struct GeographicBounds : System.IEquatable<GeographicBounds>
    {
        static public GeographicBounds WorldValue => new GeographicBounds(GeographicCoordinates.MinValue, GeographicCoordinates.MaxValue);
        static public GeographicBounds EmptyValue => new GeographicBounds(GeographicCoordinates.MaxValue, GeographicCoordinates.MinValue);

        public static bool operator ==(GeographicBounds a, GeographicBounds b)
        {
            return (a.MinimumCoordinates == b.MinimumCoordinates) && (a.MaximumCoordinates == b.MaximumCoordinates);
        }
        public static bool operator !=(GeographicBounds a, GeographicBounds b) => !(a == b);

        ////////////////////////////////////////////////////////////

        public override int GetHashCode() => System.Tuple.Create(MinimumCoordinates, MaximumCoordinates).GetHashCode();
        public override bool Equals(object obj) => (obj is GeographicBounds) && (this == (GeographicBounds)obj);
        public bool Equals(GeographicBounds obj) => this == obj;


        public GeographicCoordinates MinimumCoordinates;
        public GeographicCoordinates MaximumCoordinates;

        public GeographicBounds(GeographicCoordinates minCoordinates, GeographicCoordinates maxCoordinates)
        {
            MinimumCoordinates = minCoordinates;
            MaximumCoordinates = maxCoordinates;
        }

        public string String => string.Format("({0},{1})", MinimumCoordinates.String, MaximumCoordinates.String);

        public GeographicCoordinates Center
        {
            get
            {
                GeographicCoordinates result;
                result.Latitude = MinimumCoordinates.Latitude + (MaximumCoordinates.Latitude - MinimumCoordinates.Latitude) / 2;
                result.Longitude = MinimumCoordinates.Longitude + (MaximumCoordinates.Longitude - MinimumCoordinates.Longitude) / 2;
                return result;
            }
        }

        public CartesianBounds TransformedWith<T>(ICoordinateTransform<T> transform)
        {
            return new CartesianBounds(transform.To(MinimumCoordinates), transform.To(MaximumCoordinates));
        }

    }
}
