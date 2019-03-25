
using System;

namespace Cognitics.CDB
{
    public class YearlyVstiRepresentation : Component
    {
        public override int Selector1 => 1;
        public override int Selector2 => 1;
        public override string Name => "Yearly VSTI Representation";
        public override string Extension => ".jp2";

        internal YearlyVstiRepresentation(Dataset dataset) : base(dataset) { }

        public byte[] Read(Tile tile)
        {
            string filename = Filename(tile);
            string logname = "YearlyVstiRepresentation.Read(" + filename + ")";

            CSJ2K.Util.PortableImage img = CSJ2K.J2kImage.FromFile(filename);

            int[] ib = img.GetComponent(0);
            int[] ig = img.GetComponent(1);
            int[] ir = img.GetComponent(2);

            int dim = (int)Math.Sqrt(ib.Length);

            var result = new byte[tile.RasterDimension * tile.RasterDimension * 3];

            for (int y = 0; y < dim; ++y)
            {
                for (int x = 0; x < dim; ++x)
                {
                    int i = (y * dim) + x;
                    result[(i * 3) + 0] = (byte)ir[i];
                    result[(i * 3) + 1] = (byte)ig[i];
                    result[(i * 3) + 2] = (byte)ib[i];
                }
            }

            return result;
        }

    }

}


