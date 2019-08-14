using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Cognitics.OpenFlight;
using Cognitics.UnityCDB;
using System.Threading;

public class MaterialEntry
{
    internal UnityEngine.Material Material = null;
    internal Texture2D Texture = null;
    internal int ReferenceCount = 0;
    internal bool Loaded = false;
    internal int Memory = 0;
    public CancellationTokenSource TaskLoadToken = new CancellationTokenSource();

    internal int Width = 1;
    internal int Height = 1;
    internal Color32[] Pixels = new Color32[1] { new Color32(0, 255, 255, 255) };

    //internal Cognitics.Unity.SGIReadJob SGIReadJob;


    internal void TaskLoad(string name)
    {
        try
        {
            string path = Path.GetDirectoryName(name);
            string zipName = null;
            string texName = Path.GetFileName(name);
            if (path.EndsWith(".zip"))
            {
                zipName = Path.GetFileName(path);
                path = Path.GetDirectoryName(path);
            }

            if (texName == null)
            {
                Console.WriteLine(string.Format("no texture specified in {0}", name));
                Loaded = true;
                return;
            }

            Cognitics.OpenFlight.Texture fltTexture = null;

            if (zipName == null)
            {
                fltTexture = new Cognitics.OpenFlight.Texture(name);
                fltTexture.Parse();
            }
            else
            {
                byte[] bytes = null;
                ZipReader.DoDecompression(path, zipName, texName, ref bytes);
                fltTexture = new Cognitics.OpenFlight.Texture(texName);
                fltTexture.Parse(bytes);
            }

            if ((fltTexture.Width == 0) || (fltTexture.Height == 0))
            {
                Debug.LogError("[MaterialManager] MaterialEntry.TaskLoad(): empty texture for " + fltTexture.Path);
            }
            else
            {
                Width = fltTexture.Width;
                Height = fltTexture.Height;
                Pixels = new Color32[Width * Height];
                SetPixelsForTexture(fltTexture);
                Memory = Width * Height * 6;    // 6 = 1.5 * 4 to include mipmapping
            }

        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
        Loaded = true;
    }

    void SetPixelsForTexture(Cognitics.OpenFlight.Texture fltTexture)
    {
        int i = 0;
        for (int c = 0; c < Pixels.Length; c++, i += fltTexture.NumChannels)
        {
            ref Color32 color = ref Pixels[c];
            int index = i + fltTexture.NumChannels - 1;
            if (fltTexture.rgb.Length - 1 < index)
            {
                Debug.LogErrorFormat("[MaterialManager] MaterialEntry.SetPixelsForTexture() rgb array does not contain all required color data! {0}", fltTexture.Path);
                break;
            }
            if (fltTexture.NumChannels == 1)
            {
                color.r = fltTexture.rgb[i];
                color.g = fltTexture.rgb[i];
                color.b = fltTexture.rgb[i];
            }
            else if (fltTexture.NumChannels == 2)
            {
                byte grayscale = (fltTexture.rgb[i]);
                color.r = grayscale;
                color.g = grayscale;
                color.b = grayscale;
                color.a = (fltTexture.rgb[i + 1]);
            }
            else if (fltTexture.NumChannels == 3 || fltTexture.NumChannels == 4)
            {
                color.r = fltTexture.rgb[i];
                color.g = fltTexture.rgb[i + 1];
                color.b = fltTexture.rgb[i + 2];
            }
            else
            {
                Debug.LogErrorFormat("[MaterialManager] MaterialEntry.SetPixelsForTexture() rgb array has unexpected number of channels! {0}", fltTexture.Path);
            }
            color.a = fltTexture.NumChannels < 4 ? (byte)255 : fltTexture.rgb[i + 3];
        }
    }

    // Create material with loaded texture and assign to Material
    internal void GenerateMaterial()
    {
        Texture = new Texture2D(Width, Height, TextureFormat.RGBA32, true);
        Texture.wrapMode = TextureWrapMode.Repeat;
        Texture.SetPixels32(Pixels);
        if (MaterialManager.Shader == null)
            MaterialManager.Shader = Shader.Find("Cognitics/ModelStandard");
        Material = new UnityEngine.Material(MaterialManager.Shader);
        Material.mainTexture = Texture;
        if (Material.mainTexture != null)
        {
            Material.EnableKeyword("_ALPHATEST_ON");//_ALPHABLEND_ON
            Material.EnableKeyword("_MetallicGlossMap");//_SPECGLOSSMAP
            Material.SetFloat("_MetallicGlossMap", 0f);
            Material.EnableKeyword("_Glossiness");
            Material.SetFloat("_Glossiness", 0f);
            Material.EnableKeyword("_SMOOTHNESS");
            Material.SetFloat("_SMOOTHNESS", 0f);
            Material.EnableKeyword("_SpecularHighlights");
            Material.SetFloat("_SpecularHighlights", 0f);
            Material.EnableKeyword("_GlossyReflections");
            Material.SetFloat("_GlossyReflections", 0f);
            Material.EnableKeyword("_Mode");
            Material.SetFloat("_Mode", 1f);
            Material.enableInstancing = true;

            (Material.mainTexture as Texture2D).Apply(true, true);
        }
    }

}

public class MaterialManager
{
    public Dictionary<string, MaterialEntry> MaterialByName = new Dictionary<string, MaterialEntry>();
    public static Shader Shader = null;

