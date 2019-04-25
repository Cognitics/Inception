
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Cognitics.CoordinateSystems;

namespace Cognitics.UnityCDB
{
    public class Tile : MonoBehaviour
    {
        public CDB.Tile CDBTile;
        public GeographicBounds GeographicBounds;
        private List<TileDataCache.Request> requests = new List<TileDataCache.Request>();

        public bool IsActive = false;
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

        private DateTime lastDistanceTest = DateTime.MinValue;

        Shader Shader;

        internal Database Database;

        public int RasterDimension => (CDBTile.LOD < 0) ? CDBTile.LOD.RasterDimension : CDBTile.LOD.RasterDimension / 2;
        public int MeshDimension => RasterDimension + 1;

        float MaxElevation = float.MinValue;

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

        void Start()
        {
            Shader = Shader.Find("Cognitics/TerrainStandard");
            Initialize();
        }

        void Initialize()
        {
            if (vertices.Length > 0)
                return;
            name = CDBTile.Name;
            if (CDBTile.LOD >= 0)
            {
                name += " (";
                name += (GeographicBounds.MaximumCoordinates.Latitude < CDBTile.Bounds.MaximumCoordinates.Latitude) ? "S" : "N";
                name += (GeographicBounds.MaximumCoordinates.Longitude < CDBTile.Bounds.MaximumCoordinates.Longitude) ? "W" : "E";
                name += ")";
            }

            IsInitialized = false;
            IsLoaded = false;
            IsApplied = false;
            Task.Run(() => TaskInitialize());
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

        void DestroyTerrain(Tile tile)
        {
            tile.GetComponent<MeshFilter>().mesh.Clear();
            var meshRenderer = tile.GetComponent<MeshRenderer>();
            meshRenderer.enabled = false;
            var materials = meshRenderer.materials;
            for (int i = 0; i < materials.Length; ++i)
            {
                Destroy(materials[i].mainTexture);
                Destroy(materials[i]);
            }
            meshRenderer.materials = new Material[0];
        }

        public void DestroyTile(Tile tile)
        {
            children.ForEach(child => child.DestroyTile(child));
            DestroyTerrain(tile);
            Destroy(tile.GetComponent<MeshFilter>().mesh);
            var meshRenderer = tile.GetComponent<MeshRenderer>();
            for (int i = 0; i < meshRenderer.materials.Length; ++i)
                Destroy(meshRenderer.materials[i]);
            tile.transform.SetParent(null);
            Destroy(tile.gameObject);
        }

        void Update()
        {
            if(IsActive)
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
                Task.Run(() => TaskLoad());
                return;
            }
            var childrenApplied = false;
            if (children.Count > 0)
            {
                childrenApplied = true;
                foreach (var child in children)
                {
                    if (!child.IsApplied || !child.HasElevation || !child.HasImagery)
                    {
                        childrenApplied = false;
                        child.GetComponent<MeshRenderer>().enabled = false;
                    }
                }
            }
            if (childrenApplied && IsActive)
            {
                DestroyTerrain(this);
                
                IsActive = false;
                children.ForEach(child => child.GetComponent<MeshRenderer>().enabled = true);
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
                var bracket = Database.LODBrackets[CDBTile.LOD - Database.PerformanceOffsetLOD];
                if (children.Count > 0)
                {
                    if (!CheckForGrandchildren())
                    {
                        if (cameraDistance > bracket.Item2)
                            Consolidate();
                    }
                }
                else
                {
                    if (GetComponent<MeshRenderer>().enabled && (cameraDistance < bracket.Item1))
                        Divide();
                }
                IsDistanceTested = false;
            }

            if (IsDistanceTesting)
                return;
            if ((DateTime.Now - lastDistanceTest).TotalSeconds < 1)
                return;
            lastDistanceTest = DateTime.Now;
            IsDistanceTesting = true;
            IsDistanceTested = false;
            Task.Run(() => TaskDistanceTest());
        }

