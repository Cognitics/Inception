using System;
using System.IO;

namespace Cognitics.CDB
{
    public static class SiliconGraphicsImage
    {
        private static sbyte Storage;
        private static sbyte BytesPerPixelComponent; // BPC
        private static ushort Dimension;
        private static ushort xSize, ySize, zSize;
        private static int PixMin, PixMax;
        private static string ImageName;
        private static int ColorMapID;

        private static long ImageLength;
        private static float[] imageDataFloat;
        private static byte[] imageDataRGB8;

        public static float[] Read(string filename, out int width, out int height, out int numChannels)
        {
            width = 0;
            height = 0;
            numChannels = 0;

            byte[] bytes = File.ReadAllBytes(filename);
            Stream stream = new MemoryStream(bytes);
            EndianBinaryReader SGIReader = new EndianBinaryReader(stream);

            /** Read Header **/
            short magic = SGIReader.ReadInt16Big();    // 474
            if(magic != 474)
            {
                Console.WriteLine(string.Format("file {0} does not match SGI format specifications. The file type declared in the header is not SGI.", filename));
                return null;
            }

            Storage = SGIReader.ReadSByte();                 // 0 or 1: 0 -> Verbatim, 1 -> Run Length Encoding
            BytesPerPixelComponent = SGIReader.ReadSByte();  // 1 or 2: 1 -> 1 byte per component, 2 -> 2 bytes per component
            Dimension = SGIReader.ReadUInt16Big();          // 1, 2, or 3 -> Number of Channels (Max 3, see ZSIZE)
            xSize = SGIReader.ReadUInt16Big();              // Column Size
            ySize = SGIReader.ReadUInt16Big();              // Row Size
            zSize = SGIReader.ReadUInt16Big();              // Number of channels (can be any value)
            width = xSize;
            height = ySize;
            PixMin = SGIReader.ReadInt32Big();              // Min pixel value (e.g. 0)
            PixMax = SGIReader.ReadInt32Big();              // Max pixel value (e.g. 255)
            //var EmptySpace = new string(SGIReader.ReadChars(4));
            SGIReader.ReadChars(4);
            //ImageName = OpenFlight.FltReader.GetString(SGIReader.ReadBytes(80)); // TODO: don't use FltReader
            SGIReader.ReadBytes(80);
            ColorMapID = SGIReader.ReadInt32Big();          // used for Run Length Encoding
            if (ColorMapID != 0)
            {
                Console.WriteLine(string.Format("unsupported color map id {0} in file {1}", ColorMapID, filename));
                return null;
            }

            //var Dummy = new string(SGIReader.ReadChars(404));
            SGIReader.ReadChars(404);

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
                        float fval = BytesPerPixelComponent == 1 ? SGIReader.ReadByte() : SGIReader.ReadInt16Big();
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
                        SGIReader.ReadUInt32Big();
                    }
                }


                // Table of lengths
                //uint[,] lengthTable = new uint[numChannels, numScanlines];
                for (int channel = 0; channel < numChannels; ++channel)
                {
                    for (int scanline = 0; scanline < numScanlines; ++scanline)
                    {
                        //lengthTable[channel, scanline] = SGIReader.ReadUInt32Big();
                        SGIReader.ReadUInt32Big();
                    }
                }
                // RLE data
                if (BytesPerPixelComponent == 1)
                {
                    int size = (int)SGIReader.BaseStream.Length - (int)SGIReader.BaseStream.Position;
                    byte[] byteArray = SGIReader.ReadBytes(size);
                    int inputIndex = 0;
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

        public static byte[] ReadRGB8(string filename, out int width, out int height, out int numChannels)
        {
            width = 0;
            height = 0;
            numChannels = 0;

            byte[] bytes = File.ReadAllBytes(filename);
            Stream stream = new MemoryStream(bytes);
            EndianBinaryReader SGIReader = new EndianBinaryReader(stream);

            /** Read Header **/
            short magic = SGIReader.ReadInt16Big();    // 474
            if (magic != 474)
            {
                Console.WriteLine(string.Format("file {0} does not match SGI format specifications. The file type declared in the header is not SGI.", filename));
                return null;
            }

            Storage = SGIReader.ReadSByte();                 // 0 or 1: 0 -> Verbatim, 1 -> Run Length Encoding
            BytesPerPixelComponent = SGIReader.ReadSByte();  // 1 or 2: 1 -> 1 byte per component, 2 -> 2 bytes per component
            Dimension = SGIReader.ReadUInt16Big();          // 1, 2, or 3 -> Number of Channels (Max 3, see ZSIZE)
            xSize = SGIReader.ReadUInt16Big();              // Column Size
            ySize = SGIReader.ReadUInt16Big();              // Row Size
            zSize = SGIReader.ReadUInt16Big();              // Number of channels (can be any value)
            width = xSize;
            height = ySize;
            PixMin = SGIReader.ReadInt32Big();              // Min pixel value (e.g. 0)
            PixMax = SGIReader.ReadInt32Big();              // Max pixel value (e.g. 255)
            //var EmptySpace = new string(SGIReader.ReadChars(4));
            SGIReader.ReadChars(4);
            //ImageName = OpenFlight.FltReader.GetString(SGIReader.ReadBytes(80)); // TODO: don't use FltReader
            SGIReader.ReadBytes(80);
            ColorMapID = SGIReader.ReadInt32Big();          // used for Run Length Encoding
            if (ColorMapID != 0)
            {
                Console.WriteLine(string.Format("unsupported color map id {0} in file {1}", ColorMapID, filename));
                return null;
            }

            //var Dummy = new string(SGIReader.ReadChars(404));
            SGIReader.ReadChars(404);

            if (Dimension == 1)
                numChannels = 1;
            else if (Dimension == 2)
                numChannels = 1;
            else if (Dimension >= 3)
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

                for (int channel = 0; channel < numChannels; ++channel)         // 3 or 4 channels (RGB[A])
                {
                    for (int offset = channel; offset < ImageLength; offset += numChannels)
                    {
                        //If it's a 16 bit value, for now we'll just ignore the least significant byte
                        byte fval = BytesPerPixelComponent == 1 ? SGIReader.ReadByte() : (byte)(SGIReader.ReadInt16Big()>>8);
                        imageDataRGB8[offset] = fval;
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
                        SGIReader.ReadUInt32Big();
                    }
                }
                

                // Table of lengths
                //uint[,] lengthTable = new uint[numChannels, numScanlines];
                for (int channel = 0; channel < numChannels; ++channel)
                {
                    for (int scanline = 0; scanline < numScanlines; ++scanline)
                    {
                        //lengthTable[channel, scanline] = SGIReader.ReadUInt32Big();
                        SGIReader.ReadUInt32Big();
                    }
                }
                
                // RLE data
                if (BytesPerPixelComponent == 1)
                {
                    int size = (int)SGIReader.BaseStream.Length - (int)SGIReader.BaseStream.Position;
                    byte[] byteArray = SGIReader.ReadBytes(size);
                    int inputIndex = 0;
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
            return imageDataRGB8;
        }
    }
}
