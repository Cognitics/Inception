
using System;
using UnityEngine;

namespace Cognitics.Unity
{
    public class SGIReadJob : ImageReadJob
    {
        public override void Execute()
        {
            base.Execute();
            var sgi = SiliconGraphicsImage.ImageFromBytes(FileBytes);
            if (sgi is Image<byte>)
            {
                var img = sgi as Image<byte>;
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
            throw new FormatException("SGIReadJob: unsupported pixel format: " + sgi.Type.ToString());
        }
    }

}