using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Cognitics.OpenFlight;

public class OpenFlightRecordSet : MonoBehaviour//ScriptableObject
{
    public string Filename = null;
    public Transform Root = null;
    //public GameObject UserObject = null;
    public bool Verbose = false;
    public Dictionary<IdRecord, IdRecordData> IdRecords = new Dictionary<IdRecord, IdRecordData>();
    public Dictionary<string, TextureData> Textures = new Dictionary<string, TextureData>(); // TODO: key by string hash or other
    public List<MaterialData> Materials = new List<MaterialData>();
    [HideInInspector] public Bounds Bounds = new Bounds();

    protected bool missingNormals = false;
    protected bool missingUVs = false;

    private List<FltDatabase> fltDBs = new List<FltDatabase>();

    #region Classes

    public class IdRecordData
    {
        public GameObject GameObj = null;
        public Bounds Bounds = new Bounds();
        public List<SubmeshData> Submeshes = new List<SubmeshData>();
        public List<Vector3> Vertices = new List<Vector3>();
        public List<Vector3> Normals = new List<Vector3>();
        public List<Vector2> UVs = new List<Vector2>();

        public IdRecordData(GameObject gameObj)
        {
            GameObj = gameObj;
        }

        public SubmeshData GetOrAddSubmesh(Face face, MaterialData materialData, int[] triangles)
        {
            foreach (var submesh in Submeshes)
            {
                if (submesh.materialData.Equals(materialData.materialPalette, materialData.mainTexturePalette, materialData.detailTexturePalette, face.transparency, face.lightMode))
                {
                    return submesh;
                }
            }

            var submeshData = new SubmeshData(materialData, triangles);
            Submeshes.Add(submeshData);

            return submeshData;
        }
    }

    public class MaterialData
    {
        public MaterialPalette materialPalette = null;
        public TexturePalette mainTexturePalette = null;
        public TexturePalette detailTexturePalette = null;

        public Material CreateMaterial()
        {
            var shader = Shader.Find("Standard");
            var material = new Material(shader);

            return material;
        }

        public bool Equals(MaterialPalette otherMaterialPalette, TexturePalette otherMainTexturePalette, TexturePalette otherDetailTexturePalette, ushort otherTransparency, Face.LightMode otherLightMode)
        {
            return Equals(materialPalette, otherMaterialPalette) && 
                Equals(mainTexturePalette, otherMainTexturePalette) && 
                Equals(detailTexturePalette, otherDetailTexturePalette);
        }
    }

    public class TextureData
    {
        public int width = 0;
        public int height = 0;
        public int numChannels = 0;
        public Color32[] colors = null;
    }

    public class SubmeshData
    {
        public MaterialData materialData = null;
        public List<int> triangles = new List<int>();

        public SubmeshData(MaterialData data, int[] _triangles)
        {
            materialData = data;
            triangles.Add(_triangles[0]);
            triangles.Add(_triangles[1]);
            triangles.Add(_triangles[2]);
        }

        //public bool Equals()
        //{
        //    return false;
        //}
    }

    #endregion

    #region Materials and Textures

    public MaterialData GetOrAddMaterial(Face face)
    {
        var materialPalette = FindMaterialPalette(face);
        var mainTexturePalette = FindMainTexturePalette(face);
        var detailTexturePalette = FindDetailTexturePalette(face);

        foreach (var material in Materials)
        {
            if (material.Equals(materialPalette, mainTexturePalette, detailTexturePalette, face.transparency, face.lightMode))
            {
                return material;
            }
        }

        var materialData = new MaterialData();
        materialData.materialPalette = materialPalette;
        materialData.mainTexturePalette = mainTexturePalette;
        materialData.detailTexturePalette = detailTexturePalette;
        Materials.Add(materialData);

        return materialData;
    }

