
namespace Cognitics.CoordinateSystems
{
    public class EGM96 : EGM
    {
        EGM96() : base(4) { }

        //var egm = CreateFromNGA(@"EGM96_Interpolation_Grid\WW15MGH.GRD");
        static public EGM96 CreateFromNGA(string filename)
        {
            var egm = new EGM96();
            var lines = System.IO.File.ReadAllLines(filename);
            int line = 2;
            for (int row = 0; row < egm.Rows; ++row, line += 2)
            {
                int col = 0;
                for (int block = 0; block < 9; ++block, ++line)
                {
                    for (int blockrow = 0; blockrow < 20; ++blockrow, ++line)
                    {
                        for (int blockcol = 0; blockcol < 8; ++blockcol, ++col)
                        {
                            int index = (row * egm.Columns) + col;
                            int pos = 2 + (blockcol * 9);
                            double height = double.Parse(lines[line].Substring(pos, 8));
                            egm.Image.Data[index] = (float)height;
                        }
                    }
                }
            }
            return egm;
        }

        /*
        static public EGM96 CreateFromImage(string filename)
        {
            var bytes = System.IO.File.ReadAllBytes(filename);
            var egm = new EGM96();
            egm.Image = GeoTiff.ImageFromBytes(bytes) as Image<float>;
            return egm;
        }
        */

    }

}


