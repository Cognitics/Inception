
namespace Cognitics.CoordinateSystems
{
    public interface ICoordinateTransform<T>
    {
        GeographicCoordinates From(CartesianCoordinates cartesianCoordinates);
        CartesianCoordinates To(GeographicCoordinates geographicCoordinates);
    }
}