    protected bool LoadTexture(string filename)
    {
        if (Textures.ContainsKey(filename)) // already loaded, just return
            return true;

        string fullPath = Path.GetDirectoryName(Filename) + '/' + filename;
        if (!File.Exists(fullPath))
        {
            Debug.LogErrorFormat("texture not found, filename {0}", filename);
            return false;
        }

        int _width, _height, _numChannels;
        byte[] rgb = Cognitics.CDB.SiliconGraphicsImage.ReadRGB8(fullPath, out _width, out _height, out _numChannels);

        Color32[] _colors = new Color32[_width * _height];
        int i = 0;
        for (int c = 0; c < _colors.Length; c++, i += _numChannels)
        {
            ref Color32 color = ref _colors[c];
            int index = i + _numChannels - 1;
            if (rgb.Length - 1 < index)
            {
                Debug.LogErrorFormat("rgb array does not contain all required color data! filename {0}", filename);
                break;
            }
            if (_numChannels == 1)
            {
                color.r = rgb[i];
                color.g = rgb[i];
                color.b = rgb[i];
            }
            else
            {
                color.r = rgb[i];
                color.g = rgb[i+1];
                color.b = rgb[i+2];
            }
            color.a = _numChannels < 4 ? (byte)255 : rgb[i+3];
        }

        Textures[filename] = new TextureData {width = _width, height = _height, numChannels = _numChannels, colors = _colors};

        return true;
    }

    #endregion

    #region Palette Helpers

    public MaterialPalette FindMaterialPalette(Face face)
    {
        if (face.materialIndex != -1)
        {
            foreach (var db in fltDBs)
            {
                foreach (var palette in db.MaterialPalettes)
                {
                    if (palette.materialIndex == face.materialIndex)
                        return palette;
                }
            }

            Debug.LogErrorFormat("material palette not found for face! filename {0}", Filename);
        }

        return null;
    }

    public TexturePalette FindMainTexturePalette(Face face)
    {
        if (face.texturePatternIndex != -1)
        {
            foreach (var db in fltDBs)
            {
                foreach (var palette in db.TexturePalettes)
                {
                    if (palette.texturePatternIndex == face.texturePatternIndex)
                        return palette;
                }
            }

            Debug.LogErrorFormat("main texture palette not found for face! filename {0}", Filename);
        }
        return null;
    }

    public TexturePalette FindDetailTexturePalette(Face face)
    {
        if (face.detailTexturePatternIndex != -1)
        {
            foreach (var db in fltDBs)
            {
                foreach (var palette in db.TexturePalettes)
                {
                    if (palette.texturePatternIndex == face.detailTexturePatternIndex)
                        return palette;
                }
            }

            Debug.LogErrorFormat("detail texture palette not found for face!, filename {0}", Filename);
        }

        return null;
    }

    #endregion

    #region Core

    public void Init(string filename, Transform root)//, GameObject userObject)
    {
        Filename = filename;
        Root = root;
        //UserObject = userObject;
    }

    public void SetDB(FltDatabase fltDB)
    {
        fltDBs.Clear();
        fltDBs.Add(fltDB);
    }

