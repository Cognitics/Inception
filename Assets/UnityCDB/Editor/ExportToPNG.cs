#if UNITY_EDITOR && !UNITY_ANDROID && !UNITY_IOS
using System;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace Cognitics.UnityCDB
{
    public class ExportToPNG
    {
        [MenuItem("Project/Export Textures on Selected Objects to PNG")]
        public static void Execute()
        {
            int count = 0;
            string exportPath = Application.dataPath + "/../..";
            foreach (var gameObject in Selection.gameObjects)
            {
                var meshRenderers = gameObject.GetComponentsInChildren<MeshRenderer>();
                foreach (var meshRenderer in meshRenderers)
                {
                    foreach (var material in meshRenderer.sharedMaterials)
                    {
                        if (material == null)
                            continue;

                        Texture2D existingTexture = material.mainTexture as Texture2D;
                        if (existingTexture == null)
                            continue;

                        Texture2D newTexture = new Texture2D(existingTexture.width, existingTexture.height, TextureFormat.ARGB32, false);

                        Color[] pixels = null;
                        try
                        {
                            pixels = existingTexture.GetPixels();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                            continue;
                        }

                        newTexture.SetPixels(0,0, existingTexture.width, existingTexture.height, pixels);
                        newTexture.Apply();
                        byte[] bytes = newTexture.EncodeToPNG();

                        string path = string.Format("{0}/{1}.png", exportPath, count);
                        File.WriteAllBytes(path, bytes);

                        count++;
                    }
                }
            }

            Debug.LogFormat("Exported {0} PNGs to {1}", count, exportPath);
        }
    }
}
#endif
