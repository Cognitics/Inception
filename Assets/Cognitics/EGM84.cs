
namespace Cognitics.CoordinateSystems
{
    public class EGM84 : EGM
    {
        EGM84() : base(2) { }

        //var egm = CreateFromNGA(@"EGM84_Interpolation_Grid\WWGRID.TXT");
        static public EGM84 CreateFromNGA(string filename)
        {
            var egm = new EGM84();
            var lines = System.IO.File.ReadAllLines(filename);
            for (int line = 0, row = 0; row < egm.Rows; ++row, ++line)
            {
                for (int col = 0; col < egm.Columns; ++col, ++line)
                {
                    int index = (row * egm.Columns) + col;
                    var words = lines[line].Split(' ');
                    double height = double.Parse(words[3]);
                    egm.Image.Data[index] = (float)height;
                }
            }
            return egm;
        }

        /*
        static public EGM84 CreateFromImage(string filename)
        {
            var bytes = System.IO.File.ReadAllBytes(filename);
            var egm = new EGM84();
            egm.Image = GeoTiff.ImageFromBytes(bytes) as Image<float>;
            return egm;
        }
        */

    }

}


