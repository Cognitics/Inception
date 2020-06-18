using System;
using System.Collections.Generic;
using System.Text;

namespace Cognitics.GeoPackage
{
    public class Tile
    {
        public long ID;
        public long ZoomLevel;
        public long TileColumn;
        public long TileRow;
        public byte[] Bytes;


        public float[] RGBData()
        {

            // TODO: determine if PNG or JPG and call the appropriate handler
            // return RGBDataFromPNG();
            // return RGBDataFromJPG();

            return null;
        }



        private float[] RGBDataFromPNG()
        {
            // TODO: convert Bytes from PNG to interleaved RGB values
            return null;
        }

        private float[] RGBDataFromJPG()
        {
            // TODO: convert Bytes from JPG to interleaved RGB values
            return null;
        }


    }
}
