
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Cognitics.CoordinateSystems;
using System.Threading;

namespace Cognitics.UnityCDB
{
    public class Tile : MonoBehaviour
    {
        public CDB.Tile CDBTile;
        public GeographicBounds GeographicBounds;
        private List<TileDataCache.Request> requests = new List<TileDataCache.Request>();

        string MaterialName = null;
        Cognitics.Unity.MaterialEntry MaterialEntry;

        Tile ParentTile;

        private bool isActive = false;
        public bool IsActive
        {
            get { return isActive; }
            set
            {
                isActive = value;
                if (isActive)
                    Database.ActiveTiles[GeographicBounds] = this;
                else
                    Database.ActiveTiles.Remove(GeographicBounds);
            }
        }
        public bool IsInitialized = false;
        public bool IsLoading = false;
        public bool IsLoaded = false;
        public bool IsApplying = false;
        public bool IsApplied = false;
        public bool IsDistanceTesting = false;
        public bool IsDistanceTested = false;
        public bool HasElevation = false;
        public bool HasImagery = false;
        public bool ParentElevation = false;
        public bool ParentImagery = false;
        public double cameraDistance = 0.0f;
        public string QuadSN;
        public string QuadWE;

        Cognitics.Unity.WMSDownloadJob DownloadJob = null;

        [HideInInspector] public Vector3[] vertices;
        [HideInInspector] public Vector2[] uv;
        [HideInInspector] public int[] triangles;
        [HideInInspector] public Color32[] pixels;
        [HideInInspector] public Dictionary<CDB.Tile, int[]> trianglesByTile = new Dictionary<CDB.Tile, int[]>();
        [HideInInspector] public Dictionary<CDB.Tile, Color32[]> pixelsByTile = new Dictionary<CDB.Tile, Color32[]>();
        [HideInInspector] public float[] perimeter;
        [HideInInspector] public Vector3 Centroid;

        List<Tile> children = new List<Tile>();

        private MeshRenderer meshRenderer = null;
        private MeshFilter meshFilter = null;

        Shader Shader;

        internal Database Database;

        public int RasterDimension = 2;
        public int MeshDimension = 1;

        float MaxElevation = float.MinValue;
        float MinElevation = float.MaxValue;

        public string Name;

        public int Memory(bool activeOnly)
        {
            int result = 0;
            if (!activeOnly || IsActive)
            {
                result += vertices.Length * sizeof(float) * 3;
                result += uv.Length * sizeof(float) * 2;
                result += triangles.Length * sizeof(int);
                result += pixels.Length * sizeof(byte) * 4;
                foreach (var tile in trianglesByTile.Keys)
                    result += trianglesByTile[tile].Length * sizeof(int);
                foreach (var tile in pixelsByTile.Keys)
                    result += pixelsByTile[tile].Length * sizeof(byte) * 4;
                result += perimeter.Length * sizeof(float);
            }
            children.ForEach(tile => result += tile.Memory(activeOnly));
            return result;
        }

        void Awake()
        {
            meshRenderer = GetComponent<MeshRenderer>();
            meshFilter = GetComponent<MeshFilter>();
        }

        void Start()
        {
            Shader = Shader.Find("Cognitics/TerrainStandard");
            Initialize();
        }

