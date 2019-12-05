
using System;
using UnityEngine;

namespace Cognitics.Unity
{
    public class JP2ReadJob : ImageReadJob
    {
        public override void Execute()
        {
            base.Execute();
            CSJ2K.Util.PortableImage img = CSJ2K.J2kImage.FromBytes(FileBytes);
            int[] ib = img.GetComponent(0);
            int[] ig = img.GetComponent(1);
            int[] ir = img.GetComponent(2);
            Image.Width = img.Width;
            Image.Height = img.Height;
            Image.Channels = img.NumberOfComponents;
            Image.Data = new Color32[ib.Length];
            for (int i = 0, c = Image.Width * Image.Height; i < c; ++i)
            {
                ref Color32 color = ref Image.Data[i];
                color.r = (byte)ir[i];
                color.g = (byte)ig[i];
                color.b = (byte)ib[i];
                color.a = 255;
            }
        }
    }

}