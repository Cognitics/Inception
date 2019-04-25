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

    [HideInInspector] public ModelManager ModelManager;
    [HideInInspector] public MaterialManager MaterialManager;
    [HideInInspector] public MeshManager MeshManager;

    Dictionary<string, Material> Materials = null;
    List<string> MaterialNames = null;
    public bool Loaded = false;

    public void RunLoad()
    {
        if(MeshKey == null)
            MeshKey = ZipFilename != null ? System.IO.Path.Combine(Path, ZipFilename, FltFilename) : System.IO.Path.Combine(Path, FltFilename);
        var Lods = MeshManager.LodForName(MeshKey);
        if (Lods == null)
            return;

        if (Materials == null)
            Materials = new Dictionary<string, Material>();

        if(MaterialNames == null)
            MaterialNames = MeshManager.TexturesForMeshName(MeshKey);

        foreach (var name in MaterialNames)
        {
            string matKey = System.IO.Path.Combine(Path, name);
            var material = MaterialManager.MaterialForName(matKey);
            Materials[name] = material;
        }

        Loaded = true;
        foreach (var material in Materials)
        {
            if (material.Value == null)
                Loaded = false;
        }

        if (Loaded && (Lods.Count > 0))
        {
            var meshFilter = gameObject.AddComponent<MeshFilter>();
            // TEMP: start at the highest LOD. once we have LOD switching kicked on, we will start at the lowest
            meshFilter.mesh = Lods[0].mesh;

            var meshEntry = MeshManager.MeshByName[MeshKey];
            ModelManager.Models[this].meshEntry = meshEntry;

            var meshRenderer = gameObject.AddComponent<MeshRenderer>();
            if (Materials.Values.Count != 0)
            {
                // Check that the materials in question actually have textures, or we'll be assigning bad materials and in some cases we have more materials than submeshes.
                List<Material> materialsToAssign = new List<Material>();

                // There needs to be one material per submesh, and the mapping must be correct
                for (int i = 0; i < Lods[0].mesh.subMeshCount; ++i)
                {
                    int texturePatternIndex = meshEntry.submeshToTexturePatternIndex[i];
                    if (texturePatternIndex != -1)
                        materialsToAssign.Add(Materials[meshEntry.Textures[texturePatternIndex]]);
                }

                meshRenderer.materials = materialsToAssign.ToArray();
            }
            else
            {
                // Don't override single default material, just spit out a warning
                Debug.LogWarningFormat("no materials specified for {0}", FltFilename);
            }

            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            Materials = null;
        }
    }

    void OnDestroy()
    {
        ModelManager.Models.Remove(this);

        var meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.mesh != null)
            MeshManager.Dereference(MeshKey);

        if (MaterialNames != null)
        {
            foreach (var name in MaterialNames)
            {
                string matKey = System.IO.Path.Combine(Path, name);
                MaterialManager.Dereference(matKey);
            }
        }
    }
}
