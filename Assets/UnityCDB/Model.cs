using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cognitics.CoordinateSystems;

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
    [HideInInspector] public Cognitics.Unity.MaterialManager MaterialManager;
    [HideInInspector] public MeshManager MeshManager;
    [HideInInspector] public NetTopologySuite.Features.Feature Feature;

    Dictionary<string, Cognitics.Unity.MaterialEntry> MaterialEntries = new Dictionary<string, Cognitics.Unity.MaterialEntry>();
    Dictionary<string, Material> Materials = new Dictionary<string, Material>();
    List<string> MaterialNames = null;
    public bool Loaded = false;
    public bool Valid = false;
    MaterialPropertyBlock PropertyBlock;
    public MeshFilter MeshFilter = null;
    public MeshRenderer MeshRenderer = null;
    public int CurrentLodIndex = -1;

    public Dictionary<string, Color> ColorByTag = new Dictionary<string, Color>();

    GeographicCoordinates GeographicCoordinates;

    IEnumerator Load()
    {
        //++ModelManager.ValidCount;
        //Loaded = true;
        //yield break;

        try
        {

            var position = gameObject.transform.position;
            var cartesianCoordinates = new CartesianCoordinates(position.x, position.z);
            GeographicCoordinates = cartesianCoordinates.TransformedWith(ModelManager.Database.Projection);

            if (ZipFilename == null)
                MeshKey = System.IO.Path.GetFullPath(System.IO.Path.Combine(Path, FltFilename));
            else
                MeshKey = System.IO.Path.Combine(System.IO.Path.GetFullPath(System.IO.Path.Combine(Path, ZipFilename)), FltFilename);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Loaded = true;
            yield break;
        }

        while (Meshes == null)
        {
            try
            {
                Meshes = MeshManager.LodForName(MeshKey);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Loaded = true;
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.1f);
        }

        if (Meshes.Count == 0)
        {
            gameObject.SetActive(false);
            Loaded = true;
            yield break;
        }

        while (MaterialNames == null)
        {
            try
            {
                MaterialNames = MeshManager.TexturesForMeshName(MeshKey);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Loaded = true;
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.1f);
        }

        yield return null;

        try
        {
            foreach (var name in MaterialNames)
            {
                string matKey = System.IO.Path.GetFullPath(System.IO.Path.Combine(Path, name));
                MaterialEntries[matKey] = MaterialManager.Entry(matKey);
                Materials[name] = null;
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Loaded = true;
            yield break;
        }

        yield return null;

        bool done = false;
        while (!done)
        {
            done = true;
            try
            {
                foreach (var name in MaterialNames)
                {
                    if (Materials[name] == null)
                    {
                        string matKey = System.IO.Path.GetFullPath(System.IO.Path.Combine(Path, name));
                        if (!MaterialEntries[matKey].Loaded)
                        {
                            done = false;
                            break;
                        }
                        Materials[name] = MaterialEntries[matKey].Material;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Loaded = true;
                yield break;
            }
            if (!done)
                yield return new WaitForSecondsRealtime(0.1f);
        }

        yield return null;

        var meshFilter = gameObject.AddComponent<MeshFilter>();
        CurrentLodIndex = Meshes.Count - 1;     // Start at min LOD     (??? - abrinton)
        meshFilter.mesh = Meshes[CurrentLodIndex].mesh;

        yield return null;

        try
        {
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
                MeshRenderer.sharedMaterials = materialsToAssign.ToArray();
            }
            else
            {
                // Don't override single default material, just spit out a warning
                Debug.LogWarningFormat("no materials specified for {0}", FltFilename);
            }

            MeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            MeshRenderer.receiveShadows = false;

            PropertyBlock = new MaterialPropertyBlock();

            ++ModelManager.ValidCount;

            Valid = true;

            InvokeRepeating("UpdateElevation", 1.0f, 2.0f);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Loaded = true;
            MeshRenderer.enabled = false;
            yield break;
        }

        // TODO: TEMPORARY FOR LIGHTHOUSE
        //meshFilter.mesh.RecalculateNormals();

        Loaded = true;
    }

    void UpdateElevation()
    {
        var position = gameObject.transform.position;
        position.y = ModelManager.Database.TerrainElevationAtLocation(GeographicCoordinates);
        gameObject.transform.position = position;
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
                if(Materials.ContainsKey(name) && (Materials[name] != null))
                    MaterialManager.Release(matKey);
            }
        }

    }



}
