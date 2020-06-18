using System.Collections.Generic;

namespace Cognitics.GeoPackage
{
    enum WKBGeometryType : uint
    {
        wkbPoint = 1,
        wkbLineString = 2,
        wkbPolygon = 3,
        wkbTriangle = 17,
        wkbMultiPoint = 4,
        wkbMultiLineString = 5,
        wkbMultiPolygon = 6,
        wkbGeometryCollection = 7,
        wkbPolyhedralSurface = 15,
        wkbTIN = 16,
        wkbPointZ = 1001,
        wkbLineStringZ = 1002,
        wkbPolygonZ = 1003,
        wkbTriangleZ = 1017,
        wkbMultiPointZ = 1004,
        wkbMultiLineStringZ = 1005,
        wkbMultiPolygonZ = 1006,
        wkbGeometryCollectionZ = 1007,
        wkbPolyhedralSurfaceZ = 1015,
        wkbTINZ = 1016,
        wkbPointM = 2001,
        wkbLineStringM = 2002,
        wkbPolygonM = 2003,
        wkbTriangleM = 2017,
        wkbMultiPointM = 2004,
        wkbMultiLineStringM = 2005,
        wkbMultiPolygonM = 2006,
        wkbGeometryCollectionM = 2007,
        wkbPolyhedralSurfaceM = 2015,
        wkbTINM = 2016,
        wkbPointZM = 3001,
        wkbLineStringZM = 3002,
        wkbPolygonZM = 3003,
        wkbTriangleZM = 3017,
        wkbMultiPointZM = 3004,
        wkbMultiLineStringZM = 3005,
        wkbMultiPolygonZM = 3006,
        wkbGeometryCollectionZM = 3007,
        wkbPolyhedralSurfaceZM = 3015,
        wkbTinZM = 3016
    }

    public abstract class Geometry
    {
        //public abstract void Transform(ICoordinateTransformation transform);
    }

    public class Point : Geometry
    {
        public double X;
        public double Y;
        public double Z;
        public double M;

        /*
        public override void Transform(ICoordinateTransformation transform)
        {
            var xy1 = new double[2]{ X, Y };
            var xy2 = transform.MathTransform.Transform(xy1);
            X = xy2[0];
            Y = xy2[1];
        }
        */
    }

    public class MultiGeometry<T> : Geometry where T : Geometry
    {
        public List<T> Geometries = new List<T>();

        /*
        public override void Transform(ICoordinateTransformation transform)
        {
            Geometries.ForEach(geometry => geometry.Transform(transform));
        }
        */
    }

    public class MultiPoint : MultiGeometry<Point> { }
    public class MultiLineString : MultiGeometry<LineString> { }
    public class MultiPolygon : MultiGeometry<Polygon> { }
    public class GeometryCollection : MultiGeometry<Geometry> { }

    public class LineString : MultiPoint
    {
        public List<Point> Points => Geometries;
    }

    public class Polygon : MultiLineString
    {
        public List<LineString> Rings => Geometries;
    }



}
