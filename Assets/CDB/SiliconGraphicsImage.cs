using System;
using System.IO;

namespace Cognitics.CDB
{
    public class SiliconGraphicsImage
    {
        private sbyte Storage;
        private sbyte BytesPerPixelComponent; // BPC
        private ushort Dimension;
        private ushort xSize, ySize, zSize;
        private int PixMin, PixMax;
        private string ImageName;
        private int ColorMapID;

        private long ImageLength;
        private float[] imageDataFloat;
        private byte[] imageDataRGB8;

        public float[] Read(string filename, out int width, out int height, out int numChannels)
        {
            width = 0;
            height = 0;
            numChannels = 0;

            byte[] bytes = File.ReadAllBytes(filename);

            Stream stream = new MemoryStream(bytes);

            BinaryParser SGIReader = new BinaryParser(bytes);

            /** Read Header **/
            short magic = SGIReader.Int16BE();    // 474

            if (magic != 474)
            {
                Console.WriteLine(string.Format("file {0} does not match SGI format specifications. The file type declared in the header is not SGI.", filename));
                return null;
            }

            Storage = (sbyte)SGIReader.Byte();                 // 0 or 1: 0 -> Verbatim, 1 -> Run Length Encoding
            BytesPerPixelComponent = (sbyte)SGIReader.Byte();  // 1 or 2: 1 -> 1 byte per component, 2 -> 2 bytes per component
            Dimension = SGIReader.UInt16BE();          // 1, 2, or 3 -> Number of Channels (Max 3, see ZSIZE)
            xSize = SGIReader.UInt16BE();              // Column Size
            ySize = SGIReader.UInt16BE();              // Row Size
            zSize = SGIReader.UInt16BE();              // Number of channels (can be any value)
            width = xSize;
            height = ySize;
            PixMin = SGIReader.Int32BE();              // Min pixel value (e.g. 0)
            PixMax = SGIReader.Int32BE();              // Max pixel value (e.g. 255)
            //var EmptySpace = new string(SGIReader.ReadChars(4));
            SGIReader.Position += 4;          //.ReadChars(4);
            //ImageName = SGIReader.String(80);
            SGIReader.Position += 80;
            ColorMapID = SGIReader.Int32BE();          // used for Run Length Encoding
            if (ColorMapID != 0)
            {
                Console.WriteLine(string.Format("unsupported color map id {0} in file {1}", ColorMapID, filename));
                return null;
            }

            //var Dummy = new string(SGIReader.ReadChars(404));
            SGIReader.Position += 404;

            if (Dimension == 1)
                numChannels = 1;
            else if (Dimension == 2)
                numChannels = 1;
            else if (Dimension == 3)
                numChannels = zSize;
            else
            {
                Console.WriteLine(string.Format("unknown number of channels in {0}", filename));
                return null;
            }

            imageDataFloat = new float[xSize * ySize * numChannels];

            // Re-structure array for RGB[A] sequence
            if (Storage == 0)
            {
                ImageLength = bytes.Length - 512; // SGI Headers are 512 bytes
                //ImageLength = new FileInfo(filename).Length - 512;

                for (int channel = 0; channel < numChannels; ++channel)         // 3 or 4 channels (RGB[A])
                {
                    for (int offset = channel; offset < ImageLength; offset += numChannels)
                    {
                        float fval = BytesPerPixelComponent == 1 ? SGIReader.Byte() : SGIReader.Int16BE();
                        imageDataFloat[offset] = fval;
                    }
                }
            }
            else // RLE
            {
                // TODO: leverage start offset and length tables if and when we have data that uses them
                // Table of start offsets
                int numScanlines = ySize;

                //uint[,] startTable = new uint[numChannels, numScanlines];
                for (int channel = 0; channel < numChannels; ++channel)
                {
                    for (int scanline = 0; scanline < numScanlines; ++scanline)
                    {
                        //startTable[channel, scanline] = SGIReader.ReadUInt32Big();
                        SGIReader.UInt32BE();
                    }
                }


                // Table of lengths
                //uint[,] lengthTable = new uint[numChannels, numScanlines];
                for (int channel = 0; channel < numChannels; ++channel)
                {
                    for (int scanline = 0; scanline < numScanlines; ++scanline)
                    {
                        //lengthTable[channel, scanline] = SGIReader.ReadUInt32Big();
                        SGIReader.UInt32BE();
                    }
                }
                // RLE data
                if (BytesPerPixelComponent == 1)
                {
                    byte[] byteArray = SGIReader.Bytes;
                    int inputIndex = SGIReader.Position;
                    int outputIndex = 0;

                    while (outputIndex < imageDataFloat.Length)
                    {
                        byte val = byteArray[inputIndex];
                        int count = val & 0x7f;
                        if (count == 0)
                        {
                            inputIndex++;
                            continue;
                        }

                        bool flag = (val & 0x80) != 0;
                        if (flag)
                        {
                            while ((count--) != 0)
                            {
                                byte nextVal = byteArray[inputIndex+1];
                                imageDataFloat[outputIndex] = nextVal;
                                inputIndex++;
                                outputIndex++;
                            }

                            inputIndex++;
                        }
                        else
                        {
                            byte nextVal = byteArray[inputIndex+1];
                            while ((count--) != 0)
                            {
                                imageDataFloat[outputIndex] = nextVal;
                                outputIndex++;
                            }

                            inputIndex += 2;
                        }
                    }

                    if (inputIndex != byteArray.Length - 1 || outputIndex != imageDataFloat.Length)
                        Console.WriteLine(string.Format("did not complete read of {0}", filename));

                    var tempBuffer = new float[xSize * ySize * numChannels];
                    Array.Copy(imageDataFloat, tempBuffer, imageDataFloat.Length);
                    //Convert to interleaved
                    int pixelLen = width * height;
                    for (int i = 0; i < pixelLen; i++)
                    {
                        for (int channel = 0; channel < numChannels; channel++)
                        {
                            imageDataFloat[(i * numChannels) + channel] = tempBuffer[(pixelLen * channel) + i];
                        }
                    }
                }
                else // TODO: handle shorts
                {
                    Console.WriteLine(string.Format("unsupported BytesPerPixelComponent: {0} in {1}", BytesPerPixelComponent, filename));
                    return null;
                }
            }

            return imageDataFloat;
        }

