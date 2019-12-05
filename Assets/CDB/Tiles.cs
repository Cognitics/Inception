
using System;
using System.Collections.Generic;
using Cognitics.CoordinateSystems;

namespace Cognitics.CDB
{
    public class Tiles
    {
        public readonly Database Database;

        internal Tiles(Database Database)
        {
            this.Database = Database;
        }

        public List<GeographicCoordinates> ExistingGeocells()
        {
            var result = new List<GeographicCoordinates>();
            for (int ilat = -90; ilat < 90; ++ilat)
            {
                GeographicCoordinates tileCoordinates = new GeographicCoordinates(ilat, 0.0f);
                string latitudeSubdirectory = System.IO.Path.Combine(Database.Path, "Tiles", tileCoordinates.TileLatitudeSubdirectory);
                if (!System.IO.Directory.Exists(latitudeSubdirectory))
                    continue;
                int tileWidth = tileCoordinates.Latitude.TileWidth;
                for (int ilon = -180; ilon < 180; ilon += tileWidth)
                {
                    tileCoordinates.Longitude = ilon;
                    string longitudeDirectory = System.IO.Path.Combine(Database.Path, "Tiles", tileCoordinates.TileSubdirectory);
                    if (System.IO.Directory.Exists(longitudeDirectory))
                        result.Add(tileCoordinates);
                }
            }
            return result;
        }

        public GeographicBounds ExistingBounds()
        {
            int inorth = (int)Latitude.MinValue;
            int isouth = (int)Latitude.MaxValue;
            int ieast = (int)Longitude.MinValue;
            int iwest = (int)Longitude.MaxValue;
            for (int ilat = -90; ilat < 90; ++ilat)
            {
                GeographicCoordinates tileCoordinates = new GeographicCoordinates(ilat, 0.0f);
                string latitudeDirectory = System.IO.Path.Combine(Database.Path, "Tiles", tileCoordinates.TileLatitudeSubdirectory);
                if (!System.IO.Directory.Exists(latitudeDirectory))
                    continue;
                isouth = Math.Min(isouth, ilat);
                inorth = Math.Max(inorth, ilat + 1);
                int tileWidth = tileCoordinates.Latitude.TileWidth;
                for (int ilon = -180; ilon < 180; ilon += tileWidth)
                {
                    tileCoordinates.Longitude = ilon;
                    if ((ilon >= iwest) && (ilon < ieast))
                    {
                        ilon = ieast - 1;
                        continue;
                    }
                    string longitudeDirectory = System.IO.Path.Combine(latitudeDirectory, tileCoordinates.TileLongitudeString);
                    if (!System.IO.Directory.Exists(longitudeDirectory))
                        continue;
                    iwest = Math.Min(iwest, ilon);
                    ieast = Math.Max(ieast, ilon + tileWidth);
                }
            }
            var southWest = new GeographicCoordinates(isouth, iwest);
            var northEast = new GeographicCoordinates(inorth, ieast);
            return new GeographicBounds(southWest, northEast);
        }

        public List<Tile> Generate(GeographicBounds bounds, LOD lod)
        {
            var result = new List<Tile>();
            int isouth = bounds.MinimumCoordinates.TileLatitude;
            int iwest = bounds.MinimumCoordinates.TileLongitude;
            int inorth = bounds.MaximumCoordinates.TileLatitude;
            int ieast = bounds.MaximumCoordinates.TileLongitude;
            int rows = lod.Rows;
            int cols = lod.Columns;
            double rowHeight = 1.0f / rows;
            for (int ilat = isouth; ilat <= inorth; ++ilat)
            {
                Latitude latitude = ilat;
                int ilon_step = latitude.TileWidth;
                double col_width = (double)ilon_step / cols;
                for (int ilon = iwest; ilon <= ieast; ilon += ilon_step)
                {
                    for (uint uref = 0; uref < rows; ++uref)
                    {
                        double tile_south = ilat + (rowHeight * uref);
                        if (tile_south >= bounds.MaximumCoordinates.Latitude)
                            continue;
                        double tile_north = ilat + (rowHeight * (uref + 1));
                        if (tile_north <= bounds.MinimumCoordinates.Latitude)
                            continue;
                        for (uint rref = 0; rref < cols; ++rref)
                        {
                            double tile_west = ilon + (col_width * rref);
                            if (tile_west >= bounds.MaximumCoordinates.Longitude)
                                continue;
                            double tile_east = ilon + (col_width * (rref + 1));
                            if (tile_east <= bounds.MinimumCoordinates.Longitude)
                                continue;
                            var tile = new Tile()
                            {
                                Bounds = new GeographicBounds(
                                    new GeographicCoordinates(tile_south, tile_west),
                                    new GeographicCoordinates(tile_north, tile_east)
                                    ),
                                LOD = lod,
                                uref = uref,
                                rref = rref
                            };
                            result.Add(tile);
                        }
                    }
                }
            }
            return result;
        }

        public byte[] GeocellInventory()
        {
            var result = new byte[180 * 360];
            string path_tiles = System.IO.Path.Combine(Database.Path, "Tiles");
            foreach (var dir_latitude in System.IO.Directory.EnumerateDirectories(path_tiles))
            {
                string subdir_latitude = System.IO.Path.GetFileName(dir_latitude);
                char ns = subdir_latitude[0];
                if (!int.TryParse(subdir_latitude.Substring(1), out int ilat))
                    continue;
                int y = (ns == 'N') ? 90 + ilat : 90 - ilat;
                string path_latitude = System.IO.Path.Combine(path_tiles, subdir_latitude);
                foreach (var dir_longitude in System.IO.Directory.EnumerateDirectories(path_latitude))
                {
                    string subdir_longitude = System.IO.Path.GetFileName(dir_longitude);
                    char we = subdir_longitude[0];
                    if (!int.TryParse(subdir_longitude.Substring(1), out int ilon))
                        continue;
                    int x = (we == 'E') ? 180 + ilon : 180 - ilon;
                    result[(y * 360) + x] = 1;
                }
            }
            return result;
        }

        public byte[] DatasetInventory(Tile area_tile, Component component, LOD max_lod)
        {
            int dim = (int)Math.Floor(Math.Pow(2, max_lod - area_tile.LOD));
            double row_spacing = 1.0 / dim;
            double col_spacing = (double)area_tile.Bounds.MinimumCoordinates.Latitude.TileWidth / dim;
            var result = new byte[dim * dim];
            for (int lod = area_tile.LOD; lod <= max_lod; ++lod)
            {
                var tiles = Generate(area_tile.Bounds, lod);
                foreach (var tile in tiles)
                {
                    if (!component.Exists(tile))
                        continue;
                    int row_start = (int)Math.Floor((tile.Bounds.MinimumCoordinates.Latitude - area_tile.Bounds.MinimumCoordinates.Latitude) / row_spacing);
                    int col_start = (int)Math.Floor((tile.Bounds.MinimumCoordinates.Longitude - area_tile.Bounds.MinimumCoordinates.Longitude) / col_spacing);
                    int local_dim = (int)Math.Floor(Math.Pow(2, max_lod - tile.LOD));
                    for (int row = row_start; row < row_start + local_dim; ++row)
                    {
                        for (int col = col_start; col < col_start + local_dim; ++col)
                            result[(row * dim) + col] = (byte)(lod - area_tile.LOD);
                    }
                }
            }
            return result;
        }


    }

}
