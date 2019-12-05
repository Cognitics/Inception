
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace Cognitics.Unity.BlueMarble
{
    public class GeocellSparseTextureArray
    {
        public Texture2D MapTexture;
        public Texture2DArray TextureArray;
        public NativeArray<ushort> MapPixels;

        public GeocellSparseTextureArray(byte[] inventory, int dimension)
        {
            MapTexture = new Texture2D(360, 180, TextureFormat.R16, false);
            MapTexture.wrapMode = TextureWrapMode.Clamp;
            MapPixels = MapTexture.GetRawTextureData<ushort>();

            ushort texture_count = 0;
            for (int i = 0, c = 360 * 180; i < c; ++i)
            {
                MapPixels[i] = 0;
                if (inventory[i] == 0)
                    continue;
                ++texture_count;
                MapPixels[i] = texture_count;
            }
            MapTexture.Apply();

            TextureArray = new Texture2DArray(dimension, dimension, texture_count + 1, TextureFormat.RGBA32, false);
            TextureArray.wrapMode = TextureWrapMode.Clamp;
            var texture_pixels = new Color32[dimension * dimension];
            for (int i = 0, c = dimension * dimension; i < c; ++i)
                texture_pixels[i] = Color.clear;
            for (int texture_index = 0; texture_index <= texture_count; ++texture_index)
                TextureArray.SetPixels32(texture_pixels, texture_index);
        }

        public ushort TextureIndex(double latitude, double longitude)
        {
            int ilat = Mathf.FloorToInt((float)latitude + 90);
            int ilon = Mathf.FloorToInt((float)longitude + 180);
            int map_index = (360 * ilat) + ilon;
            return MapPixels[map_index];
        }

        public void SetTexturePixels(int texture_index, Color32[] pixels) => TextureArray.SetPixels32(pixels, texture_index);

        public void Apply() => TextureArray.Apply();

    }
}