        public byte[] ReadRGB8(string filename, byte[] bytes, out int width, out int height, out int numChannels, bool downSample = false)
        {
            width = 0;
            height = 0;
            numChannels = 0;

            Stream stream = null;
            if (bytes == null)
            {
                try
                {
                    bytes = File.ReadAllBytes(filename);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return null;
                }
            }
            stream = new MemoryStream(bytes);

            BinaryParser SGIReader = new BinaryParser(bytes);

            /** Read Header **/
            short magic = SGIReader.Int16BE();    // 474


            if (magic != 474)
            {
                Console.WriteLine(string.Format("file {0} does not match SGI format specifications. The file type declared in the header is not SGI.", filename));
                return null;
            }

            Storage = (sbyte)SGIReader.Byte();                 // 0 or 1: 0 -> Verbatim, 1 -> Run Length Encoding
            BytesPerPixelComponent = (sbyte)SGIReader.Byte();  // 1 or 2: 1 -> 1 byte per component, 2 -> 2 bytes per component
            Dimension = SGIReader.UInt16BE();          // 1, 2, or 3 -> Number of Channels (Max 3, see ZSIZE)
            xSize = SGIReader.UInt16BE();              // Column Size
            ySize = SGIReader.UInt16BE();              // Row Size
            zSize = SGIReader.UInt16BE();              // Number of channels (can be any value)
            width = xSize;
            height = ySize;
            PixMin = SGIReader.Int32BE();              // Min pixel value (e.g. 0)
            PixMax = SGIReader.Int32BE();              // Max pixel value (e.g. 255)
            //var EmptySpace = new string(SGIReader.ReadChars(4));
            SGIReader.Position += 4;          //.ReadChars(4);
            //ImageName = OpenFlight.FltReader.GetString(SGIReader.ReadBytes(80));
            SGIReader.Position += 80;
            ColorMapID = SGIReader.Int32BE();          // used for Run Length Encoding
            if (ColorMapID != 0)
            {
                Console.WriteLine(string.Format("unsupported color map id {0} in file {1}", ColorMapID, filename));
                return null;
            }

            //var Dummy = new string(SGIReader.ReadChars(404));
            SGIReader.Position += 404;

            if (Dimension == 1)
                numChannels = 1;
            else if (Dimension == 2)
                numChannels = 1;
            else if (Dimension == 3)
                numChannels = zSize;
            else
            {
                Console.WriteLine(string.Format("unknown number of channels in {0}", filename));
                return null;
            }

            imageDataRGB8 = new byte[xSize * ySize * numChannels];

            // Re-structure array for RGB[A] sequence
            if (Storage == 0)
            {
                ImageLength = bytes.Length - 512; // SGI Headers are 512 bytes
                //ImageLength = new FileInfo(filename).Length - 512;

                bool abort = false;
                for (int channel = 0; channel < numChannels; ++channel)         // 3 or 4 channels (RGB[A])
                {
                    for (int offset = channel; offset < ImageLength; offset += numChannels)
                    {
                        //If it's a 16 bit value, for now we'll just ignore the least significant byte
                        try
                        {
                            byte fval = BytesPerPixelComponent == 1 ? SGIReader.Byte() : (byte)(SGIReader.Int16BE()>>8);
                            imageDataRGB8[offset] = fval;
                        }
                        catch (Exception)// ex)
                        {
                            //Console.WriteLine(ex.ToString());
                            abort = true;
                            break;
                        }
                    }
                    if (abort)
                        break;
                }
            }
            else// RLE
            {
                // TODO: leverage start offset and length tables if and when we have data that uses them
                // Table of start offsets
                int numScanlines = ySize;
               
                //uint[,] startTable = new uint[numChannels, numScanlines];
                for (int channel = 0; channel < numChannels; ++channel)
                {
                    for (int scanline = 0; scanline < numScanlines; ++scanline)
                    {
                        //startTable[channel, scanline] = SGIReader.ReadUInt32Big();
                        SGIReader.UInt32BE();
                    }
                }

                // Table of lengths
                //uint[,] lengthTable = new uint[numChannels, numScanlines];
                for (int channel = 0; channel < numChannels; ++channel)
                {
                    for (int scanline = 0; scanline < numScanlines; ++scanline)
                    {
                        //lengthTable[channel, scanline] = SGIReader.ReadUInt32Big();
                        SGIReader.UInt32BE();
                    }
                }
                
                // RLE data
                if (BytesPerPixelComponent == 1)
                {
                    byte[] byteArray = SGIReader.Bytes;
                    int inputIndex = SGIReader.Position;
                    int outputIndex = 0;

                    while (outputIndex < imageDataRGB8.Length)
                    {
                        byte val = byteArray[inputIndex];
                        int count = val & 0x7f;
                        if (count == 0)
                        {
                            inputIndex++;
                            continue;
                        }

                        bool flag = (val & 0x80) != 0;
                        if (flag)
                        {
                            while ((count--) != 0)
                            {
                                byte nextVal = byteArray[inputIndex + 1];
                                imageDataRGB8[outputIndex] = nextVal;
                                inputIndex++;
                                outputIndex++;
                            }

                            inputIndex++;
                        }
                        else
                        {
                            byte nextVal = byteArray[inputIndex + 1];
                            while ((count--) != 0)
                            {
                                imageDataRGB8[outputIndex] = nextVal;
                                outputIndex++;
                            }

                            inputIndex += 2;
                        }
                    }

                    if (inputIndex != byteArray.Length - 1 || outputIndex != imageDataRGB8.Length)
                        Console.WriteLine(string.Format("did not complete read of {0}", filename));

                    var tempBuffer = new byte[xSize * ySize * numChannels];
                    Array.Copy(imageDataRGB8, tempBuffer, imageDataRGB8.Length);
                    //Convert to interleaved
                    int pixelLen = width * height;
                    for (int i = 0; i < pixelLen; i++)
                    {
                        for (int channel = 0; channel < numChannels; channel++)
                        {
                            imageDataRGB8[(i * numChannels) + channel] = tempBuffer[(pixelLen * channel) + i];
                        }
                    }
                }
                else // TODO: handle shorts
                {
                    Console.WriteLine(string.Format("unsupported BytesPerPixelComponent: {0} in {1}", BytesPerPixelComponent, filename));
                    return null;
                }
            }

            if (downSample)
            {
                var downSampleBuffer = Downsample(imageDataRGB8, ref width, ref height, numChannels);
                return downSampleBuffer;
            }

            return imageDataRGB8;
        }

        byte[] Downsample(byte[] imageDataRGB8, ref int width, ref int height, int numChannels)
        {
            // divide each texture dimension by 2
            var buffer = new byte[width / 2 * height / 2 * numChannels];
            int destIndex = 0;
            int srcIndex = 0;
            while (destIndex + numChannels - 1 < buffer.Length)
            {
                for (int i = 0; i < numChannels; ++i)
                    buffer[destIndex + i] = imageDataRGB8[srcIndex + i];
                destIndex += numChannels;
                srcIndex += numChannels * 2;
                if (destIndex % (numChannels * width / 2) == 0)
                {
                    // skip row
                    srcIndex += width * numChannels;
                }
            }
            width /= 2;
            height /= 2;
            return buffer;
        }
    }
}