    public void AddGameObjConditionally(Record record, Transform parent)
    {
        GameObject gameObj = null;
        Transform transform = null;
        if (record is VertexWithColor)
        {
            return;
        }
        else if (record.RecordType == RecordType.VertexList || record.RecordType == RecordType.PopLevel)
        {
            return;
        }
        else if (record.RecordType == RecordType.PushLevel)
        {
            transform = parent;
            foreach (var child in record.Children)
            {
                AddGameObjConditionally(child, transform);
            }
            return;
        }
        else if (record.RecordType == RecordType.Comment)
        {
            var comment = record as Comment;
            //if (comment.commentStr.StartsWith("DB:Switch name=\"Damage_State\""))
            //    parent.gameObject.SetActive(false);
            return;
        }
        else if (record.RecordType == RecordType.ExternalReference)
        {
            if (record.Children.Count > 0)
            {
                var fltDB = record.Children[0];
                fltDBs.Add(fltDB as FltDatabase);
            }
            else
            {
                Debug.LogErrorFormat("external reference db not found! filename {0}", Filename);
            }
        }
        else if (!(record is IdRecord) && !(record is FltDatabase) && record.RecordType != RecordType.ExternalReference && record.RecordType != RecordType.Face)
        {
            if (Verbose)
            {
                string str = string.Format("Record of type {0} being skipped for gameobj creation has {1} children\n", record.RecordType.ToString(), record.Children.Count);
                if (record.Children.Count != 0)
                {
                    str += "Children skipped:\n";
                    foreach (var child in record.Children)
                        str += child.RecordType.ToString() + "\n";
                }
                Debug.Log(str);
            }
            return;
        }
        else if (record.RecordType == RecordType.Face)
        {
            HandleFace(record as Face);
            return;
        }

        if (record.RecordType != RecordType.Header)
        {
            gameObj = new GameObject(GetName(record));
            gameObj.transform.SetParent(parent, false);
            transform = gameObj.transform;
        }

        if (record is IdRecord)
        {
            if (record.RecordType == RecordType.Group)
            {
                //gameObj = new GameObject(GetName(record));
                //gameObj.transform.SetParent(parent, false);
                //transform = gameObj.transform;

                //record.Children.ForEach(child => { if (child.RecordType == RecordType.Comment && (child as Comment).commentStr.StartsWith("DB:Switch name=\"Damage_State\"")) gameObj.SetActive(false); } );
                //foreach (var child in record.Children)
                //{
                //    if (child.RecordType == RecordType.Comment && (child as Comment).commentStr.StartsWith("DB:Switch name=\"Damage_State\"")) 
                //    {
                //        gameObj.SetActive(false);
                //        break;
                //    }
                //}

                /*var idRecordData = */IdRecords[record as IdRecord] = new IdRecordData(gameObj);
                //if (idRecordData.Vertices.Count != 0)
                //{
                //    Mesh mesh = null;
                //    var filter = gameObj.AddComponent<MeshFilter>();
                //    mesh = filter.mesh;
                //    mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                //    mesh.Clear(true);

                //    var meshRenderer = gameObj.AddComponent<MeshRenderer>();
                //    meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                //    meshRenderer.receiveShadows = false;
                //}
            }
            else if (record.RecordType == RecordType.LevelOfDetail)
            {
                //gameObj = new GameObject(GetName(record));
                //gameObj.transform.SetParent(parent, false);
                //transform = gameObj.transform;

                /*var idRecordData = */IdRecords[record as IdRecord] = new IdRecordData(gameObj);
                //if (idRecordData.Vertices.Count != 0)
                //{
                //    Mesh mesh = null;
                //    var filter = gameObj.AddComponent<MeshFilter>();
                //    mesh = filter.mesh;
                //    mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                //    mesh.Clear(true);

                //    var meshRenderer = gameObj.AddComponent<MeshRenderer>();
                //    meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                //    meshRenderer.receiveShadows = false;
                //}

                var lodRecord = record as LevelOfDetail;
                var comp = gameObj.AddComponent<FltLOD>();
                //comp.UserObject = UserObject;
                comp.SwitchInDistance = (float)lodRecord.switchInDistance;
                comp.SwitchOutDistance = (float)lodRecord.switchOutDistance;
                float vx = -(float)lodRecord.centerX;
                float vy = (float)lodRecord.centerZ;
                float vz = (float)lodRecord.centerY;
                comp.Center = new Vector3(vx, vy, vz);
                comp.SkipUpdate = false;
            }
            else if (record.RecordType != RecordType.Header)
            {
                Debug.LogWarningFormat("unhandled id record type {0}, filename {1}", record.RecordType.ToString(), Filename);
            }
        }

        foreach (var child in record.Children)
        {
            AddGameObjConditionally(child, transform);
        }

        if (record is IdRecord)
        {
            IdRecordData idRecordData = null;
            if (IdRecords.TryGetValue(record as IdRecord, out idRecordData) && idRecordData.Vertices.Count != 0)
            {
                var filter = gameObj.AddComponent<MeshFilter>();
                var mesh = filter.mesh;
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.Clear(true);

                var meshRenderer = gameObj.AddComponent<MeshRenderer>();
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;

                mesh.vertices = idRecordData.Vertices.ToArray();
                mesh.normals = idRecordData.Normals.ToArray();
                mesh.uv = idRecordData.UVs.ToArray();

                Material[] materials = new Material[idRecordData.Submeshes.Count];
                mesh.subMeshCount = idRecordData.Submeshes.Count;
                for (int i = 0; i < idRecordData.Submeshes.Count; i++)
                {
                    var material = idRecordData.Submeshes[i].materialData.CreateMaterial();
                    if (idRecordData.Submeshes[i].materialData.mainTexturePalette != null && 
                        // TODO: move loading of textures
                        LoadTexture(idRecordData.Submeshes[i].materialData.mainTexturePalette.filename))
                    {
                        TextureData textureData = null;
                        if (Textures.TryGetValue(idRecordData.Submeshes[i].materialData.mainTexturePalette.filename, out textureData))
                        {
                            var mainTexture = new Texture2D(textureData.width, textureData.height);
                            mainTexture.SetPixels32(textureData.colors);
                            mainTexture.wrapMode = TextureWrapMode.Clamp;
                            material.mainTexture = mainTexture;
                            mainTexture.Apply();

                            materials[i] = material;
                        }
                    }
                    if (idRecordData.Submeshes[i].materialData.detailTexturePalette != null && 
                        // TODO: move loading of textures
                        LoadTexture(idRecordData.Submeshes[i].materialData.detailTexturePalette.filename))
                    {
                        TextureData textureData = null;
                        if (Textures.TryGetValue(idRecordData.Submeshes[i].materialData.detailTexturePalette.filename, out textureData))
                        {
                            var detailTexture = new Texture2D(textureData.width, textureData.height);
                            detailTexture.SetPixels32(textureData.colors);
                            detailTexture.wrapMode = TextureWrapMode.Clamp;
                            material.SetTexture("_DetailAlbedoMap", detailTexture);//_DETAIL_MULX2
                            detailTexture.Apply();

                            materials[i] = material;
                        }
                    }
                    mesh.SetTriangles(idRecordData.Submeshes[i].triangles, i);
                }

                meshRenderer.materials = materials;
                meshRenderer.sharedMaterials = materials;

                meshRenderer.material.EnableKeyword("_ALPHATEST_ON");//_ALPHABLEND_ON
                meshRenderer.material.EnableKeyword("_MetallicGlossMap");//_SPECGLOSSMAP
                meshRenderer.material.SetFloat("_MetallicGlossMap", 0f);
                meshRenderer.material.EnableKeyword("_Glossiness");
                meshRenderer.material.SetFloat("_Glossiness", 0f);
                meshRenderer.material.EnableKeyword("_SMOOTHNESS");
                meshRenderer.material.SetFloat("_SMOOTHNESS", 0f);
                meshRenderer.material.EnableKeyword("_SpecularHighlights");
                meshRenderer.material.SetFloat("_SpecularHighlights", 0f);
                meshRenderer.material.EnableKeyword("_GlossyReflections");
                meshRenderer.material.SetFloat("_GlossyReflections", 0f);
                meshRenderer.material.EnableKeyword("_Mode");
                meshRenderer.material.SetFloat("_Mode", 1f);

                //meshRenderer.sharedMaterial.EnableKeyword("_ALPHATEST_ON");
                //meshRenderer.sharedMaterial.EnableKeyword("_MetallicGlossMap");
                //meshRenderer.sharedMaterial.SetFloat("_MetallicGlossMap", 0f);
                //meshRenderer.sharedMaterial.EnableKeyword("_Glossiness");
                //meshRenderer.sharedMaterial.SetFloat("_Glossiness", 0f);
                //meshRenderer.sharedMaterial.EnableKeyword("_SMOOTHNESS");
                //meshRenderer.sharedMaterial.SetFloat("_SMOOTHNESS", 0f);
                //meshRenderer.sharedMaterial.EnableKeyword("_SpecularHighlights");
                //meshRenderer.sharedMaterial.SetFloat("_SpecularHighlights", 0f);
                //meshRenderer.sharedMaterial.EnableKeyword("_GlossyReflections");
                //meshRenderer.sharedMaterial.SetFloat("_GlossyReflections", 0f);
                //meshRenderer.sharedMaterial.EnableKeyword("_Mode");
                //meshRenderer.sharedMaterial.SetFloat("_Mode", 1f);

                if (meshRenderer.material.mainTexture is Texture2D)
                    (meshRenderer.material.mainTexture as Texture2D).Apply();
                if (meshRenderer.sharedMaterial.mainTexture is Texture2D)
                    (meshRenderer.sharedMaterial.mainTexture as Texture2D).Apply();

                idRecordData.Bounds = meshRenderer.bounds;
            }
            else
            {
                //Debug.LogError("idRecord not found!");
            }
        }
    }

