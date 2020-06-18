using System;
using UnityEngine;

namespace Cognitics.Unity
{
    public static class TIFReader
    {
        public static Image<Color32> Read(string name)
        {
            try
            {
                var bytes = System.IO.File.ReadAllBytes(name);
                return Parse(bytes);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            return null;
        }

        public static Image<Color32> Parse(byte[] bytes)
        {
            try
            {
                var tif = GeoTiff.ImageFromBytes(bytes);
                if (tif is Image<byte>)
                {
                    var img = tif as Image<byte>;
                    var image = new Image<Color32>();
                    image.Width = img.Width;
                    image.Height = img.Height;
                    image.Channels = img.Channels;
                    image.Data = new Color32[image.Width * image.Height];
                    for (int i = 0, c = image.Width * image.Height; i < c; ++i)
                    {
                        ref Color32 color = ref image.Data[i];
                        int index = i * image.Channels;
                        color.r = img.Data[index];
                        color.g = (image.Channels > 1) ? img.Data[index + 1] : img.Data[index];
                        color.b = (image.Channels > 2) ? img.Data[index + 2] : img.Data[index];
                        color.a = (image.Channels > 3) ? img.Data[index + 3] : (byte)255;
                    }
                    return image;
                }
                if (tif is Image<short>)
                {
                    var img = tif as Image<short>;
                    var image = new Image<Color32>();
                    image.Width = img.Width;
                    image.Height = img.Height;
                    image.Channels = img.Channels;
                    image.Data = new Color32[image.Width * image.Height];
                    for (int i = 0, c = image.Width * image.Height; i < c; ++i)
                    {
                        ref Color32 color = ref image.Data[i];
                        int index = i * image.Channels;
                        color.r = (byte)(img.Data[index] / 255);
                        color.g = (image.Channels > 1) ? (byte)(img.Data[index + 1] / 255) : (byte)img.Data[index];
                        color.b = (image.Channels > 2) ? (byte)(img.Data[index + 2] / 255) : (byte)img.Data[index];
                        color.a = (image.Channels > 3) ? (byte)(img.Data[index + 3] / 255) : (byte)255;
                    }
                    return image;
                }
                if (tif is Image<ushort>)
                {
                    var img = tif as Image<ushort>;
                    var image = new Image<Color32>();
                    image.Width = img.Width;
                    image.Height = img.Height;
                    image.Channels = img.Channels;
                    image.Data = new Color32[image.Width * image.Height];
                    for (int i = 0, c = image.Width * image.Height; i < c; ++i)
                    {
                        ref Color32 color = ref image.Data[i];
                        int index = i * image.Channels;
                        color.r = (byte)(img.Data[index] / 255);
                        color.g = (image.Channels > 1) ? (byte)(img.Data[index + 1] / 255) : (byte)img.Data[index];
                        color.b = (image.Channels > 2) ? (byte)(img.Data[index + 2] / 255) : (byte)img.Data[index];
                        color.a = (image.Channels > 3) ? (byte)(img.Data[index + 3] / 255) : (byte)255;
                    }
                    return image;
                }
                if (tif is Image<int>)
                {
                    var img = tif as Image<int>;
                    var image = new Image<Color32>();
                    image.Width = img.Width;
                    image.Height = img.Height;
                    image.Channels = img.Channels;
                    image.Data = new Color32[image.Width * image.Height];
                    for (int i = 0, c = image.Width * image.Height; i < c; ++i)
                    {
                        ref Color32 color = ref image.Data[i];
                        int index = i * image.Channels;
                        color.r = (byte)(img.Data[index] / 255 / 255 / 255);
                        color.g = (image.Channels > 1) ? (byte)(img.Data[index + 1] / 255 / 255 / 255) : (byte)img.Data[index];
                        color.b = (image.Channels > 2) ? (byte)(img.Data[index + 2] / 255 / 255 / 255) : (byte)img.Data[index];
                        color.a = (image.Channels > 3) ? (byte)(img.Data[index + 3] / 255 / 255 / 255) : (byte)255;
                    }
                    return image;
                }
                if (tif is Image<uint>)
                {
                    var img = tif as Image<uint>;
                    var image = new Image<Color32>();
                    image.Width = img.Width;
                    image.Height = img.Height;
                    image.Channels = img.Channels;
                    image.Data = new Color32[image.Width * image.Height];
                    for (int i = 0, c = image.Width * image.Height; i < c; ++i)
                    {
                        ref Color32 color = ref image.Data[i];
                        int index = i * image.Channels;
                        color.r = (byte)(img.Data[index] / 255 / 255 / 255);
                        color.g = (image.Channels > 1) ? (byte)(img.Data[index + 1] / 255 / 255 / 255) : (byte)img.Data[index];
                        color.b = (image.Channels > 2) ? (byte)(img.Data[index + 2] / 255 / 255 / 255) : (byte)img.Data[index];
                        color.a = (image.Channels > 3) ? (byte)(img.Data[index + 3] / 255 / 255 / 255) : (byte)255;
                    }
                    return image;
                }
                if (tif is Image<float>)
                {
                    var img = tif as Image<float>;
                    var image = new Image<Color32>();
                    image.Width = img.Width;
                    image.Height = img.Height;
                    image.Channels = img.Channels;
                    image.Data = new Color32[image.Width * image.Height];
                    for (int i = 0, c = image.Width * image.Height; i < c; ++i)
                    {
                        ref Color32 color = ref image.Data[i];
                        int index = i * image.Channels;
                        color.r = (byte)(img.Data[index] * 255);
                        color.g = color.r;
                        color.b = color.r;
                        color.a = 255;
                    }
                    return image;
                }
                throw new FormatException("TIFReader: unsupported pixel format: " + tif.Type.ToString());
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            return null;
        }

    }
}




