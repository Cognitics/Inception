
using System;
using BitMiracle.LibTiff.Classic;

namespace Cognitics.CDB
{
    public class PrimaryTerrainElevation : Component
    {
        public override int Selector1 => 1;
        public override int Selector2 => 1;
        public override string Name => "Primary Terrain Elevation";
        public override string Extension => ".tif";

        internal PrimaryTerrainElevation(Dataset dataset) : base(dataset) { }

        public float[] Read(Tile tile)
        {
            // TODO: read mesh type from channel 1

            string filename = Filename(tile);
            string logname = "PrimaryTerrainElevation.Read(" + filename + ")";

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
                FieldValue[] bitDepth = tiff.GetField(TiffTag.BITSPERSAMPLE);
                FieldValue[] dataTypeTag = tiff.GetField(TiffTag.SAMPLEFORMAT);
                int bpp = bitDepth[0].ToInt();
                int dataType = dataTypeTag[0].ToInt();


                int stride = tiff.ScanlineSize();
                byte[] buffer = new byte[stride];

                var result = new float[tile.RasterDimension * tile.RasterDimension];

                for (int row = 0; row < tile.RasterDimension; ++row)
                {
                    if (!tiff.ReadScanline(buffer, row))
                    {
                        Console.WriteLine(logname + ": Tiff.ReadScanLine(buffer, " + row.ToString() + ") failed");
                        break;
                    }

                    // Case of float
                    if (bpp == 32 && dataType == 3)
                        for (int col = 0; col < tile.RasterDimension; ++col)
                            result[(row * tile.RasterDimension) + col] = BitConverter.ToSingle(buffer, col * 4);

                    // case of Int32
                    else if (bpp == 32 && dataType == 2)
                        for (int col = 0; col < tile.RasterDimension; ++col)
                            result[(row * tile.RasterDimension) + col] = BitConverter.ToInt32(buffer, col * 4);

                    // Case of Int16
                    else if (bpp == 16 && dataType == 2)
                        for (int col = 0; col < tile.RasterDimension; ++col)
                            result[(row * tile.RasterDimension) + col] = BitConverter.ToInt16(buffer, col * 2);

                    // Case of Int8
                    else if (bpp == 8 && dataType == 2)
                        for (int col = 0; col < tile.RasterDimension; ++col)
                            result[(row * tile.RasterDimension) + col] = buffer[col];

                    // Case of Unknown Datatype
                    else
                    {
                        Console.WriteLine(
                            logname +
                            ": Unknown Tiff file format " +
                            "(bits per pixel:" + bpp.ToString() +
                            ",  dataType code: " + dataType.ToString() +
                            "). Expected bpp values: 8, 16, or 32. Expected dataType values: 1 (two's complement signed int), or 3 (IEEE float)."
                            );
                    }
                }

                return result;
            }
        }
    }
}