        void Initialize()
        {
            // GetComponentInParent is really "get component in this or recursively upward"
            ParentTile = gameObject.transform.parent.gameObject.GetComponentInParent<Tile>();

            if (vertices.Length > 0)
                return;

            name = CDBTile.Name;
            RasterDimension = (CDBTile.LOD < 0) ? CDBTile.LOD.RasterDimension : CDBTile.LOD.RasterDimension / 2;
            MeshDimension = RasterDimension + 1;
            if (CDBTile.LOD >= 0)
            {
                QuadSN = (GeographicBounds.MaximumCoordinates.Latitude < CDBTile.Bounds.MaximumCoordinates.Latitude) ? "S" : "N";
                QuadWE = (GeographicBounds.MaximumCoordinates.Longitude < CDBTile.Bounds.MaximumCoordinates.Longitude) ? "W" : "E";
                name += " (" + QuadSN + QuadWE + ")";
            }
            Name = name;

            //Debug.LogFormat("[{0}] INITIALIZING", Name);

            IsInitialized = false;
            IsLoaded = false;
            IsApplied = false;

            CDB.LOD imageryLOD = CDBTile.LOD + 1;
            var imageryTiles = Database.DB.Tiles.Generate(GeographicBounds, imageryLOD);
            // we should only be getting a single imagery tile
            foreach (var imageryTile in imageryTiles)
            {
                bool exists = Database.DB.Imagery.YearlyVstiRepresentation.Exists(imageryTile) || Database.DB.Imagery.YearlyVstiRepresentation.AlternateExists(imageryTile);
                if (Database.DB.Imagery.YearlyVstiRepresentation.Exists(imageryTile))
                {
                    MaterialName = Database.DB.Imagery.YearlyVstiRepresentation.Filename(imageryTile);
                    MaterialEntry = Database.TerrainMaterialManager.Entry(MaterialName);
                }
                else
                {
                    // don't try to load imagery if our parent is using the grandparent imagery
                    if ((ParentTile == null) || ParentTile.ParentImagery)
                        continue;
                    if (Database.TileDataCache.OnlineImageryServer != null)
                    {
                        DownloadJob = new Unity.WMSDownloadJob();
                        DownloadJob.OnlineImageryServer = Database.TileDataCache.OnlineImageryServer;
                        DownloadJob.OnlineImageryLayer = Database.TileDataCache.OnlineImageryLayer;
                        DownloadJob.Width = imageryTile.RasterDimension;
                        DownloadJob.Height = imageryTile.RasterDimension;
                        DownloadJob.South = imageryTile.Bounds.MinimumCoordinates.Latitude;
                        DownloadJob.North = imageryTile.Bounds.MaximumCoordinates.Latitude;
                        DownloadJob.West = imageryTile.Bounds.MinimumCoordinates.Longitude;
                        DownloadJob.East = imageryTile.Bounds.MaximumCoordinates.Longitude;
                        DownloadJob.Filename = Database.DB.Imagery.YearlyVstiRepresentation.AlternateFilename(imageryTile);
                        DownloadJob.Schedule();
                    }
                    else
                    {
                        MaterialName = ParentTile.MaterialName;
                        MaterialEntry = Database.TerrainMaterialManager.Entry(MaterialName);
                        ParentImagery = true;
                    }
                }
            }

            Task.Run(() => { TaskInitialize(); });
        }

