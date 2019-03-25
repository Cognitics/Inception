﻿// Copyright (c) 2007-2017 CSJ2K contributors.
// Licensed under the BSD 3-Clause License.

using CSJ2K.j2k.image;

namespace CSJ2K.Util
{
    public class UnityImageCreator : IImageCreator
    {
        #region FIELDS

        private static readonly IImageCreator Instance = new UnityImageCreator();

        #endregion

        #region PROPERTIES

        /// <summary>
        /// Gets whether or not this type is classified as a default manager.
        /// </summary>
        public bool IsDefault => true;

        #endregion

        #region METHODS

        public static void Register()
        {
            ImageFactory.Register(Instance);
        }

        public IImage Create(int width, int height, byte[] bytes)
        {
            return new UnityImage(width, height, bytes);
        }

        public BlkImgDataSrc ToPortableImageSource(object imageObject)
        {
            return UnityImageSource.Create(imageObject);
        }

        #endregion
    }
}
