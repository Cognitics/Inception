
using System;

namespace Cognitics.CoordinateSystems
{
    public abstract class EGM
    {
        public readonly int PostsPerDegree = 0;

        // image is +90,-180 in top left to -90,+180 in bottom right
        public Image<float> Image = new Image<float>();

        protected EGM(int postsPerDegree)
        {
            PostsPerDegree = postsPerDegree;
            Image.Width = Columns;
            Image.Height = Rows;
            Image.Channels = 1;
            Image.Data = new float[Image.Width * Image.Height];
        }

        protected int Rows => (180 * PostsPerDegree) + 1;
        protected int Columns => 360 * PostsPerDegree;
        protected int Row(double latitude) => (int)Math.Floor((90 - latitude) * PostsPerDegree);
        protected int Column(double longitude) => (int)Math.Floor((longitude + 180) * PostsPerDegree);
        protected double Latitude(int row) => 90 - ((double)row / PostsPerDegree);
        protected double Longitude(int column) => ((double)column / PostsPerDegree) - 180;

        protected int Index(double latitude, double longitude) => (Row(latitude) * Image.Width) + Column(longitude);


        public Tuple<float, float> Range()
        {
            float min = float.MaxValue;
            float max = float.MinValue;
            for (int i = 0, c = Image.Width * Image.Height; i < c; ++i)
            {
                min = Math.Min(min, Image.Data[i]);
                max = Math.Max(max, Image.Data[i]);
            }
            return new Tuple<float, float>(min, max);
        }

        public float Height(double latitude, double longitude)
        {
            double lat = Math.Max(Math.Min(latitude, 90.0), -89.9999);
            double lon = Math.Max(Math.Min(latitude, 180.0), -179.9999);
            int nw_row = Row(lat);
            int nw_col = Column(lon);
            if (nw_row >= Rows - 1)
                --nw_row;
            int rot_lon = (nw_col < Columns - 1) ? 0 : Columns;
            int nw_index = (nw_row * Columns) + nw_col;
            double nw_lat = Latitude(nw_row);
            double nw_lon = Longitude(nw_col);
            float nw_h = Image.Data[nw_index];
            float ne_h = Image.Data[nw_index + 1 - rot_lon];
            float sw_h = Image.Data[nw_index + Columns];
            float se_h = Image.Data[nw_index + Columns + 1 - rot_lon];
            float nx = (float)(lon - nw_lon) * PostsPerDegree;
            float ny = (float)(nw_lat - lat) * PostsPerDegree;
            float a00 = nw_h;
            float a10 = ne_h - nw_h;
            float a01 = sw_h - nw_h;
            float a11 = nw_h - ne_h - sw_h + se_h;
            float height = a00 + (a10 * nx) + (a01 * ny) + (a11 * nx * ny);
            return height;
        }

        public void WriteImage(string filename)
        {
            var bytes = GeoTiff.BytesFromImage(Image);
            System.IO.File.WriteAllBytes(filename, bytes);
        }

    }

}

