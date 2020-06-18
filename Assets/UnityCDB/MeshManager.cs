using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Cognitics.CDB.OpenFlight;
using Cognitics.UnityCDB;
using System.Threading;
using System.Collections;

public class MeshEntry
{
    internal FltDatabase Flt = null;
    internal List<LODData> Lods = null;
    internal int ReferenceCount = 0;
    internal bool Loaded = false;
    internal int Memory = 0;

    internal List<string> Textures = new List<string>();
    internal Dictionary<int, int> submeshToTexturePatternIndex = new Dictionary<int, int>();

    public CancellationTokenSource TaskLoadToken = new CancellationTokenSource();

    Vector3[] vertices;
    Vector3[] normals;
    Vector2[] uvs;

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

                var bytes = System.IO.File.ReadAllBytes(name);
                var parser = new Cognitics.BinaryParser(bytes);
                var reader = new FltReader(parser);
                Flt = new FltDatabase(null, RecordType.Invalid, reader, fltName);
                Flt.Parse();
            }
            else
            {
                byte[] bytes = null;
                ZipReader.DoDecompression(path, zipName, fltName, ref bytes);
                if (bytes == null)
                {
                    // TODO: this log is too spammy for a regular build right now. remove conditional check once that issue is addressed
                    if (Database.isDebugBuild)
                        Debug.LogErrorFormat("Missing model: {0} {1} {2}", path, zipName, fltName);
                    Loaded = true;
                    return;
                }
                var parser = new Cognitics.BinaryParser(bytes);
                var reader = new FltReader(parser);
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
        if ((Flt != null) && (Flt.geometryRecords.Count == 0))
        {
            Debug.LogError("[MeshManager] MeshEntry.TaskLoad(): no primary geometry for " + Flt.Path);
            Flt = null;
        }

        if (Flt != null)
        {
            vertices = new Vector3[Flt.VertexPalette.Dict.Count];
            normals = new Vector3[Flt.VertexPalette.Dict.Count];
            uvs = new Vector2[Flt.VertexPalette.Dict.Count];

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
        }

        Loaded = true;
    }

    internal void GenerateMeshes(Database db)
    {
        db.StartCoroutine(GenerateMeshesCoroutine());
    }


    // Create mesh with loaded flt and assign to Mesh
    public IEnumerator GenerateMeshesCoroutine()
    {
        Lods = new List<LODData>();

        if (Flt == null)
            yield break;

        if (true)
        {
            ////////////////////////////////////////////////////////////////////////////////
            // this breaks LODs but makes models work for san diego

            var mesh = new UnityEngine.Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            var lod_data = new LODData { mesh = mesh };
            Lods.Add(lod_data);
            var submeshes = new List<Submesh>();
            foreach (var flt_record in Flt.geometryRecords)
            {
                var id_record = flt_record as IdRecord;
                var rec_submeshes = id_record.Submeshes;
                /*
                for (int i = 0; i < rec_submeshes.Count; ++i)
                {
                    rec_submeshes[i].triangles.Reverse();
                    rec_submeshes[i].triangles.AddRange(rec_submeshes[i].backfaceTriangles);
                }
                */
                rec_submeshes.AddRange(Flt.Submeshes);
                submeshes.AddRange(rec_submeshes);
                yield return null;
            }

            Lods[0].mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            Lods[0].mesh.vertices = vertices;
            Lods[0].mesh.normals = normals;
            Lods[0].mesh.uv = uvs;

            if (submeshes == null)
            {
                Debug.LogError("[MeshManager] MeshEntry.GenerateMeshHelper() no meshes for " + Flt.Path);
                yield break;
            }

            var submeshes_temp = new List<Submesh>();
            foreach(var submesh in submeshes)
            {
                if (submesh.material == null)
                    continue;
                if (submesh.material.mainTexturePalette == null)
                    continue;
                submeshes_temp.Add(submesh);
            }
            submeshes = submeshes_temp;
            Lods[0].mesh.subMeshCount = submeshes.Count;
            for (int i = 0; i < submeshes.Count; ++i)
            {
                submeshToTexturePatternIndex[i] = submeshes[i].material.mainTexturePalette.texturePatternIndex;
                Lods[0].mesh.SetTriangles(submeshes[i].triangles, i);
                Memory += submeshes[i].triangles.Count * sizeof(int);
                yield return null;
            }

            /*
            Lods[0].mesh.subMeshCount = submeshes.Count;
            bool abort = false;
            for (int i = 0; i < submeshes.Count; ++i)
            {
                //int texturePatternIndex = -1;     // we have to have something here or the materials won't be created correctly
                int texturePatternIndex = 0;
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

                Lods[0].mesh.SetTriangles(submeshes[i].triangles, i);
                Memory += submeshes[i].triangles.Count * sizeof(int);

                // Recalculate bounds if needed.
                // NOTE: in Unity, bounds are automatically recalculated when triangles are set
                //Lods[lodIndex].mesh.RecalculateBounds();

                yield return null;
            }
            */


        }
        else ////////////////////////////////////////////////////////////////////////////////
        {

            int lodIndex = 0;
            foreach (var record in Flt.geometryRecords)
            {
                var mesh = new UnityEngine.Mesh();
                mesh.name = lodIndex.ToString();
                var lodData = new LODData { mesh = mesh };
                Lods.Add(lodData);
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

                var idRecord = record as IdRecord;
                var lodRecord = record as LevelOfDetail;
                if (lodRecord != null)
                {
                    if (lodRecord.significantSize == 0f)
                    {
                        // significantSize undefined -> pre-15.8
                        lodData.switchInDistanceSq = (float)lodRecord.switchInDistance * (float)lodRecord.switchInDistance;
                        lodData.switchOutDistanceSq = (float)lodRecord.switchOutDistance * (float)lodRecord.switchOutDistance;
                    }
                    else
                    {
                        lodData.CalculateFromSignificantSize((float)lodRecord.significantSize);
                    }
                }
                else
                {
                    lodData.switchInDistanceSq = 2500f * lodIndex * lodIndex;// TEMP - artificial distances for prototyping
                    lodData.switchOutDistanceSq = 2500f * (lodIndex + 1) * (lodIndex + 1);
                }

                var submeshes = record.Submeshes;
                for (int i = 0; i < submeshes.Count; ++i)
                {
                    submeshes[i].triangles.Reverse();
                    submeshes[i].triangles.AddRange(submeshes[i].backfaceTriangles);
                }

                // TODO: this isn't going to work right with LODs
                submeshes.AddRange(Flt.Submeshes);

                GenerateMeshHelper(lodIndex++, submeshes.ToArray(), vertices, normals, uvs);
            }
        }

        // Fix up switch-in distances. They should match the switch-out distances of the next highest quality LOD
        Lods[0].switchInDistanceSq = 0f;
        for (int i = 1; i < Lods.Count; ++i)
            Lods[i].switchInDistanceSq = Lods[i - 1].switchOutDistanceSq;

        Memory += vertices.Length * sizeof(float) * 3;
        Memory += normals.Length * sizeof(float) * 3;
        Memory += uvs.Length * sizeof(float) * 2;

        Flt = null;
        vertices = null;
        normals = null;
        uvs = null;
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
            //int texturePatternIndex = -1;     // we have to have something here or the materials won't be created correctly
            int texturePatternIndex = 0;
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
            Memory += submeshes[i].triangles.Count * sizeof(int);

            // Recalculate bounds if needed.
            // NOTE: in Unity, bounds are automatically recalculated when triangles are set
            //Lods[lodIndex].mesh.RecalculateBounds();
        }
    }
}

public class MeshManager
{
    public Database db;
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
                meshEntry.GenerateMeshes(db);
                return null;
                //meshEntry.GenerateMeshes();
                //++meshEntry.ReferenceCount;
                //return meshEntry.Lods;
            }
            // TaskLoad in progress or failed
            return null;
        }
        var newEntry = new MeshEntry();
        MeshByName[name] = newEntry;

        var token = newEntry.TaskLoadToken.Token;
        Task.Run(() => { newEntry.TaskLoad(name); }, token);

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

        meshEntry.TaskLoadToken.Cancel();
        meshEntry.TaskLoadToken.Dispose();

        MeshByName.Remove(name);
    }

    public long Memory()
    {
        long result = 0;
        foreach (var entry in MeshByName.Values)
        {
            if (entry.Lods == null)
                continue;
            result += entry.Memory;
        }
        return result;
    }

}
