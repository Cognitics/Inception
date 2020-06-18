
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
        public string name;
    }

    public static class ResourceLoad
    {
        public static void JP2(string name, ResourceEntry re)
        {
            var entry = re as ResourceEntry_Image;
            entry.name = name;
            entry.Image = JP2Reader.Read(name);
            if (entry.Image == null)
                entry.Image = new Image<Color32>();
        }

        public static void RGB(string name, ResourceEntry re)
        {
            var entry = re as ResourceEntry_Image;
            entry.name = name;
            entry.Image = SGIReader.Read(name);
            if (entry.Image == null)
                entry.Image = new Image<Color32>();
        }

        public static void TIF(string name, ResourceEntry re)
        {
            var entry = re as ResourceEntry_Image;

            // TODO: remove this when WMS is integrated into resource manager tasks
            try
            {
                byte[] bytes = null;
                while (true)
                {
                    try
                    {
                        bytes = System.IO.File.ReadAllBytes(name);
                        break;
                    }
                    catch (System.IO.IOException e)
                    {
                        int HResult = System.Runtime.InteropServices.Marshal.GetHRForException(e);
                        const int SharingViolation = 32;
                        if ((HResult & 0xFFFF) == SharingViolation)
                            continue;
                        throw;
                    }
                }
            }
            catch (Exception e)
            {
                entry.Image = new Image<Color32>();
                Debug.LogException(e);
            }


            entry.name = name;
            entry.Image = TIFReader.Read(name);
            if (entry.Image == null)
                entry.Image = new Image<Color32>();
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

            ReferenceManager.Debug = false;
            ResourceManager.Debug = false;
            
            /*
            if (ResourceManager.Debug)
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
        public bool debug = false;

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
