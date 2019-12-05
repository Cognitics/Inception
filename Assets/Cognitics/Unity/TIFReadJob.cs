
using System;
using UnityEngine;

namespace Cognitics.Unity
{
    public class TIFReadJob : ImageReadJob
    {
        public override void Execute()
        {
            base.Execute();
            var tif = GeoTiff.ImageFromBytes(FileBytes);
            if (tif is Image<byte>)
            {
                var img = tif as Image<byte>;
                Image.Width = img.Width;
                Image.Height = img.Height;
                Image.Channels = img.Channels;
                Image.Data = new Color32[Image.Width * Image.Height];
                for (int i = 0, c = Image.Width * Image.Height; i < c; ++i)
                {
                    ref Color32 color = ref Image.Data[i];
                    int index = i * Image.Channels;
                    color.r = img.Data[index];
                    color.g = (Image.Channels > 1) ? img.Data[index + 1] : img.Data[index];
                    color.b = (Image.Channels > 2) ? img.Data[index + 2] : img.Data[index];
                    color.a = (Image.Channels > 3) ? img.Data[index + 3] : (byte)255;
                }
                return;
            }
            if (tif is Image<short>)
            {
                var img = tif as Image<short>;
                Image.Width = img.Width;
                Image.Height = img.Height;
                Image.Channels = img.Channels;
                Image.Data = new Color32[Image.Width * Image.Height];
                for (int i = 0, c = Image.Width * Image.Height; i < c; ++i)
                {
                    ref Color32 color = ref Image.Data[i];
                    int index = i * Image.Channels;
                    color.r = (byte)(img.Data[index] / 255);
                    color.g = (Image.Channels > 1) ? (byte)(img.Data[index + 1] / 255) : (byte)img.Data[index];
                    color.b = (Image.Channels > 2) ? (byte)(img.Data[index + 2] / 255) : (byte)img.Data[index];
                    color.a = (Image.Channels > 3) ? (byte)(img.Data[index + 3] / 255) : (byte)255;
                }
                return;
            }
            if (tif is Image<ushort>)
            {
                var img = tif as Image<ushort>;
                Image.Width = img.Width;
                Image.Height = img.Height;
                Image.Channels = img.Channels;
                Image.Data = new Color32[Image.Width * Image.Height];
                for (int i = 0, c = Image.Width * Image.Height; i < c; ++i)
                {
                    ref Color32 color = ref Image.Data[i];
                    int index = i * Image.Channels;
                    color.r = (byte)(img.Data[index] / 255);
                    color.g = (Image.Channels > 1) ? (byte)(img.Data[index + 1] / 255) : (byte)img.Data[index];
                    color.b = (Image.Channels > 2) ? (byte)(img.Data[index + 2] / 255) : (byte)img.Data[index];
                    color.a = (Image.Channels > 3) ? (byte)(img.Data[index + 3] / 255) : (byte)255;
                }
                return;
            }
            if (tif is Image<int>)
            {
                var img = tif as Image<int>;
                Image.Width = img.Width;
                Image.Height = img.Height;
                Image.Channels = img.Channels;
                Image.Data = new Color32[Image.Width * Image.Height];
                for (int i = 0, c = Image.Width * Image.Height; i < c; ++i)
                {
                    ref Color32 color = ref Image.Data[i];
                    int index = i * Image.Channels;
                    color.r = (byte)(img.Data[index] / 255 / 255 / 255);
                    color.g = (Image.Channels > 1) ? (byte)(img.Data[index + 1] / 255 / 255 / 255) : (byte)img.Data[index];
                    color.b = (Image.Channels > 2) ? (byte)(img.Data[index + 2] / 255 / 255 / 255) : (byte)img.Data[index];
                    color.a = (Image.Channels > 3) ? (byte)(img.Data[index + 3] / 255 / 255 / 255) : (byte)255;
                }
                return;
            }
            if (tif is Image<uint>)
            {
                var img = tif as Image<uint>;
                Image.Width = img.Width;
                Image.Height = img.Height;
                Image.Channels = img.Channels;
                Image.Data = new Color32[Image.Width * Image.Height];
                for (int i = 0, c = Image.Width * Image.Height; i < c; ++i)
                {
                    ref Color32 color = ref Image.Data[i];
                    int index = i * Image.Channels;
                    color.r = (byte)(img.Data[index] / 255 / 255 / 255);
                    color.g = (Image.Channels > 1) ? (byte)(img.Data[index + 1] / 255 / 255 / 255) : (byte)img.Data[index];
                    color.b = (Image.Channels > 2) ? (byte)(img.Data[index + 2] / 255 / 255 / 255) : (byte)img.Data[index];
                    color.a = (Image.Channels > 3) ? (byte)(img.Data[index + 3] / 255 / 255 / 255) : (byte)255;
                }
                return;
            }
            if (tif is Image<float>)
            {
                var img = tif as Image<float>;
                Image.Width = img.Width;
                Image.Height = img.Height;
                Image.Channels = img.Channels;
                Image.Data = new Color32[Image.Width * Image.Height];
                for (int i = 0, c = Image.Width * Image.Height; i < c; ++i)
                {
                    ref Color32 color = ref Image.Data[i];
                    int index = i * Image.Channels;
                    color.r = (byte)(img.Data[index] * 255);
                    color.g = color.r;
                    color.b = color.r;
                    color.a = 255;
                }
                return;
            }
            throw new FormatException("TIFReadJob: unsupported pixel format: " + tif.Type.ToString());
        }





    }

}
