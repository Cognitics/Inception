
using System;
using System.Collections.Generic;

namespace Cognitics.GeoPackage
{
    public class Database : IDisposable
    {
        public readonly DBI.Connection Connection;
        public SpatialReferenceSystem ApplicationSpatialReferenceSystem;

        public Database(string filename)
        {
            Connection = new DBI.Connection(filename);
        }

        public SpatialReferenceSystem SpatialReferenceSystem(long id)
        {
            using (var statement = Connection.Prepare("SELECT * FROM gpkg_spatial_ref_sys WHERE srs_id=@srs_id"))
            {
                statement.AddParameter("@srs_id", id);
                statement.Execute();
                while(statement.Next())
                    return ReadSpatialReferenceSystem(statement);
            }
            return null;
        }

        public IEnumerable<SpatialReferenceSystem> SpatialReferenceSystems()
        {
            using (var statement = Connection.Execute("SELECT * FROM gpkg_spatial_ref_sys"))
            {
                while(statement.Next())
                    yield return ReadSpatialReferenceSystem(statement);
            }
        }

        public Layer Layer(string name)
        {
            using (var statement = Connection.Prepare("SELECT * FROM gpkg_contents WHERE table_name=@table_name"))
            {
                statement.AddParameter("@table_name", name);
                statement.Execute();
                while (statement.Next())
                    return ReadLayer(statement);
            }
            return null;
        }

        public IEnumerable<Layer> Layers()
        {
            using (var statement = Connection.Execute("SELECT * FROM gpkg_contents"))
            {
                while(statement.Next())
                    yield return ReadLayer(statement);
            }
        }

        public IEnumerable<Layer> Layers(string dataType)
        {
            using (var statement = Connection.Prepare("SELECT * FROM gpkg_contents WHERE data_type=@data_type"))
            {
                statement.AddParameter("@data_type", dataType);
                statement.Execute();
                while (statement.Next())
                    yield return ReadLayer(statement);
            }
        }

        public IEnumerable<Layer> Layers(double minX, double maxX, double minY, double maxY)
        {
            string query = "SELECT * FROM gpkg_contents WHERE ";
            query += "(min_x <= @max_x) AND (max_x >= @min_x) AND ";
            query += "(min_y <= @max_y) AND (max_y >= @min_y)";
            using (var statement = Connection.Prepare(query))
            {
                statement.AddParameter("@min_x", minX);
                statement.AddParameter("@max_x", maxX);
                statement.AddParameter("@min_y", minY);
                statement.AddParameter("@max_y", maxY);
                statement.Execute();
                while (statement.Next())
                    yield return ReadLayer(statement);
            }
        }

        public IEnumerable<Layer> Layers(string dataType, double minX, double maxX, double minY, double maxY)
        {
            string query = "SELECT * FROM gpkg_contents WHERE data_type=@data_type AND ";
            query += "(min_x <= @max_x) AND (max_x >= @min_x) AND ";
            query += "(min_y <= @max_y) AND (max_y >= @min_y)";
            using (var statement = Connection.Prepare(query))
            {
                statement.AddParameter("@data_type", dataType);
                statement.AddParameter("@min_x", minX);
                statement.AddParameter("@max_x", maxX);
                statement.AddParameter("@min_y", minY);
                statement.AddParameter("@max_y", maxY);
                statement.Execute();
                while (statement.Next())
                    yield return ReadLayer(statement);
            }
        }

        public IEnumerable<Layer> FeatureLayers() => Layers("features");
        public IEnumerable<Layer> FeatureLayers(double minX, double maxX, double minY, double maxY)
            => Layers("features", minX, maxX, minY, maxY);
        public IEnumerable<Layer> RasterLayers() => Layers("tiles");
        public IEnumerable<Layer> RasterLayers(double minX, double maxX, double minY, double maxY)
            => Layers("tiles", minX, maxX, minY, maxY);


        #region implementation

        private SpatialReferenceSystem ReadSpatialReferenceSystem(DBI.Statement statement)
        {
            var result = new SpatialReferenceSystem
            {
                ID = statement.Value("srs_id", (long)0),
                Name = statement.Value("srs_name", ""),
                Organization = statement.Value("organization", ""),
                OrganizationCoordinateSystemID = statement.Value("organization_coordsys_id", (long)0),
                Definition = statement.Value("definition", ""),
                Description = statement.Value("description", "")
            };
            return result;
        }

        private Layer ReadLayer(DBI.Statement statement)
        {
            string layerType = statement.Value("data_type", "");
            Layer layer = null;
            if (layerType == "features")
                layer = new FeatureLayer(this);
            if (layerType == "tiles")
                layer = new RasterLayer(this);
            if (layerType == "2d-gridded-coverage")
                layer = new RasterLayer(this);
            if (layer == null)
                layer = new Layer(this);
            layer.TableName = statement.Value("table_name", "");
            layer.DataType = statement.Value("data_type", "");
            layer.Identifier = statement.Value("identifier", "");
            layer.Description = statement.Value("description", "");
            layer.LastChange = statement.Value("last_change", DateTime.MinValue);
            layer.MinX = statement.Value("min_x", double.MaxValue);
            layer.MinY = statement.Value("min_y", double.MaxValue);
            layer.MaxX = statement.Value("max_x", double.MinValue);
            layer.MaxY = statement.Value("max_y", double.MinValue);
            layer.SpatialReferenceSystemID = statement.Value("srs_id", (long)0);
            /*
            TODO: transformation handling for all queries and layers
                gpkg_contents has srs (for min/max)
                gpkg_geometry_columns has srs (for geometry)
                gpkg_tile_matrix_set has srs (for zoomlevel bounds and matrix pixel size)


            if (ApplicationSpatialReferenceSystem != null)
            {
                var layerSpatialReferenceSystem = layer.SpatialReferenceSystem;
                if (layerSpatialReferenceSystem != null)
                {
                    if (layerSpatialReferenceSystem.ID == 0)
                        layerSpatialReferenceSystem = SpatialReferenceSystem(4326);
                    if (layerSpatialReferenceSystem.Definition != ApplicationSpatialReferenceSystem.Definition)
                    {
                        try
                        {
                            var layerSRS = GeoPackage.SpatialReferenceSystem.ProjNetCoordinateSystem(layerSpatialReferenceSystem.Definition);
                            var appSRS = GeoPackage.SpatialReferenceSystem.ProjNetCoordinateSystem(ApplicationSpatialReferenceSystem.Definition);
                            layer.TransformFrom = GeoPackage.SpatialReferenceSystem.ProjNetTransform(layerSRS, appSRS);
                            layer.TransformTo = GeoPackage.SpatialReferenceSystem.ProjNetTransform(appSRS, layerSRS);
                        }
                        catch (ArgumentException) { }
                    }
                }
                if (layer.TransformFrom != null)
                {
                    {
                        var xy1 = new double[2] { layer.MinX, layer.MinY };
                        var xy2 = layer.TransformFrom.MathTransform.Transform(xy1);
                        layer.MinX = xy2[0];
                        layer.MinY = xy2[1];
                    }
                    {
                        var xy1 = new double[2] { layer.MaxX, layer.MaxY };
                        var xy2 = layer.TransformFrom.MathTransform.Transform(xy1);
                        layer.MaxX = xy2[0];
                        layer.MaxY = xy2[1];
                    }
                }
            }
            */
            return layer;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Connection.Dispose();
                }

                disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

        #endregion


    }
}
