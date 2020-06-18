using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;

namespace Cognitics.GeoPackage
{
    public class RasterLayer : Layer
    {
        public IEnumerable<TileMatrix> TileMatrices()
        {
            using (var statement = Database.Connection.Prepare("SELECT * FROM gpkg_tile_matrix WHERE table_name=@table_name"))
            {
                statement.AddParameter("@table_name", TableName);
                statement.Execute();
                while (statement.Next())
                    yield return ReadTileMatrix(statement);
            }
        }

        public IEnumerable<long> ZoomLevels()
        {
            foreach (var tileMatrix in TileMatrices())
                yield return tileMatrix.ZoomLevel;
        }

        public IEnumerable<Tile> Tiles()
        {
            // *** WARNING *** : table name cannot be parameterized ; this is vulnerable to sql injection
            using (var statement = Database.Connection.Execute("SELECT * FROM " + TableName))
            {
                while (statement.Next())
                    yield return ReadTile(statement);
            }
        }

        public IEnumerable<Tile> Tiles(double minX, double maxX, double minY, double maxY)
        {
            // *** WARNING *** : table name cannot be parameterized ; this is vulnerable to sql injection
            string query = "SELECT * FROM " + TableName + " WHERE ";
            query += "(minx <= @max_x) AND (maxx >= @min_x) AND ";
            query += "(miny <= @max_y) AND (maxy >= @min_y)";
            using (var statement = Database.Connection.Prepare(query))
            {
                statement.AddParameter("@min_x", minX);
                statement.AddParameter("@max_x", maxX);
                statement.AddParameter("@min_y", minY);
                statement.AddParameter("@max_y", maxY);
                statement.Execute();
                while (statement.Next())
                    yield return ReadTile(statement);
            }
        }

        public IEnumerable<Tile> Tiles(long zoomLevel)
        {
            // *** WARNING *** : table name cannot be parameterized ; this is vulnerable to sql injection
            using (var statement = Database.Connection.Prepare("SELECT * FROM " + TableName + " WHERE zoom_level=@zoom_level"))
            {
                statement.AddParameter("@zoom_level", zoomLevel);
                statement.Execute();
                while (statement.Next())
                    yield return ReadTile(statement);
            }
        }

        public IEnumerable<Tile> Tiles(long zoomLevel, double minX, double maxX, double minY, double maxY)
        {
            // *** WARNING *** : table name cannot be parameterized ; this is vulnerable to sql injection
            string query = "SELECT * FROM " + TableName + " WHERE zoom_level=@zoom_level AND ";
            query += "(minx <= @max_x) AND (maxx >= @min_x) AND ";
            query += "(miny <= @max_y) AND (maxy >= @min_y)";
            using (var statement = Database.Connection.Execute(query))
            {
                statement.AddParameter("@zoom_level", zoomLevel);
                statement.AddParameter("@min_x", minX);
                statement.AddParameter("@max_x", maxX);
                statement.AddParameter("@min_y", minY);
                statement.AddParameter("@max_y", maxY);
                statement.Execute();
                while (statement.Next())
                    yield return ReadTile(statement);
            }
        }

        public Tile Tile(long zoomLevel, long row, long column)
        {
            // *** WARNING *** : table name cannot be parameterized ; this is vulnerable to sql injection
            using (var statement = Database.Connection.Prepare("SELECT * FROM " + TableName + " WHERE zoom_level=@zoom_level AND tile_row=@tile_row AND tile_column=@tile_column"))
            {
                statement.AddParameter("@zoom_level", zoomLevel);
                statement.AddParameter("@tile_row", row);
                statement.AddParameter("@tile_column", column);
                statement.Execute();
                while (statement.Next())
                    return ReadTile(statement);
            }
            return null;
        }


        #region implementation


        internal RasterLayer(Database database) : base(database)
        {
            UpdateRasterExtentsFromTileMatrixSet();
        }

        /// <summary>
        /// Requirement 18 of the GeoPackage standard requires the extents in gpkg_tile_matrix_set
        /// to be exact, whereas the extents in gpkg_contents are informational only.
        /// This method corrects the extents by reading gpkg_tile_matrix_set for raster layers.
        /// </summary>
        private void UpdateRasterExtentsFromTileMatrixSet()
        {
            using (var statement = Database.Connection.Prepare("SELECT * FROM gpkg_tile_matrix_set WHERE table_name=@table_name"))
            {
                statement.AddParameter("@table_name", TableName);
                statement.Execute();
                if (statement.Next())
                {
                    MinX = statement.Value("min_x", double.MinValue);
                    MinY = statement.Value("min_y", double.MinValue);
                    MaxX = statement.Value("max_x", double.MaxValue);
                    MaxY = statement.Value("max_y", double.MaxValue);
                }
            }
        }

        private TileMatrix ReadTileMatrix(DBI.Statement statement)
        {
            return new TileMatrix
            {
                TableName = statement.Value("table_name", ""),
                ZoomLevel = statement.Value("zoom_level", (long)0),
                TilesWide = statement.Value("matrix_width", (long)0),
                TilesHigh = statement.Value("matrix_height", (long)0),
                TileWidth = statement.Value("tile_width", (long)0),
                TileHeight = statement.Value("tile_height", (long)0),
                PixelXSize = statement.Value("pixel_x_size", 0.0),
                PixelYSize = statement.Value("pixel_y_size", 0.0),
            };
        }

        private Tile ReadTile(DBI.Statement statement)
        {
            return new Tile
            {
                ID = statement.Value("id", (long)0),
                ZoomLevel = statement.Value("zoom_level", (long)0),
                TileColumn = statement.Value("tile_column", (long)0),
                TileRow = statement.Value("tile_row", (long)0),
                Bytes = statement.Value("tile_data", (byte[])null),
            };
        }

        #endregion

    }
}
