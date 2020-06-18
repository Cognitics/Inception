
using System;
using Cognitics.CoordinateSystems;
using BitMiracle.LibTiff.Classic;

namespace Cognitics.CDB
{
    public struct Tile
    {
        public static bool operator ==(Tile a, Tile b)
        {
            return (a.Bounds == b.Bounds) && (a.LOD == b.LOD) && (a.uref == b.uref) && (a.rref == b.rref);
        }

        public static bool operator !=(Tile a, Tile b) => !(a == b);

        ////////////////////////////////////////////////////////////

        public override int GetHashCode() => System.Tuple.Create(Bounds, LOD, uref, rref).GetHashCode();
        public override bool Equals(object obj) => (obj is Tile) && (this == (Tile)obj);

        public GeographicBounds Bounds;
        public LOD LOD;
        public uint uref;
        public uint rref;

        public string Name => string.Format("{0}_{1}_U{2}_R{3}", Bounds.MinimumCoordinates.TileFilename, LOD.Filename, uref, rref);
        public int RasterDimension => LOD.RasterDimension;
        public int MeshDimension => LOD.RasterDimension + 1;



        public string Path(TiledDataset dataset)
        {
            string[] parts = {
                "Tiles",
                Bounds.MinimumCoordinates.TileLatitudeSubdirectory,
                Bounds.MinimumCoordinates.TileLongitudeSubdirectory,
                dataset.Subdirectory,
                LOD.Subdirectory,
                string.Format("U{0}", uref),
                };
            return string.Join(System.IO.Path.DirectorySeparatorChar.ToString(), parts);
        }

        public string Filename(Component component)
        {
            string[] parts = {
                Bounds.MinimumCoordinates.TileFilename,
                ((TiledDataset)component.Dataset).Filename,
                string.Format("S{0:000}", component.Selector1),
                string.Format("T{0:000}", component.Selector2),
                LOD.Filename,
                string.Format("U{0}", uref),
                string.Format("R{0}", rref),
                };
            return string.Join("_", parts);
        }



    }
}
