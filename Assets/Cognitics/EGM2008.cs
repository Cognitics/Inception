
using System;

namespace Cognitics.CoordinateSystems
{
    public class EGM2008 : EGM
    {
        EGM2008() : base(24) { }

        //var egm = CreateFromNGA(@"EGM2008_Interpolation_Grid\Und_min2.5x2.5_egm2008_isw=82_WGS84_TideFree_SE");
        static public EGM2008 CreateFromNGA(string filename)
        {
            var egm = new EGM2008();
            var bytes = System.IO.File.ReadAllBytes(filename);
            for (int row = 0; row < egm.Rows; ++row)
            {
                for (int col = 0; col < egm.Columns; ++col)
                {
                    int img_index = (row * egm.Columns) + col;
                    int src_index = 1 + (row * (egm.Columns + 2)) + col;
                    egm.Image.Data[img_index] = BitConverter.ToSingle(bytes, src_index * sizeof(float));
                }
            }
            return egm;
        }

        /*
        static public EGM2008 CreateFromImage(string filename)
        {
            var bytes = System.IO.File.ReadAllBytes(filename);
            var egm = new EGM2008();
            egm.Image = GeoTiff.ImageFromBytes(bytes) as Image<float>;
            return egm;
        }
        */

    }

}