    public FltDatabase Parse()
    {
        if (!File.Exists(Filename) || Root == null)
        {
            Debug.LogErrorFormat("could not find flt db, filename {0}", Filename);
            return null;
        }

        Stream stream = File.OpenRead(Filename);
        var reader = new FltReader(stream);
        var fltDB = new FltDatabase(null, RecordType.Invalid, reader, Filename);
        fltDBs.Add(fltDB);
        fltDB.Parse();

        return fltDB;
    }

    public void Finish()
    {
        //LoadTextures();

        // Add all game objects we've decided we need
        if (fltDBs.Count > 0)
            AddGameObjConditionally(fltDBs[0], transform);

        // Calculate bounds of this flt
        foreach (var idRecord in IdRecords)
        {
            if (Bounds.size == Vector3.zero)
            {
                Bounds = idRecord.Value.Bounds; // this is to get the initialization right, otherwise we'd have an initial center at the origin, which is not necessarily within bounds of the flt
            }
            else
            {
                Bounds.Encapsulate(idRecord.Value.Bounds); // keep growing the bounds
            }
        }

        // NOTE/TODO: We are skipping the OpenFlight LOD system, at least for now, so force highest LOD
        ForceMaxLod();

        if (missingNormals)
            Debug.LogWarningFormat("one or more vertices from the source flt data do not specify a normal. filename {0}", Filename);
        if (missingUVs)
            Debug.LogWarningFormat("one or more vertices from the source flt data do not specify a UV. filename {0}", Filename);
    }

