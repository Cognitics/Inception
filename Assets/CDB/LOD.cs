using System;
using System.Collections.Generic;
using Cognitics.CoordinateSystems;

namespace Cognitics.CDB
{
    public struct LOD
    {
        static public int MinValueInt => -10;
        static public int MaxValueInt => 23;
        static public LOD MinValue => MinValueInt;
        static public LOD MaxValue => MaxValueInt;
        static public implicit operator LOD(int value) => new LOD(value);
        static public implicit operator int(LOD lod) => lod.value;

        ////////////////////////////////////////////////////////////

        private int value;

        public LOD(int value)
        {
            this.value = Math.Min(Math.Max(value, MinValueInt), MaxValueInt);
        }

        public int Rows => Math.Max(1, (int)Math.Pow(2, value));
        public int Columns => Math.Max(1, (int)Math.Pow(2, value));
        public int RasterDimension => (int)Math.Pow(2, Math.Min(value + 10, 10));
        public int MaximumPoints => (int)Math.Pow(4, Math.Min(Math.Max(value + 7, 0), 7));
        public string Filename => string.Format("{0}{1:00}", ((value < 0) ? "LC" : "L"), Math.Abs(value));
        public string Subdirectory => (value < 0) ? "LC" : Filename;

        public List<Tile> GenerateTiles(GeographicBounds geographicBounds)
        {
            var result = new List<Tile>();
            int isouth = geographicBounds.MinimumCoordinates.TileLatitude;
            int iwest = geographicBounds.MinimumCoordinates.TileLongitude;
            int inorth = geographicBounds.MaximumCoordinates.TileLatitude;
            int ieast = geographicBounds.MaximumCoordinates.TileLongitude;
            int rows = Rows;
            int cols = Columns;
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
                        if (tile_south >= geographicBounds.MaximumCoordinates.Latitude)
                            continue;
                        double tile_north = ilat + (rowHeight * (uref + 1));
                        if (tile_north <= geographicBounds.MinimumCoordinates.Latitude)
                            continue;
                        for (uint rref = 0; rref < cols; ++rref)
                        {
                            double tile_west = ilon + (col_width * rref);
                            if (tile_west >= geographicBounds.MaximumCoordinates.Longitude)
                                continue;
                            double tile_east = ilon + (col_width * (rref + 1));
                            if (tile_east <= geographicBounds.MinimumCoordinates.Longitude)
                                continue;
                            var tile = new Tile()
                            {
                                Bounds = new GeographicBounds(
                                    new GeographicCoordinates(tile_south, tile_west),
                                    new GeographicCoordinates(tile_north, tile_east)
                                    ),
                                LOD = this,
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

    }
}
