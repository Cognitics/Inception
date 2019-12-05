
using System;
using System.IO;
using BitMiracle.LibTiff.Classic;

namespace Cognitics
{
    public class GeoTiff
    {
        public static IImage ImageFromBytes(byte[] bytes)
        {
            var tiff = Tiff.ClientOpen("Cognitics.GeoTiff.ImageFromBytes", "r", new MemoryStream(bytes), new TiffStream());

            FieldValue[] value = tiff.GetField(TiffTag.SAMPLEFORMAT);
            var sampleFormat = (value != null) ? (SampleFormat)value[0].ToInt() : SampleFormat.INT;
            value = tiff.GetField(TiffTag.BITSPERSAMPLE);
            int bps = value[0].ToInt() / 8;
            IImage image = ImageFromSampleFormatAndBPS(sampleFormat, bps);
            if (image == null)
                throw new InvalidDataException("invalid sample format");

            value = tiff.GetField(TiffTag.IMAGEWIDTH);
            image.Width = value[0].ToInt();
            value = tiff.GetField(TiffTag.IMAGELENGTH);
            image.Height = value[0].ToInt();
            value = tiff.GetField(TiffTag.SAMPLESPERPIXEL);
            image.Channels = value[0].ToInt();

            int stride = tiff.ScanlineSize();
            byte[] buffer = new byte[stride];

            if (image.Type == typeof(byte))
            {
                var image8 = image as Image<byte>;
                image8.Data = new byte[image.Width * image.Height * image.Channels];
                for (int row = 0; row < image8.Height; ++row)
                {
                    if (!tiff.ReadScanline(buffer, row))
                        break;
                    buffer.CopyTo(image8.Data, row * image8.Width * image8.Channels);
                }
                return image8;
            }

            if (image.Type == typeof(short))
            {
                var image16 = image as Image<short>;
                image16.Data = new short[image.Width * image.Height];
                for (int row = 0; row < image16.Height; ++row)
                {
                    if (!tiff.ReadScanline(buffer, row))
                        break;
                    for (int col = 0; col < image16.Width; ++col)
                        image16.Data[(row * image16.Width) + col] = BitConverter.ToInt16(buffer, col * 2);
                }
                return image16;
            }

            if (image.Type == typeof(ushort))
            {
                var image16 = image as Image<ushort>;
                image16.Data = new ushort[image.Width * image.Height];
                for (int row = 0; row < image16.Height; ++row)
                {
                    if (!tiff.ReadScanline(buffer, row))
                        break;
                    for (int col = 0; col < image16.Width; ++col)
                        image16.Data[(row * image16.Width) + col] = BitConverter.ToUInt16(buffer, col * 2);
                }
                return image16;
            }

            if (image.Type == typeof(int))
            {
                var image32 = image as Image<int>;
                image32.Data = new int[image.Width * image.Height];
                for (int row = 0; row < image32.Height; ++row)
                {
                    if (!tiff.ReadScanline(buffer, row))
                        break;
                    for (int col = 0; col < image32.Width; ++col)
                        image32.Data[(row * image32.Width) + col] = BitConverter.ToInt32(buffer, col * 4);
                }
                return image32;
            }

            if (image.Type == typeof(uint))
            {
                var image32 = image as Image<uint>;
                image32.Data = new uint[image.Width * image.Height];
                for (int row = 0; row < image32.Height; ++row)
                {
                    if (!tiff.ReadScanline(buffer, row))
                        break;
                    for (int col = 0; col < image32.Width; ++col)
                        image32.Data[(row * image32.Width) + col] = BitConverter.ToUInt32(buffer, col * 4);
                }
                return image32;
            }

            if (image.Type == typeof(float))
            {
                var image32f = image as Image<float>;
                image32f.Data = new float[image.Width * image.Height];
                for (int row = 0; row < image32f.Height; ++row)
                {
                    if (!tiff.ReadScanline(buffer, row))
                        break;
                    // TODO: BlockCopy isn't working correctly?
                    //Buffer.BlockCopy(buffer, 0, data, row * image32f.Width, buffer.Length);
                    for (int col = 0; col < image32f.Width; ++col)
                        image32f.Data[(row * image32f.Width) + col] = BitConverter.ToSingle(buffer, col * 4);
                }
                return image32f;
            }

            return null;
        }

        private static IImage ImageFromSampleFormatAndBPS(SampleFormat sampleFormat, int bps)
        {
            if (sampleFormat == SampleFormat.IEEEFP)
                return new Image<float>();
            if ((sampleFormat == SampleFormat.INT) && (bps == 4))
                return new Image<int>();
            if ((sampleFormat == SampleFormat.UINT) && (bps == 4))
                return new Image<uint>();
            if ((sampleFormat == SampleFormat.INT) && (bps == 2))
                return new Image<short>();
            if ((sampleFormat == SampleFormat.UINT) && (bps == 2))
                return new Image<ushort>();
            if (bps == 1)
                return new Image<byte>();
            return null;
        }

        public static byte[] BytesFromImage(IImage image)
        {
            if (image.Type == typeof(float))
            {
                var img = image as Image<float>;
                var bytes = new byte[img.Data.Length];
                var mem = new MemoryStream();
                var tiff = Tiff.ClientOpen("Cognitics.GeoTiff.BytesFromImage", "w", mem, new TiffStream());
                tiff.SetField(TiffTag.SAMPLEFORMAT, SampleFormat.IEEEFP);
                tiff.SetField(TiffTag.PHOTOMETRIC, Photometric.MINISBLACK);
                tiff.SetField(TiffTag.IMAGEWIDTH, image.Width);
                tiff.SetField(TiffTag.IMAGELENGTH, image.Height);
                tiff.SetField(TiffTag.SAMPLESPERPIXEL, image.Channels);
                tiff.SetField(TiffTag.BITSPERSAMPLE, sizeof(float) * 8);
                tiff.SetField(TiffTag.ROWSPERSTRIP, 1);

                //tiff.SetField(TiffTag.ORIENTATION, Orientation.TOPLEFT);
                //tiff.SetField(TiffTag.COMPRESSION, Compression.NONE);
                //tiff.SetField(TiffTag.FILLORDER, FillOrder.MSB2LSB);

                byte[] buffer = new byte[image.Width * sizeof(float)];
                for (int row = 0; row < image.Height; ++row)
                {
                    for (int col = 0; col < image.Width; ++col)
                    {
                        var fbytes = BitConverter.GetBytes(img.Data[(row * image.Width) + col]);
                        buffer[(col * sizeof(float)) + 0] = fbytes[0];
                        buffer[(col * sizeof(float)) + 1] = fbytes[1];
                        buffer[(col * sizeof(float)) + 2] = fbytes[2];
                        buffer[(col * sizeof(float)) + 3] = fbytes[3];
                    }
                    tiff.WriteScanline(buffer, row);
                }
                tiff.Close();
                return mem.ToArray();
            }
            throw new NotImplementedException();
        }

    }

}
