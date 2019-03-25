
namespace Cognitics.CoordinateSystems
{
    public struct ScaledFlatEarthProjection : ICoordinateTransform<double>
    {
        FlatEarthProjection Projection;
        public readonly double Scale;


        public ScaledFlatEarthProjection(GeographicCoordinates origin, double scale)
        {
            Projection = new FlatEarthProjection(origin);
            Scale = scale;
        }

        public GeographicCoordinates From(CartesianCoordinates cartesianCoordinates)
        {
            cartesianCoordinates.X /= Scale;
            cartesianCoordinates.Y /= Scale;
            return Projection.From(cartesianCoordinates);
        }

        public CartesianCoordinates To(GeographicCoordinates geographicCoordinates)
        {
            var cartesianCoordinates = Projection.To(geographicCoordinates);
            cartesianCoordinates.X *= Scale;
            cartesianCoordinates.Y *= Scale;
            return cartesianCoordinates;
        }


    }
}
