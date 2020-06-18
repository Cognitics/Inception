using System;
using UnityEngine;

namespace Cognitics.Unity
{
    public static class SGIReader
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
                var sgi = SiliconGraphicsImage.ImageFromBytes(bytes);
                var img = sgi as Image<byte>;
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
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            return null;
        }

    }
}


