
using UnityEngine;
using Unity.Collections;

namespace Cognitics.Unity.BlueMarble
{
    public class GeocellTexture
    {
        public Texture2D Texture;

        public GeocellTexture()
        {
            Texture = new Texture2D(360, 180, TextureFormat.RGBA32, false);
            Texture.wrapMode = TextureWrapMode.Clamp;
            Clear();
        }

        public NativeArray<Color32> Pixels => Texture.GetRawTextureData<Color32>();

        public int Index(double latitude, double longitude)
        {
            int ilat = CellLatitude(latitude) + 90;
            int ilon = CellLongitude(latitude, longitude) + 180;
            return (360 * ilat) + ilon;
        }

        public void Clear()
        {
            var pixels = Pixels;
            for (int i = 0; i < 360 * 180; ++i)
                pixels[i] = UnityEngine.Color.clear;
            Texture.Apply();
        }

        public void Set(double latitude, double longitude, Color32 color)
        {
            var pixels = Pixels;
            int index = Index(latitude, longitude);
            for(int i = 0, c = CellWidth(latitude); i < c; ++i)
                pixels[index + i] = color;
            Texture.Apply();
        }

        public Color32 Color(double latitude, double longitude)
        {
            var pixels = Pixels;
            return pixels[Index(latitude, longitude)];
        }

        int CellLatitude(double latitude) => Mathf.FloorToInt((float)latitude);

        int CellLongitude(double latitude, double longitude)
        {
            int width = CellWidth(latitude);
            int ilon = Mathf.FloorToInt((float)longitude);
            ilon += 180;
            ilon /= width;
            ilon *= width;
            return ilon - 180;
        }

        int CellWidth(double latitude)
        {
            if (latitude >= 89.0f)
                return 12;
            if (latitude >= 80.0f)
                return 6;
            if (latitude >= 75.0f)
                return 4;
            if (latitude >= 70.0f)
                return 3;
            if (latitude >= 50.0f)
                return 2;
            if (latitude >= -50.0f)
                return 1;
            if (latitude >= -70.0f)
                return 2;
            if (latitude >= -75.0f)
                return 3;
            if (latitude >= -80.0f)
                return 4;
            if (latitude >= -89.0f)
                return 6;
            return 12;
        }

    }
}
