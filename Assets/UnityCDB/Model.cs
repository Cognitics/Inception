using System.Collections.Generic;
using UnityEngine;

public class Model : MonoBehaviour
{
    // NOTE: three strings might seem like overkill, but I'm aiming for correctness and clarity before we take shortcuts
    public string Path; // this is the path, without any filename
    public string ZipFilename; // this is the zip filename with .zip extension if it exists, w/o path
    public string FltFilename; // this is the flt filename with .flt extension, w/o path
    // e.g. Path[/ZipFilename]/FltFilename

    string MeshKey = null;
    public List<LODData> Meshes = null;


    [HideInInspector] public ModelManager ModelManager;
    [HideInInspector] public MaterialManager MaterialManager;
    [HideInInspector] public MeshManager MeshManager;
    [HideInInspector] public NetTopologySuite.Features.Feature Feature;

    Dictionary<string, Material> Materials = new Dictionary<string, Material>();
    List<string> MaterialNames = null;
    public bool Loaded = false;
    MaterialPropertyBlock PropertyBlock;
    public MeshFilter MeshFilter = null;
    public MeshRenderer MeshRenderer = null;
    public int CurrentLodIndex = -1;

    public Dictionary<string, Color> ColorByTag = new Dictionary<string, Color>();


    void Awake()
    {
        ColorByTag["test"] = Color.magenta; // DEBUG
    }

    public bool RunLoad()
    {
        if (MeshKey == null)
        {
            if (ZipFilename == null)
                MeshKey = System.IO.Path.GetFullPath(System.IO.Path.Combine(Path, FltFilename));
            else
                MeshKey = System.IO.Path.Combine(System.IO.Path.GetFullPath(System.IO.Path.Combine(Path, ZipFilename)), FltFilename);
        }

        Meshes = MeshManager.LodForName(MeshKey);
        if (Meshes == null)
            return false;

        if (MaterialNames == null)
            MaterialNames = MeshManager.TexturesForMeshName(MeshKey);

        foreach (var name in MaterialNames)
        {
            string matKey = System.IO.Path.GetFullPath(System.IO.Path.Combine(Path, name));
            var material = MaterialManager.MaterialForName(matKey);
            Materials[name] = material;
        }

        Loaded = true;
        foreach (var material in Materials)
        {
            if (material.Value == null)
                Loaded = false;
        }

        if (!Loaded)
            return false;

        if (Meshes.Count == 0)
            return false;

        var meshFilter = gameObject.AddComponent<MeshFilter>();
        // Start at min LOD
        CurrentLodIndex = Meshes.Count - 1;
        meshFilter.mesh = Meshes[CurrentLodIndex].mesh;

        var meshEntry = MeshManager.MeshByName[MeshKey];

        MeshRenderer = gameObject.AddComponent<MeshRenderer>();
        if (Materials.Values.Count != 0)
        {
            // Check that the materials in question actually have textures, or we'll be assigning bad materials 
            // and in some cases we have more materials than submeshes.
            List<Material> materialsToAssign = new List<Material>();

            // There needs to be one material per submesh, and the mapping must be correct
            for (int i = 0; i < Meshes[0].mesh.subMeshCount; ++i)
            {
                int texturePatternIndex = meshEntry.submeshToTexturePatternIndex[i];
                if (texturePatternIndex != -1)
                    materialsToAssign.Add(Materials[meshEntry.Textures[texturePatternIndex]]);
            }

            MeshRenderer.materials = materialsToAssign.ToArray();
        }
        else
        {
            // Don't override single default material, just spit out a warning
            Debug.LogWarningFormat("no materials specified for {0}", FltFilename);
        }

        MeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        MeshRenderer.receiveShadows = false;

        PropertyBlock = new MaterialPropertyBlock();

        return true;
    }

    public void SetMaxLod()
    {
        if (MeshFilter == null)
            return;
        if (Meshes != null && Meshes.Count > 0)
        {
            CurrentLodIndex = 0;
            MeshFilter.mesh = Meshes[CurrentLodIndex].mesh;
        }
    }

    public void HighlightForTag(string tag)
    {
        if (MeshRenderer == null)
            return;
        var color = ColorByTag.ContainsKey(tag) ? ColorByTag[tag] : Color.white;
        MeshRenderer.GetPropertyBlock(PropertyBlock);
        PropertyBlock.SetColor("_Color", color);
        MeshRenderer.SetPropertyBlock(PropertyBlock);
    }

    void OnDestroy()
    {
        ModelManager.Remove(this);

        if (Meshes != null)
            MeshManager.Dereference(MeshKey);

        if (MaterialNames != null)
        {
            foreach (var name in MaterialNames)
            {
                string matKey = System.IO.Path.GetFullPath(System.IO.Path.Combine(Path, name));
                if(Materials[name] != null)
                    MaterialManager.Dereference(matKey);
            }
        }
    }
}