    public UnityEngine.Material MaterialForName(string name)
    {
        if (MaterialByName.ContainsKey(name))
        {
            var matEntry = MaterialByName[name];
            if (matEntry.Material != null)
            {
                ++matEntry.ReferenceCount;
                return matEntry.Material;
            }
            if (matEntry.Loaded)
            {
                // TaskLoad() completed
                matEntry.GenerateMaterial();
                ++matEntry.ReferenceCount;
                return matEntry.Material;
            }
            // TaskLoad in progress

            /*
            if ((matEntry.SGIReadJob != null) && matEntry.SGIReadJob.IsCompleted)
            {
                matEntry.SGIReadJob.Complete();
                matEntry.Width = matEntry.SGIReadJob.Width;
                matEntry.Height = matEntry.SGIReadJob.Height;
                matEntry.Pixels = matEntry.SGIReadJob.Pixels;
                matEntry.GenerateMaterial();
                ++matEntry.ReferenceCount;
                return matEntry.Material;
            }
            */

            return null;
        }
        var newMatEntry = new MaterialEntry();
        MaterialByName[name] = newMatEntry;

        /*
        if (Path.GetExtension(name) == ".rgb")
        {
            newMatEntry.SGIReadJob = new Cognitics.Unity.SGIReadJob() { Filename = name };
            newMatEntry.SGIReadJob.Execute();
            return null;
        }
        */


        var token = newMatEntry.TaskLoadToken.Token;
        Task.Run(() => { newMatEntry.TaskLoad(name); }, token);

        return null;
    }

    public void Dereference(string name)
    {
        if (!MaterialByName.ContainsKey(name))
        {
            Debug.LogErrorFormat("[MaterialManager] Dereference() attempt to dereference material {0} that does not exist", name);
            return;
        }
        var matEntry = MaterialByName[name];
        --matEntry.ReferenceCount;
        if (matEntry.ReferenceCount > 0)
            return;

        if (matEntry.Material != null)
        {
            if (matEntry.Material.mainTexture != null)
                UnityEngine.Object.Destroy(matEntry.Material.mainTexture);
            UnityEngine.Object.Destroy(matEntry.Material);
        }

        matEntry.TaskLoadToken.Cancel();
        matEntry.TaskLoadToken.Dispose();

        MaterialByName.Remove(name);
    }

    public long Memory()
    {
        long result = 0;
        foreach (var entry in MaterialByName.Values)
        {
            if (entry.Material == null)
                continue;
            result += entry.Memory;
        }
        return result;
    }


}
