
namespace Cognitics.CoordinateSystems
{
    public struct CartesianCoordinates : System.IEquatable<CartesianCoordinates>
    {
        static public bool operator ==(CartesianCoordinates a, CartesianCoordinates b)
        {
            return (a.X == b.X) && (a.Y == b.Y);
        }
        static public bool operator !=(CartesianCoordinates a, CartesianCoordinates b) => !(a == b);

        ////////////////////////////////////////////////////////////

        public override int GetHashCode() => System.Tuple.Create(X, Y).GetHashCode();
        public override bool Equals(object obj) => (obj is CartesianCoordinates) && (this == (CartesianCoordinates)obj);
        public bool Equals(CartesianCoordinates obj) => this == obj;


        public double X;
        public double Y;

        public CartesianCoordinates(double x, double y)
        {
            X = x;
            Y = y;
        }

        public string String => string.Format("({0},{1})", X, Y);

        public GeographicCoordinates TransformedWith<T>(ICoordinateTransform<T> transform) => transform.From(this);

    }
}