        private void TaskLoad()
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
                IsLoaded = true;
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
            CartesianBounds cartesianBounds = GeographicBounds.TransformedWith(Database.Projection);
            double spacingX = (cartesianBounds.MaximumCoordinates.X - cartesianBounds.MinimumCoordinates.X) / RasterDimension;
            double spacingY = (cartesianBounds.MaximumCoordinates.Y - cartesianBounds.MinimumCoordinates.Y) / RasterDimension;
            double originX = cartesianBounds.MinimumCoordinates.X;
            double originY = cartesianBounds.MinimumCoordinates.Y;
            vertices = new Vector3[MeshDimension * MeshDimension];
            int vertexIndex = 0;
            for (int row = 0; row < MeshDimension; ++row)
                for (int column = 0; column < MeshDimension; ++column, ++vertexIndex)
                    vertices[vertexIndex] = new Vector3((float)(originX + (column * spacingX)), 0.0f, (float)(originY + (row * spacingY)));
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
                for (int column = 0; column < MeshDimension; ++column, ++vertexIndex)
                    uv[vertexIndex] = new Vector2((float)column / RasterDimension, (float)row / RasterDimension);
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
                    var texture = new Texture2D(textureDimension, textureDimension, TextureFormat.RGBA32, false);
                    var data = texture.GetRawTextureData<Color32>();
                    data.CopyFrom(pixels);
                    texture.wrapMode = TextureWrapMode.Clamp;
                    materials[matIndex] = new Material(Shader);
                    materials[matIndex].mainTexture = texture;
                    texture.Apply(true, true);

