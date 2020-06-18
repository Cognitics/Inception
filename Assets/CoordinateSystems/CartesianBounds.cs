
namespace Cognitics.CoordinateSystems
{
    public struct CartesianBounds : System.IEquatable<CartesianBounds>
    {
        public static bool operator ==(CartesianBounds a, CartesianBounds b)
        {
            return (a.MinimumCoordinates == b.MinimumCoordinates) && (a.MaximumCoordinates == b.MaximumCoordinates);
        }
        public static bool operator !=(CartesianBounds a, CartesianBounds b) => !(a == b);

        ////////////////////////////////////////////////////////////

        public override int GetHashCode() => System.Tuple.Create(MinimumCoordinates, MaximumCoordinates).GetHashCode();
        public override bool Equals(object obj) => (obj is CartesianBounds) && (this == (CartesianBounds)obj);
        public bool Equals(CartesianBounds obj) => this == obj;

        public CartesianCoordinates MinimumCoordinates;
        public CartesianCoordinates MaximumCoordinates;

        public CartesianBounds(CartesianCoordinates minCoordinates, CartesianCoordinates maxCoordinates)
        {
            MinimumCoordinates = minCoordinates;
            MaximumCoordinates = maxCoordinates;
        }

        public string String => string.Format("({0},{1})", MinimumCoordinates.String, MaximumCoordinates.String);

        public CartesianCoordinates Center
        {
            get
            {
                CartesianCoordinates result;
                result.X = MinimumCoordinates.X + (MaximumCoordinates.X - MinimumCoordinates.X) / 2;
                result.Y = MinimumCoordinates.Y + (MaximumCoordinates.Y - MinimumCoordinates.Y) / 2;
                return result;
            }
        }

        public GeographicBounds TransformedWith<T>(ICoordinateTransform<T> transform)
        {
            return new GeographicBounds(transform.From(MinimumCoordinates), transform.From(MaximumCoordinates));
        }

    }
}
