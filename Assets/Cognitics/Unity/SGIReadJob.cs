using UnityEngine;

namespace Cognitics.Unity
{
    public class SGIReadJob : FileReadJob
    {
        public int Width = -1;
        public int Height = -1;
        public int Depth = -1;
        public Color32[] Pixels = null;

        public override void Execute()
        {
            base.Execute();
            var sgi = new CDB.SiliconGraphicsImage();
            var bytes = sgi.ReadRGB8("", FileBytes, out Width, out Height, out Depth);
            Pixels = new Color32[Width * Height];
            for (int i = 0, c = Width * Height; i < c; ++i)
            {
                ref Color32 color = ref Pixels[i];
                int index = i * Depth;
                color.r = bytes[index];
                color.g = (Depth > 1) ? bytes[index + 1] : bytes[index];
                color.b = (Depth > 2) ? bytes[index + 2] : bytes[index];
                color.a = (Depth > 3) ? bytes[index + 3] : (byte)255;
            }
        }
    }

}