                    ++matIndex;
                }
            }

            var meshRenderer = GetComponent<MeshRenderer>();
            var meshFilter = GetComponent<MeshFilter>();

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

        public List<Tile> ActiveTiles()
        {
            var result = new List<Tile>();
            if (IsActive)
                result.Add(this);
            children.ForEach(tile => result.AddRange(tile.ActiveTiles()));
            return result;
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
            bool hasGrandchildren = false;
            children.ForEach(child =>
            {
                if (child.children.Count > 0)
                    hasGrandchildren = true;
            });
            return hasGrandchildren;
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
            //Debug.LogFormat("[TILE] CONSOLIDATE {0} ({1} > {2})", name, cameraDistance, Database.LODBrackets[CDBTile.LOD - Database.PerformanceOffsetLOD].Item2);
            ApplyTerrain();
            GetComponent<MeshRenderer>().enabled = true;
            IsActive = true;
            foreach (var child in children)
                DestroyTile(child);
            children.Clear();
            Database.UpdateTerrain(GeographicBounds);
        }

        private void RegenerateElevationFromChildren()
        {
            for (int child = 0; child < children.Count; ++child)
            {
                int offset = 0;
                SetParentBounds(child, ref offset);

                for (int row = 0; row < MeshDimension; row += 2)
                    for (int col = 0; col < MeshDimension; col += 2)
                        vertices[(int)((row * 0.5) * MeshDimension + (col * 0.5)) + offset].y = children[child].vertices[row * MeshDimension + col].y;
            }
        }

        private void SetParentBounds(int child, ref int offset)
        {
            // The case of 4 children

            if (child == 0)
            {
                offset = 0;
                return;
            }

            int halfway = (int)((MeshDimension - 1) * 0.5);

            if (child == 1)
                offset = halfway;
            else if (child == 2)
                offset = MeshDimension * halfway;
            else if (child == 3)
                offset = halfway * (MeshDimension + 1);
        }

        public void Divide()
        {
            //Debug.LogFormat("[TILE] DIVIDE {0} ({1} < {2})", name, cameraDistance, Database.LODBrackets[CDBTile.LOD - Database.PerformanceOffsetLOD].Item1);
            var cdbTiles = Database.DB.Tiles.Generate(GeographicBounds, CDBTile.LOD + 1);
            cdbTiles.ForEach(cdbTile => children.AddRange(Database.GenerateTiles(cdbTile)));
            children.ForEach(child => child.transform.SetParent(transform));
            children.ForEach(child => child.GetComponent<MeshRenderer>().enabled = false);
            children.ForEach(child => child.Initialize());
        }

        private void InterpolateChildren()
        {
            int halfway = (int)((MeshDimension - 1) * 0.5);
            int index = 0;


            /** ORIGINS **/
            if (children.Count == 4)
            {
                children[0].vertices[0] = vertices[index];
                children[1].vertices[0] = vertices[index += halfway];
                children[2].vertices[0] = vertices[index *= MeshDimension];
                children[3].vertices[0] = vertices[index += halfway];
            }

            for (int child = 0; child < children.Count; ++child)
            {
                // The case of 4 children

                int childcolstart = 2, R0rowstart = 0, R0colstart = 0;
                int C0rowstart = 1, childrowstart = 2, C0colstart = 0;

                SetQuadBounds(child, ref R0rowstart, ref R0colstart, halfway, ref C0rowstart, ref C0colstart);

                /** CHILDROW ZERO **/
                FillRowZero(child, R0rowstart, childcolstart, R0colstart);
                /** CHILDCOL ZERO**/
                FillChildColZero(child, C0rowstart, childrowstart, C0colstart);
                /** INNER GRID **/
                FillInnerGrid(child, childcolstart, R0rowstart + 1, childrowstart, C0colstart + 1);
            }
        }


        private void SetQuadBounds(int child, ref int R0rowstart, ref int R0colstart, int halfway, ref int C0rowstart, ref int C0colstart)
        {

            if (child == 0)
            {
                R0rowstart = 0;
                R0colstart = 1;
                C0rowstart = 1;
                C0colstart = 0;
            }
            if (child == 1)
            {
                R0rowstart = 0;
                R0colstart = halfway + 1;
                C0rowstart = 1;
                C0colstart = halfway;
            }
            if (child == 2)
            {
                R0rowstart = halfway;
                R0colstart = 1;
                C0rowstart = halfway + 1;
                C0colstart = 0;
            }
            if (child == 3)
            {
                R0rowstart = halfway;
                R0colstart = halfway + 1;
                C0rowstart = halfway + 1;
                C0colstart = halfway;
            }
        }


        private void FillInnerGrid(int child, int childcolstart, int R0rowstart, int childrowstart, int C0colstart)
        {
            for (int childrow = childrowstart, row = R0rowstart; childrow < MeshDimension; childrow += 2, ++row)
            {
                for (int childcol = childcolstart, col = C0colstart; childcol < MeshDimension; childcol += 2, ++col)
                {
                    int index = childrow * MeshDimension + childcol;
                    children[child].vertices[index] = vertices[row * MeshDimension + col];
                    children[child].vertices[index - 1].y = linearHalfwayInterpolation(children[child].vertices[index].y, children[child].vertices[index - 2].y);
                }

                for (int childcol = 0; childcol < MeshDimension; ++childcol)
                {
                    int index = (childrow - 1) * MeshDimension + childcol;
                    children[child].vertices[index].y = linearHalfwayInterpolation(
                        children[child].vertices[index - MeshDimension].y,
                        children[child].vertices[index + MeshDimension].y
                        );
                }
            }
        }

        private void FillChildColZero(int child, int rowstart, int childrowstart, int colstart)
        {
            for (int row = rowstart, childrow = childrowstart, col = colstart; childrow < MeshDimension; childrow += 2, ++row)
            {
                children[child].vertices[childrow * MeshDimension].y = vertices[row * MeshDimension + col].y;

                children[child].vertices[(childrow - 1) * MeshDimension].y
                    = linearHalfwayInterpolation(
                        children[child].vertices[childrow * MeshDimension].y,
                        children[child].vertices[(childrow - 2) * MeshDimension].y
                        );
            }
        }


        private void FillRowZero(int child, int rowstart, int childcolstart, int colstart)
        {
            for (int row = rowstart, childcol = childcolstart, col = colstart; childcol < MeshDimension; childcol += 2, ++col)
            {
                children[child].vertices[childcol].y = vertices[row * MeshDimension + col].y;
                children[child].vertices[childcol - 1].y =
                    linearHalfwayInterpolation(children[child].vertices[childcol].y,
                    children[child].vertices[childcol - 2].y
                    );
            }
        }

        private float linearHalfwayInterpolation(float a, float b)
        {
            return Math.Abs(a + (b - a) * 0.5f);
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

            int xStop = xIndexOffset + MeshDimension - 1;
            int zStop = zIndexOffset + MeshDimension - 1;

            int vertexIndex = 0;

            for (int z = zIndexOffset; z <= zStop; ++z)
                for (int x = xIndexOffset; x <= xStop; ++x, ++vertexIndex)
                    vertices[vertexIndex].y = primaryTerrainElevation.data[z * cdbTile.MeshDimension + x] * (float)Database.Projection.Scale;

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
                        for(int i = 0; i < 6; ++i, ++index)
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

        public void CopyArraySubsection(int dimension, int origin, int stop, ref Vector3[] subSection)
        {
            int xOrigin = origin % dimension;
            int yOrigin = origin / dimension;

            int xStop = stop % dimension;
            int yStop = stop / dimension;

            int i = 0;
            for (int y = yOrigin; y <= yStop; ++y)
                for (int x = xOrigin; x <= xStop; ++x)
                    subSection[i++] = vertices[y * dimension + x];
        }

        private void CullOutOfView()
        {
            return;
            var planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            if(!GeometryUtility.TestPlanesAABB(planes, GetComponent<MeshRenderer>().bounds))
            {
                GetComponent<MeshRenderer>().enabled = false;
            }
            else
            {
                GetComponent<MeshRenderer>().enabled = true;
            }
        }

    }

}
