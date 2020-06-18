using System;
using UnityEngine;

namespace Cognitics.Unity
{
    public static class JP2Reader
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
                var img = CSJ2K.J2kImage.FromBytes(bytes);
                int[] ib = img.GetComponent(0);
                int[] ig = img.GetComponent(1);
                int[] ir = img.GetComponent(2);
                var image = new Image<Color32>
                {
                    Width = img.Width,
                    Height = img.Height,
                    Channels = img.NumberOfComponents,
                    Data = new Color32[ib.Length]
                };
                for (int i = 0, c = image.Width * image.Height; i < c; ++i)
                {
                    ref Color32 color = ref image.Data[i];
                    color.r = (byte)ir[i];
                    color.g = (byte)ig[i];
                    color.b = (byte)ib[i];
                    color.a = 255;
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
