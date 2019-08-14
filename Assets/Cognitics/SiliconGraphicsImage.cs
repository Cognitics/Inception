using System;
using System.IO;

namespace Cognitics
{
    public class SiliconGraphicsImage
    {
        public static IImage ImageFromBytes(byte[] bytes)
        {
            var parser = new BinaryParser(bytes);
            if (parser.Int16BE() != 474)
                throw new InvalidDataException("invalid header");
            var storage = parser.Byte();
            IImage image = ImageFromBytesPerPixel(parser.Byte());
            if(image == null)
                throw new InvalidDataException("invalid bpc");
            var dimension = parser.UInt16BE();
            image.Width = parser.UInt16BE();
            image.Height = parser.UInt16BE();
            image.Channels = parser.UInt16BE();
            if (dimension != 3)
                image.Channels = 1;
            parser.Position += 92;
            var colormap = parser.Int32BE();
            if (colormap != 0)
                throw new NotSupportedException("unsupported colormap");
            parser.Position += 404;
            switch (storage)
            {
                case 0: ParseVerbatim(image, parser); break;
                case 1: ParseRLE(image, parser); break;
                default:
                    throw new InvalidDataException("invalid storage value");
            }
            return image;
        }

        private static IImage ImageFromBytesPerPixel(byte bpc)
        {
            if (bpc == 1)
                return new Image<byte>();
            if (bpc == 2)
                return new Image<short>();
            return null;
        }

        private static void ParseVerbatim(IImage image, BinaryParser parser)
        {
            int size = image.Width * image.Height;
            if (image is Image<byte>)
            {
                var image8 = image as Image<byte>;
                image8.Data = new byte[size * image.Channels];
                for (int channel = 0; channel < image.Channels; ++channel)
                    for (int i = 0; i < size; ++i)
                        image8.Data[channel * (i * image.Channels)] = parser.Byte();
            }
            if (image is Image<short>)
            {
                var image16 = image as Image<short>;
                image16.Data = new short[size * image.Channels];
                for (int channel = 0; channel < image.Channels; ++channel)
                    for (int i = 0; i < size; ++i)
                        image16.Data[channel + (i * image.Channels)] = parser.Int16BE();
            }
        }

        private static void ParseRLE(IImage image, BinaryParser parser)
        {
            var startTable = new int[image.Height * image.Channels];
            for (int channel = 0; channel < image.Channels; ++channel)
                for (int y = 0; y < image.Height; ++y)
                    startTable[(channel * image.Height) + y] = parser.Int32BE();
            var lengthTable = new int[image.Height * image.Channels];
            for (int channel = 0; channel < image.Channels; ++channel)
                for (int y = 0; y < image.Height; ++y)
                    lengthTable[(channel * image.Height) + y] = parser.Int32BE();
            if (image is Image<byte>)
            {
                var image8 = image as Image<byte>;
                image8.Data = new byte[image.Width * image.Height * image.Channels];
                for (int channel = 0; channel < image.Channels; ++channel)
                {
                    for (int y = 0; y < image.Height; ++y)
                    {
                        var rleStart = startTable[(channel * image.Height) + y];
                        var rleLength = lengthTable[(channel * image.Height) + y];
                        int position = rleStart;
                        int target = channel + (y * image.Width * image.Channels);
                        while (true)
                        {
                            byte value = parser.Bytes[position];
                            ++position;
                            int count = value & 0x7F;
                            if (count == 0)
                                break;
                            if ((value & 0x80) > 0)
                            {
                                for (int i = 0; i < count; ++i, target += image.Channels, ++position)
                                    image8.Data[target] = parser.Bytes[position];
                            }
                            else
                            {
                                for (int i = 0; i < count; ++i, target += image.Channels)
                                    image8.Data[target] = parser.Bytes[position];
                                ++position;
                            }
                        }
                    }
                }
            }
            if (image is Image<short>)
            {
                throw new NotImplementedException();
            }
        }

        public static byte[] BytesFromImage(IImage image)
        {
            throw new NotImplementedException();
        }


    }


}