    protected void ForceMaxLod()
    {
        //FltLOD maxLod = null;
        //float maxVal = float.MinValue;
        //foreach (var elem in IdRecords)
        //{
        //    var idRecordData = elem.Value;
        //    var lod = idRecordData.GameObj.GetComponent<FltLOD>();
        //    if (lod != null && lod.SwitchInDistance > maxVal)
        //    {
        //        maxLod = lod;
        //        maxVal = lod.SwitchInDistance;
        //    }
        //}

        var lods = new List<FltLOD>();
        foreach (var elem in IdRecords)
        {
            var idRecordData = elem.Value;
            if (idRecordData.GameObj != null)
            {
                var lod = idRecordData.GameObj.GetComponent<FltLOD>();
                if (lod != null)
                    lods.Add(lod);
            }
        }
        for (int i = 0; i < lods.Count; i++)
        {
            var lod = lods[i];
            lod.SkipUpdate = true;
            lod.gameObject.SetActive(i == lods.Count - 1);
        }
    }

    #endregion

    #region Misc Helpers

    protected VertexList FindVertexList(Record record)
    {
        var vertexListRecord = record.Children.Find(child => child is VertexList) as VertexList;
        if (vertexListRecord == null)
        {
            foreach (var child in record.Children)
            {
                vertexListRecord = FindVertexList(child);
                if (vertexListRecord != null)
                    break;
            }
        }

        return vertexListRecord;
    }

    protected IdRecord FindParentIdRecord(Record record)
    {
        Record parent = record.Parent;
        if (parent == null)
            return null;

        while (!(parent is IdRecord))
        {
            parent = parent.Parent;
        }

        return parent as IdRecord;
    }

    protected string GetName(Record record)
    {
        string gameObjName = "<unknown>";
        if (record is FltDatabase)
            gameObjName = (record as FltDatabase).Path;
        else if (record is Header)
            gameObjName = "Header";
        else if (record is IdRecord)
            gameObjName = (record as IdRecord).idStr;
        else
            gameObjName = record.RecordType.ToString();

        return gameObjName;
    }

    #endregion

    #region Record Handlers

