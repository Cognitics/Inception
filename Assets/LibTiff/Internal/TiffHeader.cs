﻿namespace BitMiracle.LibTiff.Classic.Internal
{
    struct TiffHeader
    {
        public const int TIFF_MAGIC_SIZE = 2;
        public const int TIFF_VERSION_SIZE = 2;
        public const int TIFF_DIROFFSET_SIZE = 4;
        /// <summary>
        /// magic number (defines byte order)
        /// </summary>
        public short tiff_magic;

        /// <summary>
        /// TIFF version number
        /// </summary>
        public short tiff_version;

        /// <summary>
        /// byte offset to first directory
        /// </summary>
        public ulong tiff_diroff;
        /// <summary>
        /// reperesents the size in bytes of the offsets
        /// </summary>
        public short tiff_offsize;
        /// <summary>
        /// constant for possibly bigtiff convert
        /// </summary>
        public short tiff_fill;
        /// <summary>
        /// size in bytes of the header depending on the current format
        /// </summary>
        /// <param name="isBigTiff">if set to <c>true</c> then the bigtiff size will be returned.</param>
        /// <returns></returns>
        public static int SizeInBytes(bool isBigTiff)
        {
            if (isBigTiff) return 16;
            return 8;
        }
    }
}
