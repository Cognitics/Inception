
using System.Collections.Generic;

namespace Cognitics.GeoPackage
{
    public class FeatureLayer : Layer
    {

        public IEnumerable<Feature> Features()
        {
            // *** WARNING *** : table name cannot be parameterized ; this is vulnerable to sql injection
            var geometryColumn = GeometryColumn();
            using (var statement = Database.Connection.Execute("SELECT * FROM " + TableName))
            {
                int geometryColumnIndex = (geometryColumn == null) ? -1 : statement.Ordinal(geometryColumn.ColumnName);
                while(statement.Next())
                    yield return ReadFeature(statement, geometryColumnIndex);
            }
        }

        public IEnumerable<Feature> Features(double minX, double maxX, double minY, double maxY)
        {
            // *** WARNING *** : table name cannot be parameterized ; this is vulnerable to sql injection
            var geometryColumn = GeometryColumn();
            string query = "SELECT * FROM " + TableName + " WHERE fid IN (SELECT id FROM rtree_" + TableName + "_geom WHERE ";
            query += "(minx <= @max_x) AND (maxx >= @min_x) AND ";
            query += "(miny <= @max_y) AND (maxy >= @min_y))";
            using (var statement = Database.Connection.Prepare(query))
            {
                statement.AddParameter("@min_x", minX);
                statement.AddParameter("@max_x", maxX);
                statement.AddParameter("@min_y", minY);
                statement.AddParameter("@max_y", maxY);
                statement.Execute();
                int geometryColumnIndex = (geometryColumn == null) ? -1 : statement.Ordinal(geometryColumn.ColumnName);
                while (statement.Next())
                    yield return ReadFeature(statement, geometryColumnIndex);
            }
        }

        #region implementation

        internal FeatureLayer(Database database) : base(database)
        {
        }

        public GeometryColumn GeometryColumn()
        {
            using (var statement = Database.Connection.Prepare("SELECT * FROM gpkg_geometry_columns WHERE table_name=@table_name"))
            {
                statement.AddParameter("@table_name", TableName);
                statement.Execute();
                while (statement.Next())
                {
                    var result = new GeometryColumn
                    {
                        TableName = statement.Value("table_name", ""),
                        ColumnName = statement.Value("column_name", ""),
                        GeometryTypeName = statement.Value("geometry_type_name", ""),
                        SpatialReferenceSystemID = statement.Value("srs_id", (long)0),
                        m = statement.Value("m", (byte)0),
                        z = statement.Value("z", (byte)0)
                    };
                    return result;
                }
            }
            return null;
        }

        private Feature ReadFeature(DBI.Statement statement, int geometryColumnIndex)
        {
            var feature = new Feature();
            for (int i = 0; i < statement.FieldCount; ++i)
            {
                if (i == geometryColumnIndex)
                {
                    feature.Geometry = BinaryGeometry.Read(statement.Stream(i));
                    if (feature.Geometry == null)
                        continue;
                    /*
                    if (TransformFrom == null)
                        continue;
                    feature.Geometry.Transform(TransformFrom);
                    */
                    continue;
                }
                feature.Attributes[statement.Key(i)] = statement.Value(i);
            }
            return feature;
        }

        #endregion

    }
}
