
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
        public double cameraDistance = 0.0f;

        [HideInInspector] public Vector3[] vertices;
        //[HideInInspector] public Vector3[] normals;
        [HideInInspector] public Vector2[] uv;
        [HideInInspector] public int[] triangles;
        [HideInInspector] public Color32[] pixels;
        [HideInInspector] public Dictionary<CDB.Tile, int[]> trianglesByTile = new Dictionary<CDB.Tile, int[]>();
        [HideInInspector] public Dictionary<CDB.Tile, Color32[]> pixelsByTile = new Dictionary<CDB.Tile, Color32[]>();
        [HideInInspector] public float[] perimeter;

        List<Tile> children = new List<Tile>();

        private MeshRenderer meshRenderer = null;
        private MeshFilter meshFilter = null;

        private float lastDistanceTest = 0f;

        Shader Shader;

        internal Database Database;

        public int RasterDimension = 2;
        public int MeshDimension = 1;

        float MaxElevation = float.MinValue;

        [HideInInspector] public CancellationTokenSource TaskInitializeToken = new CancellationTokenSource();
        [HideInInspector] public CancellationTokenSource TaskDistanceToken = new CancellationTokenSource();
        [HideInInspector] public CancellationTokenSource TaskLoadToken = new CancellationTokenSource();

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
                name += " (";
                name += (GeographicBounds.MaximumCoordinates.Latitude < CDBTile.Bounds.MaximumCoordinates.Latitude) ? "S" : "N";
                name += (GeographicBounds.MaximumCoordinates.Longitude < CDBTile.Bounds.MaximumCoordinates.Longitude) ? "W" : "E";
                name += ")";
            }
            Name = name;

            //Debug.LogFormat("[{0}] INITIALIZING", Name);

            IsInitialized = false;
            IsLoaded = false;
            IsApplied = false;

            var token = TaskInitializeToken.Token;
            Task.Run(() => { TaskInitialize(); }, token);
        }

        private void TaskInitialize()
        {
            GenerateVertices();
            GenerateTriangles();
            GenerateUVs();
            //GenerateNormals();

            // TODO: handle geopackage
            if (Database.GPKG != null)
                return;


            var primaryTerrainElevation = new TileDataCache.Request { Tile = CDBTile, Component = Database.DB.Elevation.PrimaryTerrainElevation };
            requests.Add(primaryTerrainElevation);
            Database.TileDataCache.RequestEntry(CDBTile, Database.DB.Elevation.PrimaryTerrainElevation);

            CDB.LOD imageryLOD = CDBTile.LOD + 1;
            var imageryTiles = Database.DB.Tiles.Generate(GeographicBounds, imageryLOD);
            foreach (var imageryTile in imageryTiles)
            {
                var yearlyVstiRepresentation = new TileDataCache.Request { Tile = imageryTile, Component = Database.DB.Imagery.YearlyVstiRepresentation };
                requests.Add(yearlyVstiRepresentation);
                Database.TileDataCache.RequestEntry(imageryTile, Database.DB.Imagery.YearlyVstiRepresentation);
            }

            //ApplyComponent(CDBTile, Database.DB.Elevation.PrimaryTerrainElevation);
            //ApplyComponent(CDBTile, Database.DB.Imagery.YearlyVstiRepresentation);

            IsInitialized = true;
        }

        static void DestroyTerrain(Tile tile)
        {
            tile.meshFilter.mesh.Clear();
            tile.meshRenderer.enabled = false;
            var materials = tile.meshRenderer.materials;
            for (int i = 0; i < materials.Length; ++i)
            {
                Destroy(materials[i].mainTexture);
                Destroy(materials[i]);
            }
            tile.meshRenderer.materials = new Material[0];
        }

        public static void DestroyTile(Tile tile)
        {
            tile.TaskInitializeToken.Cancel();
            tile.TaskInitializeToken.Dispose();
            tile.TaskLoadToken.Cancel();
            tile.TaskLoadToken.Dispose();
            tile.TaskDistanceToken.Cancel();
            tile.TaskDistanceToken.Dispose();

            DestroyTerrain(tile);
            Destroy(tile.meshFilter.mesh);
            for (int i = 0; i < tile.meshRenderer.materials.Length; ++i)
                Destroy(tile.meshRenderer.materials[i]);

            tile.transform.SetParent(null);
            Destroy(tile.gameObject);

            tile.Database.ActiveTiles.Remove(tile.GeographicBounds);
        }

        void Update()
        {
            if (IsActive)
                CullOutOfView();
            if (!IsInitialized)
                return;
            if (IsLoading)
                return;
            if (IsApplying)
                return;
            if (!IsLoaded)
            {
                IsLoading = true;
                var token = TaskLoadToken.Token;
                Task.Run(() => { TaskLoad(); }, token);
                return;
            }
            var childrenApplied = false;
            if (children.Count > 0)
            {
                childrenApplied = true;
                foreach (var child in children)
                {
                    if (!child.IsApplied || !child.HasElevation || !child.HasImagery)
                    //if (!child.IsApplied)
                    {
                        childrenApplied = false;
                        child.meshRenderer.enabled = false;
                    }
                }
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
                Database.UpdateTerrain(GeographicBounds);
                ApplyTerrain();
                return;
            }

            if (CheckForGrandchildren())
                return;

            if (IsDistanceTested)
            {
                var lodSwitch = Database.LODSwitchByObject[Database];
                var divideDistance = lodSwitch.EntryDistanceForLOD(CDBTile.LOD - Database.PerformanceOffsetLOD);
                var consolidateDistance = lodSwitch.ExitDistanceForLOD(CDBTile.LOD - Database.PerformanceOffsetLOD);
                if (children.Count > 0)
                {
                    if (!CheckForGrandchildren())
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
                    if(divide)
                        Divide();
                }
                IsDistanceTested = false;
            }

            if (IsDistanceTesting)
                return;
            if (Time.time - lastDistanceTest < 1f)
                return;
            lastDistanceTest = Time.time;
            IsDistanceTesting = true;
            IsDistanceTested = false;

            var distanceToken = TaskDistanceToken.Token;
            Task.Run(() => { TaskDistanceTest(); }, distanceToken);
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

        private void TaskDistanceTest()
        {
            try
            {
                cameraDistance = Database.DistanceForBounds(GeographicBounds);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            IsDistanceTested = true;
            IsDistanceTesting = false;
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
                    //vertices[vertexIndex] = new Vector3((float)(originX + (column * spacingX)), 0.0f, (float)(originY + (row * spacingY)));
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

        void GenerateUVs()
        {
            uv = new Vector2[vertices.Length];
            int vertexIndex = 0;
            for (int row = 0; row < MeshDimension; ++row)
            {
                for (int column = 0; column < MeshDimension; ++column, ++vertexIndex)
                {
                    ref Vector2 uvv = ref uv[vertexIndex];
                    uvv.x = (float)column / RasterDimension;
                    uvv.y = (float)row / RasterDimension;
                }
            }
        }

        void GenerateNormals()
        {
            /*
            normals = new Vector3[vertices.Length];
            for (int i = 0; i < normals.Length; ++i)
                normals[i] = Vector3.up;
            */
        }

        private void ApplyTerrain()
        {
            if (!HasImagery)
                return;

            IsApplying = true;

            var materials = new Material[0];
            if (pixelsByTile.Count > 0)
            {
                materials = new Material[pixelsByTile.Count];
                int matIndex = 0;
                foreach (var cdbTile in pixelsByTile.Keys)
                {
                    var pixels = pixelsByTile[cdbTile];
                    int textureDimension = (int)Math.Sqrt(pixels.Length);
                    var texture = new Texture2D(textureDimension, textureDimension, TextureFormat.RGBA32, true);
                    texture.SetPixels32(pixels);
                    //var data = texture.GetRawTextureData<Color32>();
                    //data.CopyFrom(pixels);
                    texture.wrapMode = TextureWrapMode.Clamp;
                    materials[matIndex] = new Material(Shader);
                    materials[matIndex].mainTexture = texture;
                    texture.Apply(true, true);

                    ++matIndex;
                }
            }

            meshRenderer.materials = materials;
            //Destroy(meshFilter.mesh);
            //meshFilter.mesh = new Mesh();
            var mesh = meshFilter.mesh;
            mesh.Clear();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.subMeshCount = pixelsByTile.Count;
            int materialIndex = 0;
            foreach (var cdbTile in trianglesByTile.Keys)
            {
                var subtriangles = trianglesByTile[cdbTile];
                mesh.SetTriangles(subtriangles, materialIndex);
                ++materialIndex;
            }

            mesh.RecalculateNormals();
            //mesh.UploadMeshData(true);

            IsApplying = false;
            IsApplied = true;
        }

        private double GetDistanceToNearestChild(Vector3 position)
        {
            double nearestChildDistance = double.MaxValue;
            for (int child = 0; child < children.Count; ++child)
            {
                double childDistance = children[child].GetDistance(position);
                if (childDistance < nearestChildDistance)
                    nearestChildDistance = childDistance;
            }
            return nearestChildDistance;
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

        private double GetDistance(Vector3 position)
        {
            if (!IsInitialized)
                return double.MaxValue;
            NearestVertex nearestVertexCalculator = new NearestVertex();
            int nearestVertexIndex = nearestVertexCalculator.GetNearestVertexIndex(position, vertices);
            Vector3 nearestVertex = vertices[nearestVertexIndex];
            //DrawLine(position, nearestVertex);
            double nearestVertexDistance = Vector3.Distance(nearestVertex, position);
            return nearestVertexDistance;
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

        public void Divide()
        {
            //Debug.LogFormat("[{0}] DIVIDE", Name);
            var cdbTiles = Database.DB.Tiles.Generate(GeographicBounds, CDBTile.LOD + 1);
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

            // disable elevation expansion until some bugs are worked out
            if (primaryTerrainElevation.data == null)
                return;

            if ((ParentTile != null) && (primaryTerrainElevation.data == null))
            {
                //Debug.LogFormat("[{0}] GENERATING ELEVATION", Name);
                var parent_bounds = ParentTile.CDBTile.Bounds;
                int parent_x_offset = (CDBTile.Bounds.MinimumCoordinates.Longitude == parent_bounds.MinimumCoordinates.Longitude) ? 0 : RasterDimension / 2;
                int parent_z_offset = (CDBTile.Bounds.MinimumCoordinates.Latitude == parent_bounds.MinimumCoordinates.Latitude) ? 0 : RasterDimension / 2;
                var data = new float[MeshDimension * MeshDimension];
                int i = 0;
                for (int z = 0; z < MeshDimension; ++z)
                {
                    for (int x = 0; x < MeshDimension; ++x, ++i)
                    {
                        int parent_x = parent_x_offset + (x / 2);
                        int parent_z = parent_z_offset + (z / 2);
                        int parent_i = (parent_z * ParentTile.MeshDimension) + parent_x;
                        int local_i = (z * ParentTile.MeshDimension) + x;
                        vertices[local_i].Set(vertices[local_i].x, ParentTile.vertices[parent_i].y, vertices[local_i].z);
                        if (vertices[i].y > MaxElevation)
                            MaxElevation = vertices[i].y;
                    }
                }
                perimeter = Database.GetMeshPerimeterY(vertices, MeshDimension);
                return;
            }

            HasElevation = true;

            Vector3 origin = vertices[0];
            CartesianBounds cdbTileCartesianBounds = cdbTile.Bounds.TransformedWith(Database.Projection);
            Vector3 cdborigin = new Vector3((float)cdbTileCartesianBounds.MinimumCoordinates.X, 0, (float)cdbTileCartesianBounds.MinimumCoordinates.Y);
            Vector3 tileOffset = origin - cdborigin;

            float xStep = Math.Abs(vertices[1].x - vertices[0].x);
            float zStep = Math.Abs(vertices[MeshDimension].z - vertices[0].z);

            int xIndexOffset = (int)(tileOffset.x / xStep);
            int zIndexOffset = (int)(tileOffset.z / zStep);

            int xStop = xIndexOffset + MeshDimension - 1;
            int zStop = zIndexOffset + MeshDimension - 1;

            int vertexIndex = 0;

            var meshDimension = cdbTile.MeshDimension;
            for (int z = zIndexOffset; z <= zStop; ++z)
                for (int x = xIndexOffset; x <= xStop; ++x, ++vertexIndex)
                    vertices[vertexIndex].y = primaryTerrainElevation.data[z * meshDimension + x] * (float)Database.Projection.Scale;

            for (int i = 0; i < vertices.Length; ++i)
                if (vertices[i].y > MaxElevation)
                    MaxElevation = vertices[i].y;

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
            return;
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
