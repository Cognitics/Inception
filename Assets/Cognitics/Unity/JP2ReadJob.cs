using UnityEngine;

namespace Cognitics.Unity
{
    public class JP2ReadJob : FileReadJob
    {
        public int Width = -1;
        public int Height = -1;
        public int Depth = -1;
        public Color32[] Pixels = null;

        public override void Execute()
        {
            base.Execute();
            CSJ2K.Util.PortableImage img = CSJ2K.J2kImage.FromBytes(FileBytes);
            int[] ib = img.GetComponent(0);
            int[] ig = img.GetComponent(1);
            int[] ir = img.GetComponent(2);
            Pixels = new Color32[ib.Length];
            for (int i = 0, c = Width * Height; i < c; ++i)
            {
                ref Color32 color = ref Pixels[i];
                color.r = (byte)ir[i];
                color.g = (byte)ig[i];
                color.b = (byte)ib[i];
                color.a = 255;
            }

        }
    }

}