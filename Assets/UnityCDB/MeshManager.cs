using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Cognitics.OpenFlight;
using Cognitics.UnityCDB;

public class MeshEntry
{
    internal FltDatabase Flt = null;
    internal List<LODData> Lods = null;
    internal int ReferenceCount = 0;
    internal bool Loaded = false;

    internal List<string> Textures = new List<string>();
    internal Dictionary<int, int> submeshToTexturePatternIndex = new Dictionary<int, int>();

    internal void TaskLoad(string name)
    {
        try
        {
            string path = Path.GetDirectoryName(name);
            string zipName = null;
            string fltName = Path.GetFileName(name);
            if (path.EndsWith(".zip"))
            {
                zipName = Path.GetFileName(path);
                path = Path.GetDirectoryName(path);
            }

            if (fltName == null)
            {
                Debug.LogErrorFormat("[MeshManager] MeshEntry.TaskLoad(): no FLT specified in {0}", name);
                Loaded = true;
                return;
            }

            if (zipName == null)
            {
                Stream stream = File.OpenRead(name);
                var reader = new FltReader(stream);
                Flt = new FltDatabase(null, RecordType.Invalid, reader, fltName);
                Flt.Parse();
            }
            else
            {
                byte[] bytes = null;
                ZipReader.DoDecompression(path, zipName, fltName, ref bytes);
                if (bytes == null)
                {
                    Debug.LogErrorFormat("Missing model: {0} {1} {2}", path, zipName, fltName);
                    Loaded = true;
                    return;
                }
                var reader = new FltReader(new MemoryStream(bytes));
                Flt = new FltDatabase(null, RecordType.Invalid, reader, fltName);
                Flt.Parse();
            }

            foreach (var texturePalette in Flt.TexturePalettes)
            {
                string texPath = Path.Combine(path, texturePalette.filename);
                if (File.Exists(texPath))
                {
                    Textures.Add(texPath);
                }
                else
                {
                    string textureFilename = Path.Combine(path, texturePalette.filename);
                    string zippath = Path.GetDirectoryName(textureFilename);
                    string zipfn = Path.GetFileNameWithoutExtension(textureFilename);
                    int us_index = 0;
                    for (int i = 0; i < 7; ++i)
                        us_index = zipfn.IndexOf('_', us_index) + 1;
                    zipfn = zipfn.Substring(0, us_index - 1);
                    string zipfile = Path.Combine(zippath, zipfn + ".zip");
                    texPath = Path.Combine(zipfile, Path.GetFileName(texturePalette.filename));
                    // TODO: we could query the zip here to verify the file exists within its contents
                    //if (File.Exists(texPath))
                    {
                        Textures.Add(texPath);
                    }
                    //else
                    //{
                    //    Debug.LogErrorFormat("[MeshManager] MeshEntry.TaskLoad(): texture {0} does not exist", texPath);
                    //    return;
                    //}
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        Loaded = true;
    }

    // Create mesh with loaded flt and assign to Mesh
    internal void GenerateMeshes()
    {
        Lods = new List<LODData>();

        if (Flt == null)
            return;

        if (Flt.geometryRecords.Count == 0)
        {
            Debug.LogError("[MeshManager] MeshEntry.GenerateMesh(): no primary geometry for " + Flt.Path);
            return;
        }

        var vertices = new Vector3[Flt.VertexPalette.Dict.Count];
        var normals = new Vector3[Flt.VertexPalette.Dict.Count];
        var uvs = new Vector2[Flt.VertexPalette.Dict.Count];

        int i = 0;
        foreach (var elem in Flt.VertexPalette.Dict)
        {
            var vert = elem.Value;
            if (vert is VertexWithColorNormal)
            {
                var v = vert as VertexWithColorNormal;
                vertices[i].x = (float)v.x;
                vertices[i].y = (float)v.z;
                vertices[i].z = (float)v.y;
                normals[i].x = v.i;
                normals[i].y = v.k;
                normals[i].z = v.j;
                uvs[i] = Vector2.zero;
            }
            else if (vert is VertexWithColorNormalUV)
            {
                var v = vert as VertexWithColorNormalUV;
                vertices[i].x = (float)v.x;
                vertices[i].y = (float)v.z;
                vertices[i].z = (float)v.y;
                normals[i].x = v.i;
                normals[i].y = v.k;
                normals[i].z = v.j;
                uvs[i].x = v.u;
                uvs[i].y = v.v;
            }
            else if (vert is VertexWithColorUV)
            {
                var v = vert as VertexWithColorUV;
                vertices[i].x = (float)v.x;
                vertices[i].y = (float)v.z;
                vertices[i].z = (float)v.y;
                normals[i] = Vector3.up;
                uvs[i].x = v.u;
                uvs[i].y = v.v;
            }
            else if (vert is VertexWithColor) // base class
            {
                var v = vert as VertexWithColor;
                vertices[i].x = (float)v.x;
                vertices[i].y = (float)v.z;
                vertices[i].z = (float)v.y;
                normals[i] = Vector3.up;
                uvs[i] = Vector2.zero;
            }

            ++i;
        }

        int lodIndex = 0;
        foreach (var record in Flt.geometryRecords)
        {
            var mesh = new UnityEngine.Mesh();
            var lodData = new LODData { mesh = mesh };
            Lods.Add(lodData);
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            var idRecord = record as IdRecord;
            var lodRecord = record as LevelOfDetail;
            if (lodRecord != null)
            {
                //lodData.switchInDistanceSq = (float)lodRecord.switchInDistance * (float)lodRecord.switchInDistance;
                //lodData.switchOutDistanceSq = (float)lodRecord.switchOutDistance * (float)lodRecord.switchOutDistance;

                lodData.switchInDistanceSq = 2500f * lodIndex * lodIndex;// TEMP - artificial distances for prototyping
                lodData.switchOutDistanceSq = 2500f * (lodIndex + 1) * (lodIndex + 1);
            }
            else
            {
                //lodData.switchInDistanceSq = 0f;
                //lodData.switchOutDistanceSq = float.MaxValue;

                lodData.switchInDistanceSq = 2500f * lodIndex * lodIndex;// TEMP - artificial distances for prototyping
                lodData.switchOutDistanceSq = 2500f * (lodIndex + 1) * (lodIndex + 1);
            }

            var submeshes = record.Submeshes;
            // TODO: this isn't going to work right with LODs
            submeshes.AddRange(Flt.Submeshes);

            for (i = 0; i < submeshes.Count; ++i)
            {
                submeshes[i].triangles.Reverse();
                submeshes[i].triangles.AddRange(submeshes[i].backfaceTriangles);
            }

            GenerateMeshHelper(lodIndex++, submeshes.ToArray(), vertices, normals, uvs);
        }
    }

    private void GenerateMeshHelper(int lodIndex, Submesh[] submeshes, Vector3[] vertices, Vector3[] normals, Vector2[] uvs)
    {
        Lods[lodIndex].mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        Lods[lodIndex].mesh.vertices = vertices;
        Lods[lodIndex].mesh.normals = normals;
        Lods[lodIndex].mesh.uv = uvs;

        if (submeshes == null)
        {
            Debug.LogError("[MeshManager] MeshEntry.GenerateMeshHelper() no meshes for " + Flt.Path);
            return;
        }

        Lods[lodIndex].mesh.subMeshCount = submeshes.Length;
        bool abort = false;
        for (int i = 0; i < submeshes.Length; ++i)
        {
            int texturePatternIndex = -1;
            if (submeshes[i].material != null && submeshes[i].material.mainTexturePalette != null)
                texturePatternIndex = submeshes[i].material.mainTexturePalette.texturePatternIndex;
            submeshToTexturePatternIndex[i] = texturePatternIndex;

            for (int j = 0; j < submeshes[i].triangles.Count; ++j)
            {
                if (submeshes[i].triangles[j] > vertices.Length - 1)
                {
                    Debug.LogError("[MeshManager] MeshEntry.GenerateMeshHelper() invalid vertex index for " + Flt.Path);
                    abort = true;
                    break;
                }
            }
            if (abort)
                break;

            Lods[lodIndex].mesh.SetTriangles(submeshes[i].triangles, i);
        }
    }
}

public class MeshManager
{
    public Dictionary<string, MeshEntry> MeshByName = new Dictionary<string, MeshEntry>();

    public List<LODData> LodForName(string name)
    {
        if (MeshByName.ContainsKey(name))
        {
            var meshEntry = MeshByName[name];
            if (meshEntry.Lods != null)
            {
                ++meshEntry.ReferenceCount;
                return meshEntry.Lods;
            }
            if (meshEntry.Loaded)
            {
                // TaskLoad() completed
                meshEntry.GenerateMeshes();
                ++meshEntry.ReferenceCount;
                return meshEntry.Lods;
            }
            // TaskLoad in progress or failed
            return null;
        }
        var newEntry = new MeshEntry();
        MeshByName[name] = newEntry;
        Task.Run(() => newEntry.TaskLoad(name));
        return null;
    }

    public List<string> TexturesForMeshName(string name)
    {
        if (!MeshByName.ContainsKey(name) || MeshByName[name].Lods == null)
            throw new Exception("[MeshManager] TexturesForMeshName() should not be called until MeshForName() returns a valid mesh");
        return MeshByName[name].Textures;
    }

    public void Dereference(string name)
    {
        if (!MeshByName.ContainsKey(name))
        {
            Debug.LogErrorFormat("[MeshManager] Dereference() attempt to dereference mesh {0} that does not exist", name);
            return;
        }
        var meshEntry = MeshByName[name];
        --meshEntry.ReferenceCount;
        if (meshEntry.ReferenceCount > 0)
            return;

        if (meshEntry.Lods != null)
        {
            foreach (var lod in meshEntry.Lods)
                UnityEngine.Object.Destroy(lod.mesh);
        }
        MeshByName.Remove(name);
    }
}
