
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Cognitics.Unity
{
    public class ResourceEntry_Image : ResourceEntry
    {
        public Image<Color32> Image = null;
    }

    public static class ResourceLoad
    {
        public static void JP2(string name, ResourceEntry re)
        {
            var entry = re as ResourceEntry_Image;
            try
            {
                var bytes = System.IO.File.ReadAllBytes(name);
                CSJ2K.Util.PortableImage img = CSJ2K.J2kImage.FromBytes(bytes);
                int[] ib = img.GetComponent(0);
                int[] ig = img.GetComponent(1);
                int[] ir = img.GetComponent(2);
                var image = new Image<Color32>();
                image.Width = img.Width;
                image.Height = img.Height;
                image.Channels = img.NumberOfComponents;
                image.Data = new Color32[ib.Length];
                for (int i = 0, c = image.Width * image.Height; i < c; ++i)
                {
                    ref Color32 color = ref image.Data[i];
                    color.r = (byte)ir[i];
                    color.g = (byte)ig[i];
                    color.b = (byte)ib[i];
                    color.a = 255;
                }
                entry.Image = image;
            }
            catch (Exception e)
            {
                entry.Image = new Image<Color32>();
                Debug.LogException(e);
            }
        }

        public static void RGB(string name, ResourceEntry re)
        {
            var entry = re as ResourceEntry_Image;
            try
            {
                var bytes = System.IO.File.ReadAllBytes(name);
                var sgi = SiliconGraphicsImage.ImageFromBytes(bytes);
                if (sgi is Image<byte>)
                {
                    var img = sgi as Image<byte>;
                    var image = new Image<Color32>();
                    image.Width = img.Width;
                    image.Height = img.Height;
                    image.Channels = img.Channels;
                    image.Data = new Color32[image.Width * image.Height];
                    for (int i = 0, c = image.Width * image.Height; i < c; ++i)
                    {
                        ref Color32 color = ref image.Data[i];
                        int index = i * image.Channels;
                        color.r = img.Data[index];
                        color.g = (image.Channels > 1) ? img.Data[index + 1] : img.Data[index];
                        color.b = (image.Channels > 2) ? img.Data[index + 2] : img.Data[index];
                        color.a = (image.Channels > 3) ? img.Data[index + 3] : (byte)255;
                    }
                    entry.Image = image;
                    return;
                }
                throw new FormatException("SGIReadJob: unsupported pixel format: " + sgi.Type.ToString());
            }
            catch (Exception e)
            {
                entry.Image = new Image<Color32>();
                Debug.LogException(e);
            }
        }

        public static void TIF(string name, ResourceEntry re)
        {
            var entry = re as ResourceEntry_Image;
            try
            {
                var bytes = System.IO.File.ReadAllBytes(name);
                var tif = GeoTiff.ImageFromBytes(bytes);
                if (tif is Image<byte>)
                {
                    var img = tif as Image<byte>;
                    var image = new Image<Color32>();
                    image.Width = img.Width;
                    image.Height = img.Height;
                    image.Channels = img.Channels;
                    image.Data = new Color32[image.Width * image.Height];
                    for (int i = 0, c = image.Width * image.Height; i < c; ++i)
                    {
                        ref Color32 color = ref image.Data[i];
                        int index = i * image.Channels;
                        color.r = img.Data[index];
                        color.g = (image.Channels > 1) ? img.Data[index + 1] : img.Data[index];
                        color.b = (image.Channels > 2) ? img.Data[index + 2] : img.Data[index];
                        color.a = (image.Channels > 3) ? img.Data[index + 3] : (byte)255;
                    }
                    entry.Image = image;
                    return;
                }
                if (tif is Image<short>)
                {
                    var img = tif as Image<short>;
                    var image = new Image<Color32>();
                    image.Width = img.Width;
                    image.Height = img.Height;
                    image.Channels = img.Channels;
                    image.Data = new Color32[image.Width * image.Height];
                    for (int i = 0, c = image.Width * image.Height; i < c; ++i)
                    {
                        ref Color32 color = ref image.Data[i];
                        int index = i * image.Channels;
                        color.r = (byte)(img.Data[index] / 255);
                        color.g = (image.Channels > 1) ? (byte)(img.Data[index + 1] / 255) : (byte)img.Data[index];
                        color.b = (image.Channels > 2) ? (byte)(img.Data[index + 2] / 255) : (byte)img.Data[index];
                        color.a = (image.Channels > 3) ? (byte)(img.Data[index + 3] / 255) : (byte)255;
                    }
                    entry.Image = image;
                    return;
                }
                if (tif is Image<ushort>)
                {
                    var img = tif as Image<ushort>;
                    var image = new Image<Color32>();
                    image.Width = img.Width;
                    image.Height = img.Height;
                    image.Channels = img.Channels;
                    image.Data = new Color32[image.Width * image.Height];
                    for (int i = 0, c = image.Width * image.Height; i < c; ++i)
                    {
                        ref Color32 color = ref image.Data[i];
                        int index = i * image.Channels;
                        color.r = (byte)(img.Data[index] / 255);
                        color.g = (image.Channels > 1) ? (byte)(img.Data[index + 1] / 255) : (byte)img.Data[index];
                        color.b = (image.Channels > 2) ? (byte)(img.Data[index + 2] / 255) : (byte)img.Data[index];
                        color.a = (image.Channels > 3) ? (byte)(img.Data[index + 3] / 255) : (byte)255;
                    }
                    entry.Image = image;
                    return;
                }
                if (tif is Image<int>)
                {
                    var img = tif as Image<int>;
                    var image = new Image<Color32>();
                    image.Width = img.Width;
                    image.Height = img.Height;
                    image.Channels = img.Channels;
                    image.Data = new Color32[image.Width * image.Height];
                    for (int i = 0, c = image.Width * image.Height; i < c; ++i)
                    {
                        ref Color32 color = ref image.Data[i];
                        int index = i * image.Channels;
                        color.r = (byte)(img.Data[index] / 255 / 255 / 255);
                        color.g = (image.Channels > 1) ? (byte)(img.Data[index + 1] / 255 / 255 / 255) : (byte)img.Data[index];
                        color.b = (image.Channels > 2) ? (byte)(img.Data[index + 2] / 255 / 255 / 255) : (byte)img.Data[index];
                        color.a = (image.Channels > 3) ? (byte)(img.Data[index + 3] / 255 / 255 / 255) : (byte)255;
                    }
                    entry.Image = image;
                    return;
                }
                if (tif is Image<uint>)
                {
                    var img = tif as Image<uint>;
                    var image = new Image<Color32>();
                    image.Width = img.Width;
                    image.Height = img.Height;
                    image.Channels = img.Channels;
                    image.Data = new Color32[image.Width * image.Height];
                    for (int i = 0, c = image.Width * image.Height; i < c; ++i)
                    {
                        ref Color32 color = ref image.Data[i];
                        int index = i * image.Channels;
                        color.r = (byte)(img.Data[index] / 255 / 255 / 255);
                        color.g = (image.Channels > 1) ? (byte)(img.Data[index + 1] / 255 / 255 / 255) : (byte)img.Data[index];
                        color.b = (image.Channels > 2) ? (byte)(img.Data[index + 2] / 255 / 255 / 255) : (byte)img.Data[index];
                        color.a = (image.Channels > 3) ? (byte)(img.Data[index + 3] / 255 / 255 / 255) : (byte)255;
                    }
                    entry.Image = image;
                    return;
                }
                if (tif is Image<float>)
                {
                    var img = tif as Image<float>;
                    var image = new Image<Color32>();
                    image.Width = img.Width;
                    image.Height = img.Height;
                    image.Channels = img.Channels;
                    image.Data = new Color32[image.Width * image.Height];
                    for (int i = 0, c = image.Width * image.Height; i < c; ++i)
                    {
                        ref Color32 color = ref image.Data[i];
                        int index = i * image.Channels;
                        color.r = (byte)(img.Data[index] * 255);
                        color.g = color.r;
                        color.b = color.r;
                        color.a = 255;
                    }
                    entry.Image = image;
                    return;
                }
                throw new FormatException("TIFReadJob: unsupported pixel format: " + tif.Type.ToString());
            }
            catch (Exception e)
            {
                entry.Image = new Image<Color32>();
                Debug.LogException(e);
            }
        }

    }



    // TODO: async/await or some such so we can derive from ReferenceManager rather than be a monobehaviour
    public class MaterialManager : MonoBehaviour    // monobehaviour so we can coroutine
    {
        private ReferenceManager<MaterialEntry> ReferenceManager = new ReferenceManager<MaterialEntry>();
        private ResourceManager ResourceManager = new ResourceManager(2);

        private void Start()
        {
            ResourceManager.SetLoadDelegate(new Regex(".jp2$", RegexOptions.Compiled | RegexOptions.IgnoreCase), ResourceLoad.JP2);
            ResourceManager.SetLoadDelegate(new Regex(".rgb$", RegexOptions.Compiled | RegexOptions.IgnoreCase), ResourceLoad.RGB);
            ResourceManager.SetLoadDelegate(new Regex(".tif$", RegexOptions.Compiled | RegexOptions.IgnoreCase), ResourceLoad.TIF);

            /*
            ReferenceManager.Debug = true;
            ResourceManager.Debug = true;
            if(ResourceManager.Debug)
                InvokeRepeating("RMUpdate", 1f, 1f);
                */
        }

        void RMUpdate()
        {
            ResourceManager.DumpToConsole();
        }

        public MaterialEntry Entry(string name)
        {
            // TODO: ReferenceManager needs to be improved
            var entry = ReferenceManager.Entry(name);
            if (entry.Manager == null)  // it's a new entry
                entry.ResourceEntry = ResourceManager.Fetch<ResourceEntry_Image>(name);
            else
                ResourceManager.Prod(name);
            entry.Manager = this;
            return entry;
        }
        public void Release(string name)
        {
            ReferenceManager.Release(name);
            ResourceManager.Release(name);
        }
        public long Memory() => ReferenceManager.Memory();

    }

    public class MaterialEntry : ReferenceEntry
    {
        internal MaterialManager Manager = null;
        internal ResourceEntry_Image ResourceEntry = null;
        public Material Material = null;
        private bool loading = false;

        public override bool Loaded
        {
            get
            {
                if (Material != null)
                    return true;
                if (!ResourceEntry.LoadComplete)
                    return false;
                if(ResourceEntry.Image.Data == null)
                    return true;
                if (loading)
                    return false;
                Manager.StartCoroutine(LoadCoroutine());
                return false;
            }
        }

        public override void Load(string name)
        {
            /*
            if (System.IO.Path.GetExtension(name) == ".rgb")
                ReadJob = new SGIReadJob() { Filename = name };
            if (System.IO.Path.GetExtension(name) == ".jp2")
                ReadJob = new JP2ReadJob() { Filename = name };
            if (System.IO.Path.GetExtension(name) == ".tif")
                ReadJob = new TIFReadJob() { Filename = name };
            if (ReadJob == null)
            {
                Debug.LogWarning("MaterialEntry.Load(" + name + "): unsupported file format");
                return;
            }
            ReadJob.Schedule();
                */
        }

        public override void Unload()
        {
            if (Material == null)
                return;
            if (Material.mainTexture != null)
                UnityEngine.Object.Destroy(Material.mainTexture);
            UnityEngine.Object.Destroy(Material);
            Material = null;
        }

        private IEnumerator LoadCoroutine()
        {
            loading = true;
            var image = ResourceEntry.Image;

            var texture = new Texture2D(image.Width, image.Height, TextureFormat.RGBA32, true);
            
            texture.wrapMode = TextureWrapMode.Repeat;

            yield return null;

            texture.SetPixels32(image.Data);

            yield return null;

            var material = new Material(Shader.Find("Cognitics/ModelStandard"));

            yield return null;

            material.mainTexture = texture;
            material.EnableKeyword("_ALPHATEST_ON");//_ALPHABLEND_ON
            material.EnableKeyword("_MetallicGlossMap");//_SPECGLOSSMAP
            material.SetFloat("_MetallicGlossMap", 0f);
            material.EnableKeyword("_Glossiness");
            material.SetFloat("_Glossiness", 0f);
            material.EnableKeyword("_SMOOTHNESS");
            material.SetFloat("_SMOOTHNESS", 0f);
            material.EnableKeyword("_SpecularHighlights");
            material.SetFloat("_SpecularHighlights", 0f);
            material.EnableKeyword("_GlossyReflections");
            material.SetFloat("_GlossyReflections", 0f);
            material.EnableKeyword("_Mode");
            material.SetFloat("_Mode", 1f);
            material.enableInstancing = true;

            yield return null;

            (material.mainTexture as Texture2D).Apply(true, true);

            yield return null;

            Memory = material.mainTexture.width * material.mainTexture.height * 4;
            Material = material; 
        }

    }

}