        private void TaskInitialize()
        {
            try
            {
                GenerateVertices();
                GenerateTriangles();
                GenerateUVs();

                if (Database.DB.Elevation.PrimaryTerrainElevation.Exists(CDBTile) || (Database.TileDataCache.OnlineElevationServer != null))
                {
                    var primaryTerrainElevation = new TileDataCache.Request { Tile = CDBTile, Component = Database.DB.Elevation.PrimaryTerrainElevation };
                    requests.Add(primaryTerrainElevation);
                    Database.TileDataCache.RequestEntry(CDBTile, Database.DB.Elevation.PrimaryTerrainElevation);
                }
                else
                {
                    if(ParentTile != null)
                        ParentElevation = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            IsInitialized = true;
        }

        static void DestroyTerrain(Tile tile)
        {
            tile.meshRenderer.enabled = false;
        }

        public static void DestroyTile(Tile tile)
        {
            if (tile == null)
                return;
            tile.Database.ActiveTiles.Remove(tile.GeographicBounds);
            tile.meshRenderer.material = null;
            DestroyTerrain(tile);
            Destroy(tile.meshFilter.mesh);
            if(tile.MaterialName != null)
                tile.Database.TerrainMaterialManager.Release(tile.MaterialName);
            tile.transform.SetParent(null);
            Destroy(tile.gameObject);
        }

        void Update()
        {
            if (!IsInitialized)
                return;
            if (IsLoading)
                return;
            if (IsApplying)
                return;
            if (!IsLoaded)
            {
                if (DownloadJob != null)
                {
                    if (!DownloadJob.IsCompleted)
                        return;
                    DownloadJob.Complete();
                    if (System.IO.File.Exists(DownloadJob.Filename))
                    {
                        MaterialName = DownloadJob.Filename;
                        MaterialEntry = Database.TerrainMaterialManager.Entry(MaterialName);
                    }
                    else
                    {
                        if (ParentTile != null)
                        {
                            MaterialName = ParentTile.MaterialName;
                            MaterialEntry = Database.TerrainMaterialManager.Entry(MaterialName);
                            ParentImagery = true;
                        }
                    }
                }
                IsLoading = true;
                Task.Run(() => { TaskLoad(); });
                return;
            }
            var childrenApplied = false;
            if (children.Count > 0)
            {
                childrenApplied = true;
                bool fake = true;
                foreach (var child in children)
                {
                    //if (!child.ParentElevation || !child.ParentImagery)
                    if (!child.ParentImagery)
                        fake = false;
                    if (!child.IsApplied || !child.HasElevation || !child.HasImagery)
                    {
                        childrenApplied = false;
                        child.meshRenderer.enabled = false;
                    }
                }
                if (fake)
                    childrenApplied = false;
            }
            if (childrenApplied && IsActive)
            {
                //Debug.LogFormat("[{0}] DEACTIVATING", Name);

                DestroyTerrain(this);

                IsActive = false;
                children.ForEach(child => child.meshRenderer.enabled = true);
                children.ForEach(child => child.IsActive = true);
                Database.UpdateTerrain(GeographicBounds);
                return;
            }
            if (!IsApplied)
            {
                if (Database.TileAppliedTerrain)
                    return;
                Database.TileAppliedTerrain = true;
                HasImagery = (MaterialEntry != null) && MaterialEntry.Loaded && (MaterialEntry.Material != null);
                if (!HasImagery)
                    return;
                IsApplying = true;
                Database.UpdateTerrain(GeographicBounds);
                StartCoroutine(ApplyTerrain());
                float repeatRate = (CDBTile.LOD < 1) ? 1f : CDBTile.LOD;
                InvokeRepeating("UpdateDistance", 0f, repeatRate);
            }
        }

        IEnumerator UpdateCoroutine()
        {
            if (!IsLoaded)
            {
                if (DownloadJob != null)
                {
                    if (!DownloadJob.IsCompleted)
                        yield break;
                    DownloadJob.Complete();
                    if (System.IO.File.Exists(DownloadJob.Filename))
                    {
                        MaterialName = DownloadJob.Filename;
                        MaterialEntry = Database.TerrainMaterialManager.Entry(MaterialName);
                    }
                    else
                    {
                        if (ParentTile != null)
                        {
                            MaterialName = ParentTile.MaterialName;
                            MaterialEntry = Database.TerrainMaterialManager.Entry(MaterialName);
                            ParentImagery = true;
                        }
                    }
                }
                IsLoading = true;
                Task.Run(() => { TaskLoad(); });
                yield break;
            }
            yield return null;
            var childrenApplied = false;
            if (children.Count > 0)
            {
                childrenApplied = true;
                bool fake = true;
                foreach (var child in children)
                {
                    if (!child.ParentElevation || !child.ParentImagery)
                        fake = false;
                    if (!child.IsApplied || !child.HasElevation || !child.HasImagery)
                    {
                        childrenApplied = false;
                        child.meshRenderer.enabled = false;
                    }
                }
                if (fake)
                    childrenApplied = false;
            }
            yield return null;
            if (childrenApplied && IsActive)
            {
                //Debug.LogFormat("[{0}] DEACTIVATING", Name);

                DestroyTerrain(this);

                IsActive = false;
                children.ForEach(child => child.meshRenderer.enabled = true);
                children.ForEach(child => child.IsActive = true);
                Database.UpdateTerrain(GeographicBounds);
                yield break;
            }
            yield return null;
            if (!IsApplied)
            {
                if (Database.TileAppliedTerrain)
                    yield break ;
                Database.TileAppliedTerrain = true;
                HasImagery = (MaterialEntry != null) && MaterialEntry.Loaded && (MaterialEntry.Material != null);
                if (!HasImagery)
                    yield break;
                IsApplying = true;
                Database.UpdateTerrain(GeographicBounds);
                StartCoroutine(ApplyTerrain());
                float repeatRate = (CDBTile.LOD < 1) ? 1f : CDBTile.LOD;
                InvokeRepeating("UpdateDistance", 0f, repeatRate);
            }
        }

        void UpdateDistance()
        {
            bool hasGrandchildren = CheckForGrandchildren();
            if (hasGrandchildren)
                return;

            cameraDistance = Vector3.Distance(Database.lastCameraPosition, Centroid);

            var lodSwitch = Database.LODSwitchByObject[Database];
            var divideDistance = lodSwitch.EntryDistanceForLOD(CDBTile.LOD - Database.PerformanceOffsetLOD);
            var consolidateDistance = lodSwitch.ExitDistanceForLOD(CDBTile.LOD - Database.PerformanceOffsetLOD);
            if (children.Count > 0)
            {
                if (!hasGrandchildren)
                {
                    if (cameraDistance > consolidateDistance)
                        Consolidate();
                }
            }
            else
            {
                bool divide = true;
                if (!meshRenderer.enabled)
                    divide = false;
                if (cameraDistance > divideDistance)
                    divide = false;
                if (Database.SystemMemoryLimitExceeded)
                    divide = false;
                if (!HasImagery)
                    divide = false;
                if (!HasElevation)
                    divide = false;
                if (ParentImagery)
                    divide = false;
                if (divide)
                    StartCoroutine(Divide2());

            }
        }

        private void TaskLoad()
        {
            try
            {
                bool allLoaded = true;
                var requests_copy = new List<TileDataCache.Request>(requests);
                foreach (var request in requests_copy)
                {
                    if (!Database.TileDataCache.IsLoaded(request.Tile, request.Component))
                    {
                        allLoaded = false;
                        continue;
                    }
                    ApplyComponent(request.Tile, request.Component);
                }
                if (allLoaded)
                {
                    IsLoaded = true;
                    foreach (var request in requests_copy)
                        ApplyComponent(request.Tile, request.Component);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            IsLoading = false;
        }

        void GenerateVertices()
        {
            var meshDimension = MeshDimension;
            CartesianBounds cartesianBounds = GeographicBounds.TransformedWith(Database.Projection);
            double spacingX = (cartesianBounds.MaximumCoordinates.X - cartesianBounds.MinimumCoordinates.X) / RasterDimension;
            double spacingY = (cartesianBounds.MaximumCoordinates.Y - cartesianBounds.MinimumCoordinates.Y) / RasterDimension;
            double originX = cartesianBounds.MinimumCoordinates.X;
            double originY = cartesianBounds.MinimumCoordinates.Y;
            vertices = new Vector3[meshDimension * meshDimension];
            int vertexIndex = 0;
            for (int row = 0; row < meshDimension; ++row)
                for (int column = 0; column < meshDimension; ++column, ++vertexIndex)
                {
                    ref Vector3 vertex = ref vertices[vertexIndex];
                    vertex.x = (float)(originX + (column * spacingX));
                    vertex.y = 0f;
                    vertex.z = (float)(originY + (row * spacingY));
                }
        }

        void GenerateTriangles(int meshType = 0)
        {
            triangles = new int[RasterDimension * RasterDimension * 6];
            int triangleIndex = 0;
            for (int row = 0; row < RasterDimension; ++row)
            {
                for (int column = 0; column < RasterDimension; ++column, triangleIndex += 6)
                {
                    int vertexIndex = (row * MeshDimension) + column;

                    int lowerLeftIndex = vertexIndex;
                    int lowerRightIndex = lowerLeftIndex + 1;
                    int upperLeftIndex = lowerLeftIndex + MeshDimension;
                    int upperRightIndex = upperLeftIndex + 1;

                    if (meshType == 0)
                    {
                        triangles[triangleIndex + 0] = lowerLeftIndex;
                        triangles[triangleIndex + 1] = upperLeftIndex;
                        triangles[triangleIndex + 2] = upperRightIndex;

                        triangles[triangleIndex + 3] = lowerLeftIndex;
                        triangles[triangleIndex + 4] = upperRightIndex;
                        triangles[triangleIndex + 5] = lowerRightIndex;
                    }
                    else
                    {
                        triangles[triangleIndex + 0] = upperLeftIndex;
                        triangles[triangleIndex + 1] = lowerRightIndex;
                        triangles[triangleIndex + 2] = lowerLeftIndex;

                        triangles[triangleIndex + 3] = upperLeftIndex;
                        triangles[triangleIndex + 4] = upperRightIndex;
                        triangles[triangleIndex + 5] = lowerRightIndex;
                    }
                }
            }
        }

        void GenerateUVs(bool quad = false, bool north = false, bool east = false)
        {
            uv = new Vector2[vertices.Length];
            int vertexIndex = 0;
            int row_offset = north ? MeshDimension / 2 : 0;
            int column_offset = east ? MeshDimension / 2 : 0;
            for (int row = 0; row < MeshDimension; ++row)
            {
                for (int column = 0; column < MeshDimension; ++column, ++vertexIndex)
                {
                    float xnum = quad ? column_offset + (column / 2) : column;
                    float ynum = quad ? row_offset + (row / 2) : row;
                    ref Vector2 uvv = ref uv[vertexIndex];
                    uvv.x = xnum / RasterDimension;
                    uvv.y = 1.0f - (ynum / RasterDimension);
                }
            }
        }

        private IEnumerator ApplyTerrain()
        {
            IsApplying = true;

            bool isNorthQuad = (ParentTile != null) && ParentImagery ? ParentTile.GeographicBounds.MaximumCoordinates.Latitude == GeographicBounds.MaximumCoordinates.Latitude : false;
            bool isEastQuad = (ParentTile != null) && ParentImagery ? ParentTile.GeographicBounds.MaximumCoordinates.Longitude == GeographicBounds.MaximumCoordinates.Longitude : false;
            GenerateUVs(ParentImagery, isNorthQuad, isEastQuad);

            yield return null;

            if (ParentElevation)
                ApplyParentElevation();

            yield return null;

            meshRenderer.material = MaterialEntry.Material;
            (meshRenderer.sharedMaterial.mainTexture as Texture2D).wrapMode = TextureWrapMode.Clamp;

            yield return null;

            var mesh = meshFilter.mesh;
            mesh.Clear();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            yield return null;

            mesh.vertices = vertices;

            yield return null;

            mesh.uv = uv;

            yield return null;

            mesh.triangles = triangles;

            yield return null;

            mesh.RecalculateNormals();

            yield return null;

            IsApplying = false;
            IsApplied = true;
        }

        void ApplyParentElevation()
        {
            HasElevation = true;
            var parent_bounds = ParentTile.CDBTile.Bounds;
            int parent_z_offset = (QuadSN == "S") ? 0 : MeshDimension / 2;
            int parent_x_offset = (QuadWE == "W") ? 0 : MeshDimension / 2;
            //Debug.LogFormat("[{0}] GENERATING ELEVATION ({1},{2})", Name, parent_x_offset, parent_z_offset);
            var data = new float[MeshDimension * MeshDimension];
            int i = 0;
            for (int z = 0; z < MeshDimension; ++z)
            {
                for (int x = 0; x < MeshDimension; ++x, ++i)
                {
                    int parent_x0 = parent_x_offset + (x / 2);
                    int parent_z0 = parent_z_offset + (z / 2);
                    int parent_x1 = (x < MeshDimension) ? parent_x_offset + ((x + 1) / 2) : parent_x0;
                    int parent_z1 = (z < MeshDimension) ? parent_z_offset + ((z + 1) / 2) : parent_z0;
                    int parent_i00 = (parent_z0 * ParentTile.MeshDimension) + parent_x0;
                    int parent_i01 = (parent_z0 * ParentTile.MeshDimension) + parent_x1;
                    int parent_i10 = (parent_z1 * ParentTile.MeshDimension) + parent_x0;
                    int parent_i11 = (parent_z1 * ParentTile.MeshDimension) + parent_x1;
                    float y00 = ParentTile.vertices[parent_i00].y;
                    float y01 = ParentTile.vertices[parent_i01].y;
                    float y10 = ParentTile.vertices[parent_i10].y;
                    float y11 = ParentTile.vertices[parent_i11].y;
                    float y = (y00 + y01 + y10 + y11) / 4;
                    vertices[i].y = y;
                    if (y > MaxElevation)
                        MaxElevation = y;
                    if (y < MinElevation)
                        MinElevation = y;
                }
            }
            float xMin, xMax, zMin, zMax;
            xMin = vertices[0].x;
            zMin = vertices[0].z;
            xMax = vertices[(MeshDimension * MeshDimension) - 1].x;
            zMax = vertices[(MeshDimension * MeshDimension) - 1].z;
            Centroid = new Vector3((xMin + xMax) / 2, (MinElevation + MaxElevation) / 2, (zMin + zMax) / 2);
            perimeter = Database.GetMeshPerimeterY(vertices, MeshDimension);
        }

        private bool CheckForGrandchildren()
        {
            foreach (var child in children)
            {
                if (child.children.Count > 0)
                    return true;
            }
            return false;
        }

        public void Consolidate()
        {
            //Debug.LogFormat("[{0}] CONSOLIDATE", Name);
            ApplyTerrain();
            meshRenderer.enabled = true;
            IsActive = true;
            children.ForEach(child => DestroyTile(child));
            children.Clear();
            Database.UpdateTerrain(GeographicBounds);
        }

        public IEnumerator Divide2()
        {
            List<CDB.Tile> cdbTiles = new List<CDB.Tile>();
            Thread t = new Thread(() => { cdbTiles = Database.DB.Tiles.Generate(GeographicBounds, CDBTile.LOD + 1); });
            t.Start();
            //Debug.LogFormat("[{0}] DIVIDE", Name);
            //var cdbTiles = Database.DB.Tiles.Generate(GeographicBounds, CDBTile.LOD + 1);
            while (t.ThreadState == ThreadState.Running)
                yield return null;
            cdbTiles.ForEach(cdbTile => children.AddRange(Database.GenerateTiles(cdbTile)));
            children.ForEach(child => child.transform.SetParent(transform));
            children.ForEach(child => child.meshRenderer.enabled = false);
            children.ForEach(child => child.Initialize());
        }

        public void ApplyComponent(CDB.Tile cdbTile, CDB.Component component)
        {
            if (cdbTile.Bounds.MinimumCoordinates.Latitude >= GeographicBounds.MaximumCoordinates.Latitude)
                return;
            if (cdbTile.Bounds.MinimumCoordinates.Longitude >= GeographicBounds.MaximumCoordinates.Longitude)
                return;
            if (cdbTile.Bounds.MaximumCoordinates.Latitude <= GeographicBounds.MinimumCoordinates.Latitude)
                return;
            if (cdbTile.Bounds.MaximumCoordinates.Longitude <= GeographicBounds.MinimumCoordinates.Longitude)
                return;

            if (children.Count > 0)
                children.ForEach(child => child.ApplyComponent(cdbTile, component));

            if (!requests.Contains(new TileDataCache.Request { Tile = cdbTile, Component = component }))
                return;

            if (component == Database.DB.Elevation.PrimaryTerrainElevation)
                ApplyPrimaryTerrainElevation(cdbTile);
            if (component == Database.DB.Imagery.YearlyVstiRepresentation)
                ApplyYearlyVstiRepresentation(cdbTile);

            if (children.Count > 0)
                return;
        }

        void ApplyPrimaryTerrainElevation(CDB.Tile cdbTile)
        {
            var primaryTerrainElevation = Database.TileDataCache.GetEntry<float[]>(cdbTile, Database.DB.Elevation.PrimaryTerrainElevation);
            if (primaryTerrainElevation == null)
                return;
            if (primaryTerrainElevation.data == null)
                return;
            HasElevation = true;

            Vector3 origin = vertices[0];
            CartesianBounds cdbTileCartesianBounds = cdbTile.Bounds.TransformedWith(Database.Projection);
            Vector3 cdborigin = new Vector3((float)cdbTileCartesianBounds.MinimumCoordinates.X, 0, (float)cdbTileCartesianBounds.MinimumCoordinates.Y);
            Vector3 tileOffset = origin - cdborigin;

            float xStep = Math.Abs(vertices[1].x - vertices[0].x);
            float zStep = Math.Abs(vertices[MeshDimension].z - vertices[0].z);

            int xIndexOffset = (int)(tileOffset.x / xStep);
            int zIndexOffset = (int)(tileOffset.z / zStep);
            if (xIndexOffset > 0)
                xIndexOffset = 512;
            if (zIndexOffset > 0)
                zIndexOffset = 512;

            int xStop = xIndexOffset + MeshDimension - 1;
            int zStop = zIndexOffset + MeshDimension - 1;

            int vertexIndex = 0;

            var meshDimension = cdbTile.MeshDimension;
            for (int z = zIndexOffset; z <= zStop; ++z)
                for (int x = xIndexOffset; x <= xStop; ++x, ++vertexIndex)
                    vertices[vertexIndex].y = primaryTerrainElevation.data[z * meshDimension + x] * (float)Database.Projection.Scale;

            for (int i = 0; i < vertices.Length; ++i)
            {
                if (vertices[i].y > MaxElevation)
                    MaxElevation = vertices[i].y;

                if (vertices[i].y < MinElevation)
                    MinElevation = vertices[i].y;
            }
            float xMin, xMax, zMin, zMax;
            xMin = vertices[0].x;
            zMin = vertices[0].z;
            if(meshDimension < 1024)
            {
                xMax = vertices[meshDimension - 1].x;
                zMax = vertices[(meshDimension * meshDimension) - 1].z;
            }
            else
            {
                xMax = vertices[(meshDimension / 2) - 1].x;
                zMax = vertices[((meshDimension / 2) * (meshDimension / 2)) - 1].z;
            }
            Centroid = new Vector3((xMin + xMax) / 2, (MinElevation + MaxElevation) / 2, (zMin + zMax) / 2);
            perimeter = Database.GetMeshPerimeterY(vertices, MeshDimension);
        }

        void ApplyYearlyVstiRepresentation(CDB.Tile cdbTile)
        {
            var yearlyVstiRepresentation = Database.TileDataCache.GetEntry<byte[]>(cdbTile, Database.DB.Imagery.YearlyVstiRepresentation);
            if (yearlyVstiRepresentation == null)
                return;
            if (yearlyVstiRepresentation.data == null)
                return;
            HasImagery = true;
            var tileCartesianBounds = GeographicBounds.TransformedWith(Database.Projection);
            var dataCartesianBounds = cdbTile.Bounds.TransformedWith(Database.Projection);
            var bounds = new CartesianBounds();
            bounds.MinimumCoordinates.X = Math.Max(dataCartesianBounds.MinimumCoordinates.X, tileCartesianBounds.MinimumCoordinates.X);
            bounds.MinimumCoordinates.Y = Math.Max(dataCartesianBounds.MinimumCoordinates.Y, tileCartesianBounds.MinimumCoordinates.Y);
            bounds.MaximumCoordinates.X = Math.Min(dataCartesianBounds.MaximumCoordinates.X, tileCartesianBounds.MaximumCoordinates.X);
            bounds.MaximumCoordinates.Y = Math.Min(dataCartesianBounds.MaximumCoordinates.Y, tileCartesianBounds.MaximumCoordinates.Y);
            double boundsWidth = bounds.MaximumCoordinates.X - bounds.MinimumCoordinates.X;
            double boundsHeight = bounds.MaximumCoordinates.Y - bounds.MinimumCoordinates.Y;

            // copy triangles and assign UVs
            {
                float stepX = (float)(tileCartesianBounds.MaximumCoordinates.X - tileCartesianBounds.MinimumCoordinates.X) / RasterDimension;
                float stepY = (float)(tileCartesianBounds.MaximumCoordinates.Y - tileCartesianBounds.MinimumCoordinates.Y) / RasterDimension;
                double offsetX = bounds.MinimumCoordinates.X - tileCartesianBounds.MinimumCoordinates.X;
                double offsetY = bounds.MinimumCoordinates.Y - tileCartesianBounds.MinimumCoordinates.Y;
                int startColumn = (int)Math.Round(offsetX / stepX);
                int startRow = (int)Math.Round(offsetY / stepY);
                int width = (int)Math.Round(boundsWidth / stepX);
                int height = (int)Math.Round(boundsHeight / stepY);

                var submeshTriangles = new int[width * height * 6];
                int index = 0;
                for (int row = 0; row < width; ++row)
                {
                    for (int column = 0; column < height; ++column)
                    {
                        int vertexIndex = ((startRow + row) * MeshDimension) + startColumn + column;
                        uv[vertexIndex] = new Vector2((float)column / height, (float)row / width);

                        int triangleIndex = (((startRow + row) * RasterDimension) + startColumn + column) * 6;
                        for (int i = 0; i < 6; ++i, ++index)
                            submeshTriangles[index] = triangles[triangleIndex + i];
                    }
                }
                trianglesByTile[cdbTile] = submeshTriangles;
            }

            // extract pixels
            {
                float stepX = (float)(dataCartesianBounds.MaximumCoordinates.X - dataCartesianBounds.MinimumCoordinates.X) / cdbTile.RasterDimension;
                float stepY = (float)(dataCartesianBounds.MaximumCoordinates.Y - dataCartesianBounds.MinimumCoordinates.Y) / cdbTile.RasterDimension;
                double offsetX = bounds.MinimumCoordinates.X - dataCartesianBounds.MinimumCoordinates.X;
                double offsetY = bounds.MinimumCoordinates.Y - dataCartesianBounds.MinimumCoordinates.Y;
                int startColumn = (int)Math.Round(offsetX / stepX);
                int startRow = (int)Math.Round(offsetY / stepY);
                int width = (int)Math.Round(boundsWidth / stepX);
                int height = (int)Math.Round(boundsHeight / stepY);

                var pixels = new Color32[width * height];
                int pixelIndex = 0;
                for (int row = 0; row < width; ++row)
                {
                    for (int column = 0; column < height; ++column, ++pixelIndex)
                    {
                        int imageryIndex = (((startRow + row) * cdbTile.RasterDimension) + startColumn + column) * 3;
                        byte r = yearlyVstiRepresentation.data[imageryIndex + 0];
                        byte g = yearlyVstiRepresentation.data[imageryIndex + 1];
                        byte b = yearlyVstiRepresentation.data[imageryIndex + 2];
                        byte a = 255;
                        pixels[pixelIndex] = new Color32(r, g, b, a);
                    }
                }
                pixelsByTile[cdbTile] = pixels;
            }
        }

        private void CullOutOfView()
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            if (!GeometryUtility.TestPlanesAABB(planes, meshRenderer.bounds))
            {
                meshRenderer.enabled = false;
            }
            else
            {
                meshRenderer.enabled = true;
            }
        }

    }

}
