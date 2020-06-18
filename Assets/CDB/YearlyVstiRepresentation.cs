
using System;
using BitMiracle.LibTiff.Classic;

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

        public string AlternateFilename(Tile tile) => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Filename(tile)), System.IO.Path.GetFileNameWithoutExtension(Filename(tile)) + ".tif");

        public bool AlternateExists(Tile tile) => System.IO.File.Exists(AlternateFilename(tile));

        public byte[] AlternateRead(Tile tile)
        {
            string filename = AlternateFilename(tile);
            string logname = "YearlyVstiRepresentation.Read(" + filename + ")";

            var tiff = Tiff.Open(filename, "r");
            if (tiff == null)
            {
                Console.WriteLine(logname + ": Tiff.Open() failed");
                return null;
            }
            {
                FieldValue[] value = tiff.GetField(TiffTag.IMAGEWIDTH);
                int width = value[0].ToInt();
                if (width != tile.RasterDimension)
                {
                    Console.WriteLine(logname + ": invalid width (" + width.ToString() + "); expected " + tile.RasterDimension.ToString());
                    return null;
                }
            }
            {
                FieldValue[] value = tiff.GetField(TiffTag.IMAGELENGTH);
                int height = value[0].ToInt();
                if (height != tile.RasterDimension)
                {
                    Console.WriteLine(logname + ": invalid height (" + height.ToString() + "); expected " + tile.RasterDimension.ToString());
                    return null;
                }
            }
            {
                int stride = tiff.ScanlineSize();
                byte[] buffer = new byte[stride];

                var result = new byte[tile.RasterDimension * tile.RasterDimension * 3];

                for (int row = 0; row < tile.RasterDimension; ++row)
                {
                    if (!tiff.ReadScanline(buffer, row))
                    {
                        Console.WriteLine(logname + ": Tiff.ReadScanLine(buffer, " + row.ToString() + ") failed");
                        break;
                    }
                    for (int col = 0; col < tile.RasterDimension * 3; ++col)
                        result[((tile.RasterDimension - row - 1) * tile.RasterDimension * 3) + col] = buffer[col];
                }

                return result;
            }
        }
    }

}


