
using System;
using BitMiracle.LibTiff.Classic;

namespace Cognitics.CDB
{
    public class LibTiffErrorHandler : TiffErrorHandler
    {
        static public void Apply()
        {
            Tiff.SetErrorHandler(new LibTiffErrorHandler());
        }

        public override void WarningHandler(Tiff tif, string method, string format, params Object[] args)
        {
            // noop: disable warning about the field ordering
        }

    }
}
