using System;
using System.IO;
using System.Collections.Generic;

namespace Cognitics.UnityCDB
{
    public class ZipReader
    {
        static public void DoDecompression(string path, string file, string desiredFile, ref byte[] bytes)
        {
            string filename = null;
            if (!string.IsNullOrEmpty(file))
                filename = string.Format("{0}/{1}", path, file);
            else
                filename = path;

//Windows & WSA10 only (see lzip.cs for more info)
#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_WSA)
            lzip.setEncoding(65001);//CP_UTF8  // CP_OEMCP/UNICODE = 1
#endif

            lzip.entry2Buffer(filename, desiredFile, ref bytes);
        }
    }
}
