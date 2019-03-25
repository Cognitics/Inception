
using UnityEngine;


/** Usage:
 * BitmapReader bReader = new BitmapReader();
 * Texture2D texture = bReader.LoadJPG(Width, Height, bytes);
 * GetComponent<Renderer>().material.mainTexture = bReader;
 **/

class BitmapReader
{
    public Texture2D LoadJPG(int Width, int Height, byte[] bytes)
    {
        Texture2D JPG = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        JPG.LoadImage(bytes);

        return JPG;
    }

    public Texture2D LoadPNG(int Width, int Height, byte[] bytes)
    {
        Texture2D PNG = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        PNG.LoadImage(bytes);

        return PNG;
    }

}