    protected void HandleFace(Face face)
    {
        // if (face.flags //Flags & 0x4000000 hidden
        //     return;

        var idRecord = FindParentIdRecord(face);
        if (idRecord == null)
        {
            Debug.LogErrorFormat("parent id record not found for face! filename {0}", Filename);
            return;
        }
        IdRecordData idRecordData = null;
        if (!IdRecords.TryGetValue(idRecord, out idRecordData))
        {
            Debug.LogErrorFormat("parent id record data not found for face! filename {0}", Filename);
            return;
        }

        if (!(idRecord.RecordType == RecordType.Group || 
            idRecord.RecordType == RecordType.LevelOfDetail || 
            idRecord.RecordType == RecordType.Header))
        {
            Debug.LogWarningFormat("parent IdRecord of face is type {0}, filename {1}", idRecord.RecordType, Filename);
        }

        var vertexListRecord = FindVertexList(face);
        if (vertexListRecord == null)
        {
            Debug.LogError("missing vertex list for face");
            return;
        }
        if (vertexListRecord.offsets.Count == 3)
        {
            int start = idRecordData.Vertices.Count;
            bool missing = false;
            foreach (int index in vertexListRecord.offsets)
            {
                VertexWithColor vert = null;
                if (!face.Root.VertexPalette.Dict.TryGetValue(index, out vert))
                {
                    missing = true;
                    break;
                }
            }
            if (missing)
            {
                //Debug.LogErrorFormat("one or more vertices not found for face! filename {0}", Filename);
                return;
            }

            foreach (int index in vertexListRecord.offsets)
            {
                VertexWithColor vert = null;
                if (face.Root.VertexPalette.Dict.TryGetValue(index, out vert))
                {
                    // Perform coordinate conversion. Alternatively we could do it during the parse, or set the root's Euler rotation to -90,0,180
                    float vx = -(float)vert.x;
                    float vy = (float)vert.z;
                    float vz = (float)vert.y;
                    Vector3 vec = new Vector3(vx, vy, vz);
                    idRecordData.Vertices.Add(vec);

                    var vertNormal = vert as VertexWithColorNormal;
                    if (vertNormal != null)
                    {
                        float nx = -vertNormal.i;
                        float ny = vertNormal.k;
                        float nz = vertNormal.j;
                        idRecordData.Normals.Add(new Vector3(nx, ny, nz));
                    }
                    else
                    {
                        idRecordData.Normals.Add(Vector3.zero);
                        missingNormals = true;
                    }

                    var vertNormalUv = vert as VertexWithColorNormalUv;
                    if (vertNormalUv != null)
                    {
                        idRecordData.UVs.Add(new Vector2(vertNormalUv.u, vertNormalUv.v));
                    }
                    else
                    {
                        idRecordData.UVs.Add(Vector2.zero);
                        missingUVs = true;
                    }
                }
                else
                {
                    Debug.LogError("vertex offset not found in vertex palette!");
                }
            }
            if (face.drawType == Face.DrawType.DrawSolidNoBackfaceCulling)
            {
                // dupe and flip normal
                idRecordData.Vertices.Add(idRecordData.Vertices[start]);
                idRecordData.Vertices.Add(idRecordData.Vertices[start+1]);
                idRecordData.Vertices.Add(idRecordData.Vertices[start+2]);
                idRecordData.Normals.Add(-idRecordData.Normals[start]);
                idRecordData.Normals.Add(-idRecordData.Normals[start+1]);
                idRecordData.Normals.Add(-idRecordData.Normals[start+2]);
                idRecordData.UVs.Add(idRecordData.UVs[start]);
                idRecordData.UVs.Add(idRecordData.UVs[start+1]);
                idRecordData.UVs.Add(idRecordData.UVs[start+2]);
            }

            int[] triangles = new int[3];
            triangles[0] = start;
            triangles[1] = start+1;
            triangles[2] = start+2;

            var material = GetOrAddMaterial(face);
            var submesh = idRecordData.GetOrAddSubmesh(face, material, triangles);
            submesh.triangles.AddRange(triangles);
            if (face.drawType == Face.DrawType.DrawSolidNoBackfaceCulling)
            {
                int[] flippedTriangles = new int[3];
                flippedTriangles[0] = start+3;
                flippedTriangles[1] = start+5;
                flippedTriangles[2] = start+4;
                submesh.triangles.AddRange(flippedTriangles);
            }
        }
        else
        {
            // TODO: handle triangulation on polygons
            Debug.LogWarningFormat("polygon found with {0} vertices. These are not handled yet.", vertexListRecord.offsets.Count);
        }
    }

    #endregion
}
