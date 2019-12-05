using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using Cognitics.CoordinateSystems;

namespace Cognitics.UnityCDB
{
    public class Database : MonoBehaviour
    {
        public string Path;
        public double OriginLatitude = 0.0f;
        public double OriginLongitude = 0.0f;
        public GameObject TilePrefab = null;

        public int PerformanceOffsetLOD = 0;

        [HideInInspector] public GeographicBounds GeographicBounds = GeographicBounds.EmptyValue;
        [HideInInspector] public ScaledFlatEarthProjection Projection;

        internal Dictionary<string, LODSwitch> LODSwitchByString = new Dictionary<string, LODSwitch>();
        internal Dictionary<object, LODSwitch> LODSwitchByObject = new Dictionary<object, LODSwitch>();
        internal Dictionary<object, QuadTree> QuadTreeByObject = new Dictionary<object, QuadTree>();
        [HideInInspector] public GTFeatureData ManMadeData = null;
        [HideInInspector] public GTFeatureData TreeData = null;
        [HideInInspector] public GSFeatureData ManMadeDataSpecific = null;

        [HideInInspector] public int MaxLOD = 23;

        internal Vector3 lastCameraPosition = new Vector3(0.0f, 0.0f, 0.0f);

        [HideInInspector] public CDB.Database DB = null;

        public Dictionary<GeographicBounds, Tile> ActiveTiles = new Dictionary<GeographicBounds, Tile>();
        List<Tile> tiles = new List<Tile>();
        public TileDataCache TileDataCache = new TileDataCache();

        private const bool useCameraPositionTask = true; // TEMP
        private Task cameraPositionTask = null;

        [HideInInspector] public ModelManager ModelManager = new ModelManager();
        [HideInInspector] public MeshManager MeshManager = new MeshManager();

        [HideInInspector] public Cognitics.Unity.MaterialManager MaterialManager = null;

        public float SystemMemoryUtilization = 0.0f;
        public float SharedMemoryUtilization = 0.0f;
        public float SystemMemoryUtilizationLimit = 0.8f;
        public float SharedMemoryUtilizationLimit = 0.8f;
        public bool SystemMemoryLimitExceeded => SystemMemoryUtilization > SystemMemoryUtilizationLimit;
        public bool SharedMemoryLimitExceeded => SharedMemoryUtilization > SharedMemoryUtilizationLimit;

        public static bool isDebugBuild = false;


        public bool TileAppliedTerrain = false;

        private void OnLowMemory()
        {
            //SetMaxLOD(MaxLOD - 1);
            Resources.UnloadUnusedAssets();
        }

#if false
        // This is the old, faster version that gets elevation at SW corner of a triangle pair. 
        // TODO: if we want to keep this around for quick but inaccurate lookups, we could rename it appropriately
        public float TerrainElevationAtLocation(GeographicCoordinates location)
        {
            var tiles = ActiveTiles();
            foreach (var tile in tiles)
            {
                if (location.Latitude < tile.GeographicBounds.MinimumCoordinates.Latitude)
                    continue;
                if (location.Longitude < tile.GeographicBounds.MinimumCoordinates.Longitude)
                    continue;
                if (location.Latitude > tile.GeographicBounds.MaximumCoordinates.Latitude)
                    continue;
                if (location.Longitude > tile.GeographicBounds.MaximumCoordinates.Longitude)
                    continue;

                var point = location.TransformedWith(Projection);
                var bounds = tile.GeographicBounds.TransformedWith(Projection);
                var spacingX = (bounds.MaximumCoordinates.X - bounds.MinimumCoordinates.X) / tile.MeshDimension;
                var spacingY = (bounds.MaximumCoordinates.Y - bounds.MinimumCoordinates.Y) / tile.MeshDimension;
                int indexX = (int)((point.X - bounds.MinimumCoordinates.X) / spacingX);
                int indexY = (int)((point.Y - bounds.MinimumCoordinates.Y) / spacingY);
                int index = (indexY * tile.MeshDimension) + indexX;
                if (index >= tile.vertices.Length)
                    index = tile.vertices.Length - 1;
                var vertex = tile.vertices[index];
                return vertex.y;
            }
            return float.MaxValue;
        }
#else
        public float TerrainElevationAtLocation(GeographicCoordinates location)
        {
            foreach (var elem in ActiveTiles)
            {
                Tile tile = elem.Value;
                if (location.Latitude < tile.GeographicBounds.MinimumCoordinates.Latitude)
                    continue;
                if (location.Longitude < tile.GeographicBounds.MinimumCoordinates.Longitude)
                    continue;
                if (location.Latitude > tile.GeographicBounds.MaximumCoordinates.Latitude)
                    continue;
                if (location.Longitude > tile.GeographicBounds.MaximumCoordinates.Longitude)
                    continue;

                var point = location.TransformedWith(Projection);
                var cartesianBounds = tile.GeographicBounds.TransformedWith(Projection);

                double spcX = (cartesianBounds.MaximumCoordinates.X - cartesianBounds.MinimumCoordinates.X) / tile.MeshDimension;
                double spcZ = (cartesianBounds.MaximumCoordinates.Y - cartesianBounds.MinimumCoordinates.Y) / tile.MeshDimension;
                double orgX = cartesianBounds.MinimumCoordinates.X;
                double orgZ = cartesianBounds.MinimumCoordinates.Y;

                float xComponent = (float)point.X - (float)orgX;
                float zComponent = (float)point.Y - (float)orgZ;

                int xIndex = Math.Min(tile.MeshDimension - 2, (int)Math.Floor(xComponent / spcX));
                int zIndex = Math.Min(tile.MeshDimension - 2, (int)Math.Floor(zComponent / spcZ));

                int[] indices = new int[4];
                Vector3[] vertices = new Vector3[4];
                indices[0] = zIndex * tile.MeshDimension + xIndex;
                indices[1] = indices[0] + 1;
                indices[2] = indices[0] + tile.MeshDimension;
                indices[3] = indices[0] + tile.MeshDimension + 1;
                for (int i = 0; i < indices.Length; ++i)
                    vertices[i] = tile.vertices[indices[i]];
                ref Vector3 a = ref vertices[0];
                ref Vector3 b = ref vertices[1];
                ref Vector3 c = ref vertices[2];
                ref Vector3 d = ref vertices[3];

                Vector3 position = new Vector3((float)point.X, 0f, (float)point.Y);
                Vector2 position2d = new Vector2((float)point.X, (float)point.Y);

                //Vector3 p0 = position;
                //Vector3 p1 = position;
                //bool success0 = SurfaceCollider.InterpolatePointInPlane(ref p0, a, c, d);
                //bool success1 = SurfaceCollider.InterpolatePointInPlane(ref p1, a, d, b);

                float u0, v0, w0;
                SurfaceCollider.GetBarycentricCoords(position, a, c, d, out u0, out v0, out w0);
                float u1, v1, w1;
                SurfaceCollider.GetBarycentricCoords(position, a, d, b, out u1, out v1, out w1);

                var plane0 = SurfaceCollider.GetPlane(a, c, d);
                var plane1 = SurfaceCollider.GetPlane(a, d, b);

                float y0 = SurfaceCollider.GetHeight(plane0, position2d);
                float y1 = SurfaceCollider.GetHeight(plane1, position2d);
                if (v0 >= 0f && w0 >= 0f && u0 <= 1f)
                    return y0;
                else if (v1 >= 0f && w1 >= 0f && u1 <= 1f)
                    return y1;
                else
                    return (y0 + y1) / 2;
            }
            return float.MaxValue;
        }
#endif
        public float DistanceForBounds(GeographicBounds bounds)
        {
            Vector3 position = lastCameraPosition;
            List<Tile> tilesToCheck = new List<Tile>();

            CartesianBounds cartesianBounds = bounds.TransformedWith(Projection);

            // LeftLowerBound and boundsMin both provide the same data and can be refactored later. This is also true of RightUpperBound and boundsMax.
            Vector3 LeftLowerBound = new Vector3((float)cartesianBounds.MinimumCoordinates.X, 0, (float)cartesianBounds.MinimumCoordinates.Y);
            Vector3 RightUpperBound = new Vector3((float)cartesianBounds.MaximumCoordinates.X, 0, (float)cartesianBounds.MaximumCoordinates.Y);
            Vector2 boundsMin = new Vector2(LeftLowerBound.x, LeftLowerBound.z);
            Vector2 boundsMax = new Vector2(RightUpperBound.x, RightUpperBound.z);

            var active_tiles = new List<Tile>(ActiveTiles.Values);
            foreach (var t in active_tiles)
            {
                CartesianBounds tileBounds = t.GeographicBounds.TransformedWith(Projection);

                Vector2 tileBoundsMin = new Vector2((float)tileBounds.MinimumCoordinates.X, (float)tileBounds.MinimumCoordinates.Y);
                Vector2 tileBoundsMax = new Vector2((float)tileBounds.MaximumCoordinates.X, (float)tileBounds.MaximumCoordinates.Y);

                if (BoundsOverlap(tileBoundsMin, tileBoundsMax, boundsMin, boundsMax))
                    tilesToCheck.Add(t);
            }

            NearestVertex v = new NearestVertex();
            float closestVertexDistance = float.MaxValue;

            foreach (Tile t in tilesToCheck)
            {
                int candidateIndex = v.GetNearestVertexIndex(position, t.vertices, LeftLowerBound, RightUpperBound);
                if (candidateIndex == -1)
                    continue;
                float candidateDistance = Vector3.Distance(t.vertices[candidateIndex], position);
                if (candidateDistance < closestVertexDistance)
                    closestVertexDistance = candidateDistance;
            }

            return closestVertexDistance;
        }

        private bool BoundsOverlap(Vector2 BottomLeft1, Vector2 TopRight1, Vector2 BottomLeft2, Vector2 TopRight2)
        {
            if (TopRight2.x < BottomLeft1.x || TopRight1.x < BottomLeft2.x)
                return false;

            if (TopRight2.y < BottomLeft1.y || TopRight1.y < BottomLeft2.y)
                return false;

            return true;
        }

        public void InitializeGTFeatureLayer(ref GTFeatureData featureData, CDB.VectorComponent component, CDB.LOD lod)
        {
            if (featureData != null)
                return;

            featureData = new GTFeatureData();
            featureData.Database = this;
            featureData.Component = component;
            featureData.lod = lod;

            LODSwitchByObject[featureData] = LODSwitchByString["GTFeatures"];

            var childGameObject = new GameObject();
            childGameObject.transform.SetParent(gameObject.transform);
            childGameObject.name = component.GetType().Name;

            var childQuadTree = childGameObject.AddComponent<QuadTree>();
            childQuadTree.Initialize(this, GeographicBounds);
            childQuadTree.SwitchDelegate = LODSwitchByObject[featureData].QuadTreeSwitchUpdate;
            childQuadTree.LoadDelegate = featureData.QuadTreeDataLoad;
            childQuadTree.LoadedDelegate = featureData.QuadTreeDataLoaded;
            childQuadTree.UnloadDelegate = featureData.QuadTreeDataUnload;
            childQuadTree.UpdateDelegate = featureData.QuadTreeDataUpdate;

            QuadTreeByObject[featureData] = childQuadTree;

            var cdbTiles = DB.Tiles.Generate(GeographicBounds, lod);
            foreach (var cdbTile in cdbTiles)
                childQuadTree.AddChild(cdbTile.Bounds);
        }

        public void DestroyGTFeatureLayer(ref GTFeatureData featureData)
        {
            Destroy(QuadTreeByObject[featureData].gameObject);
            QuadTreeByObject.Remove(featureData);
            LODSwitchByObject.Remove(featureData);
            featureData.Unload();
            featureData = null;
        }

        public void InitializeGSFeatureLayer(ref GSFeatureData featureData, CDB.VectorComponent component, CDB.LOD lod)
        {
            if (featureData != null)
                return;

            featureData = new GSFeatureData();
            featureData.Database = this;
            featureData.Component = component;
            featureData.lod = lod;

            LODSwitchByObject[featureData] = LODSwitchByString["GSFeatures"];

            var childGameObject = new GameObject();
            childGameObject.transform.SetParent(gameObject.transform);
            childGameObject.name = component.GetType().Name;

            var childQuadTree = childGameObject.AddComponent<QuadTree>();
            childQuadTree.Initialize(this, GeographicBounds);
            childQuadTree.SwitchDelegate = LODSwitchByObject[featureData].QuadTreeSwitchUpdate;
            childQuadTree.LoadDelegate = featureData.QuadTreeDataLoad;
            childQuadTree.LoadedDelegate = featureData.QuadTreeDataLoaded;
            childQuadTree.UnloadDelegate = featureData.QuadTreeDataUnload;
            childQuadTree.UpdateDelegate = featureData.QuadTreeDataUpdate;

            QuadTreeByObject[featureData] = childQuadTree;

            var cdbTiles = DB.Tiles.Generate(GeographicBounds, lod);
            foreach (var cdbTile in cdbTiles)
                childQuadTree.AddChild(cdbTile.Bounds);
        }

        public void DestroyGSFeatureLayer(ref GSFeatureData featureData)
        {
            Destroy(QuadTreeByObject[featureData].gameObject);
            QuadTreeByObject.Remove(featureData);
            LODSwitchByObject.Remove(featureData);
            featureData.Unload();
            featureData = null;
        }

        public void OnDestroy()
        {
            if(ManMadeDataSpecific != null)
                DestroyGSFeatureLayer(ref ManMadeDataSpecific);

            if(ManMadeData != null)
                DestroyGTFeatureLayer(ref ManMadeData);

            if(TreeData != null)
                DestroyGTFeatureLayer(ref TreeData);
        }

        ~Database()
        {
            foreach (var tile in tiles)
                Tile.DestroyTile(tile);
        }

        public bool Exists => DB.Exists;

        public void Initialize()
        {
            if(MaterialManager == null)
                MaterialManager = gameObject.AddComponent<Cognitics.Unity.MaterialManager>();

            // NOTE: Debug.isDebugBuild can only be queried from main thread, so we cache it here for use in any thread
            isDebugBuild = Debug.isDebugBuild;

            MeshManager.db = this;


            //MaterialManager.Debug = true;


            Application.lowMemory += OnLowMemory;
            if (DB != null)
                return;
            if (Path.Length == 0)
                return;
            DB = new CDB.Database(Path);
            if (!DB.Exists)
                return;
            if (GeographicBounds == GeographicBounds.EmptyValue)
                GeographicBounds = DB.ExistingBounds();
            if ((OriginLatitude == 0.0f) && (OriginLongitude == 0.0f))
            {
                OriginLatitude = GeographicBounds.Center.Latitude;
                OriginLongitude = GeographicBounds.Center.Longitude;
            }
            Projection = new ScaledFlatEarthProjection(new GeographicCoordinates(OriginLatitude, OriginLongitude), 0.1f);

            InitLODRanges();
            SetLODBracketsForOverview();

            var cartesianBounds = GeographicBounds.TransformedWith(Projection);
            Unity.TouchInput.CreateWalls(cartesianBounds);
            ModelManager.Init(cartesianBounds);
            ModelManager.Database = this;
        }

        void InitLODRanges()
        {
            {
                var lods = new LODSwitch(this);
                lods.EntryDistanceByLOD[-10] = float.MaxValue; lods.ExitDistanceByLOD[-10] = float.MaxValue;
                lods.EntryDistanceByLOD[-9] = 100000; lods.ExitDistanceByLOD[-9] = 110000;
                lods.EntryDistanceByLOD[-8] = 50000; lods.ExitDistanceByLOD[-8] = 55000;
                lods.EntryDistanceByLOD[-7] = 25000; lods.ExitDistanceByLOD[-7] = 27500;
                lods.EntryDistanceByLOD[-6] = 10000; lods.ExitDistanceByLOD[-6] = 11000;
                lods.EntryDistanceByLOD[-5] = 5000; lods.ExitDistanceByLOD[-5] = 5500;
                lods.EntryDistanceByLOD[-4] = 2500; lods.ExitDistanceByLOD[-4] = 2750;
                lods.EntryDistanceByLOD[-3] = 1000; lods.ExitDistanceByLOD[-3] = 2200;
                lods.EntryDistanceByLOD[-2] = 500; lods.ExitDistanceByLOD[-2] = 550;
                lods.EntryDistanceByLOD[-1] = 250; lods.ExitDistanceByLOD[-1] = 275;
                LODSwitchByString["Overview"] = lods;
            }

            {
                var lods = new LODSwitch(this);
                lods.EntryDistanceByLOD[-10] = float.MaxValue; lods.ExitDistanceByLOD[-10] = float.MaxValue;
                lods.EntryDistanceByLOD[-9] = 1000000; lods.ExitDistanceByLOD[-9] = 1100000;
                lods.EntryDistanceByLOD[-8] = 500000; lods.ExitDistanceByLOD[-8] = 550000;
                lods.EntryDistanceByLOD[-7] = 250000; lods.ExitDistanceByLOD[-7] = 275000;
                lods.EntryDistanceByLOD[-6] = 100000; lods.ExitDistanceByLOD[-6] = 110000;
                lods.EntryDistanceByLOD[-5] = 50000; lods.ExitDistanceByLOD[-5] = 55000;
                lods.EntryDistanceByLOD[-4] = 25000; lods.ExitDistanceByLOD[-4] = 27500;
                lods.EntryDistanceByLOD[-3] = 10000; lods.ExitDistanceByLOD[-3] = 22000;
                lods.EntryDistanceByLOD[-2] = 500; lods.ExitDistanceByLOD[-2] = 550;
                lods.EntryDistanceByLOD[-1] = 300; lods.ExitDistanceByLOD[-1] = 330;
                lods.EntryDistanceByLOD[0] = 300; lods.ExitDistanceByLOD[0] = 310;
                lods.EntryDistanceByLOD[1] = 200; lods.ExitDistanceByLOD[1] = 210;
                lods.EntryDistanceByLOD[2] = 100; lods.ExitDistanceByLOD[2] = 110;
                lods.EntryDistanceByLOD[3] = 80; lods.ExitDistanceByLOD[3] = 90;
                lods.EntryDistanceByLOD[4] = 60; lods.ExitDistanceByLOD[4] = 70;
#if !UNITY_ANDROID
                lods.EntryDistanceByLOD[5] = 40; lods.ExitDistanceByLOD[5] = 50;
                lods.EntryDistanceByLOD[6] = 30; lods.ExitDistanceByLOD[6] = 40;
                //lods.EntryDistanceByLOD[7] = 20; lods.ExitDistanceByLOD[7] = 30;
                //lods.EntryDistanceByLOD[8] = 15; lods.ExitDistanceByLOD[8] = 20;

                //lods.EntryDistanceByLOD[9] = 12; lods.ExitDistanceByLOD[9] = 18;
                //lods.EntryDistanceByLOD[10] = 9; lods.ExitDistanceByLOD[10] = 15;
                //lods.EntryDistanceByLOD[11] = 5; lods.ExitDistanceByLOD[11] = 10;
#endif
                LODSwitchByString["Detail"] = lods;
            }

            {
                var lods = new LODSwitch(this);
                lods.EntryDistanceByLOD[0] = 6000; lods.ExitDistanceByLOD[0] = 6600;
                lods.EntryDistanceByLOD[1] = 5000; lods.ExitDistanceByLOD[1] = 5500;
                lods.EntryDistanceByLOD[2] = 4000; lods.ExitDistanceByLOD[2] = 4400;
                lods.EntryDistanceByLOD[3] = 3000; lods.ExitDistanceByLOD[3] = 3300;
                lods.EntryDistanceByLOD[4] = 2000; lods.ExitDistanceByLOD[4] = 2200;
                lods.EntryDistanceByLOD[5] = 1000; lods.ExitDistanceByLOD[5] = 1100;
                //lods.EntryDistanceByLOD[6] = 900; lods.ExitDistanceByLOD[6] = 990;
                LODSwitchByString["GTFeatures"] = lods;
            }

            {
                var lods = new LODSwitch(this);
                lods.EntryDistanceByLOD[0] = 6000; lods.ExitDistanceByLOD[0] = 6600;
                lods.EntryDistanceByLOD[1] = 5000; lods.ExitDistanceByLOD[1] = 5500;
                lods.EntryDistanceByLOD[2] = 4000; lods.ExitDistanceByLOD[2] = 4400;
                lods.EntryDistanceByLOD[3] = 3000; lods.ExitDistanceByLOD[3] = 3300;
                lods.EntryDistanceByLOD[4] = 2000; lods.ExitDistanceByLOD[4] = 2200;

                //lods.EntryDistanceByLOD[5] = 1000; lods.ExitDistanceByLOD[5] = 1100;
                //lods.EntryDistanceByLOD[6] = 900; lods.ExitDistanceByLOD[6] = 910;

                //lods.EntryDistanceByLOD[7] = 800; lods.ExitDistanceByLOD[6] = 1810;
                //lods.EntryDistanceByLOD[8] = 700; lods.ExitDistanceByLOD[6] = 1710;
                //lods.EntryDistanceByLOD[9] = 600; lods.ExitDistanceByLOD[6] = 1610;
                LODSwitchByString["GSFeatures"] = lods;
            }

            LoadLODRanges(System.IO.Path.Combine(Path, "LODRanges.xml"));
            LoadOnlineInfo(System.IO.Path.Combine(Path, "OnlineInfo.xml"));
        }

        public void SetLODBracketsForDetail()
        {
            LODSwitchByObject[this] = LODSwitchByString["Detail"];
        }

        public void SetLODBracketsForOverview()
        {
            LODSwitchByObject[this] = LODSwitchByString["Overview"];
        }

        private void LoadOnlineInfo(string filename)
        {
            try
            {
                var xml = new XmlDocument();
                xml.Load(filename);
                if (xml.DocumentElement.Name != "OnlineInfo")
                    return;
                foreach (XmlNode child in xml.DocumentElement.ChildNodes)
                {
                    if (child.Name == "Elevation")
                    {
                        foreach (XmlNode grandchild in child.ChildNodes)
                        {
                            if (grandchild.Name == "Server")
                                TileDataCache.OnlineElevationServer = grandchild.InnerText;
                            if (grandchild.Name == "Layer")
                                TileDataCache.OnlineElevationLayer = grandchild.InnerText;
                        }
                    }
                    if (child.Name == "Imagery")
                    {
                        foreach (XmlNode grandchild in child.ChildNodes)
                        {
                            if (grandchild.Name == "Server")
                                TileDataCache.OnlineImageryServer = grandchild.InnerText;
                            if (grandchild.Name == "Layer")
                                TileDataCache.OnlineImageryLayer = grandchild.InnerText;
                        }
                    }
                }
            }
            catch
            {
            }
        }

        private void LoadLODRanges(string filename)
        {
            try
            {
                var xml = new XmlDocument();
                xml.Load(filename);
                if (xml.DocumentElement.Name != "LODRanges")
                    return;
                foreach (XmlNode child in xml.DocumentElement.ChildNodes)
                    LoadLODRange(child as XmlElement);
            }
            catch
            {
            }
        }

        private void LoadLODRange(XmlElement node)
        {
            if (node == null)
                return;
            var lodSwitch = new LODSwitch(this);
            if (node.HasAttribute("max_distance") && (float.TryParse(node.Attributes["max_distance"].Value, out float max_distance)))
                lodSwitch.MaxDistance = max_distance;
            foreach (XmlElement child in node.ChildNodes)
            {
                if (child.Name != "LOD")
                    continue;
                if (!child.HasAttribute("id"))
                    continue;
                if (!int.TryParse(child.Attributes["id"].Value, out int id))
                    continue;
                float indist = float.MaxValue;
                float outdist = float.MaxValue;
                if(child.HasAttribute("in") && (!float.TryParse(child.Attributes["in"].Value, out indist)))
                    indist = float.MaxValue;
                if(child.HasAttribute("out") && (!float.TryParse(child.Attributes["out"].Value, out outdist)))
                    outdist = float.MaxValue;
                lodSwitch.EntryDistanceByLOD[id] = indist;
                lodSwitch.ExitDistanceByLOD[id] = outdist;
            }
            LODSwitchByString[node.Name] = lodSwitch;
        }

        
        public void SetMaxLOD(int value)
        {
            MaxLOD = value;
            //DefineLODBrackets();
        }

        public void Initialize(string path)
        {
            Path = path;
            Initialize();
        }

        void Awake()
        {
            Initialize();
        }

        void Start()
        {
            Initialize();
        }

        void Update()
        {
            if(Time.frameCount % 10 == 0)
                TileAppliedTerrain = false;
            TileDataCache.Run();
        }

        public void HighlightModelByTag(string tag) => ModelManager.HighlightForTag(tag);

        public void GenerateTerrainForLOD(CDB.LOD lod)
        {
            var cdbTiles = DB.Tiles.Generate(GeographicBounds, lod);
            cdbTiles.ForEach(cdbTile => tiles.AddRange(GenerateTiles(cdbTile)));
            tiles.ForEach(tile => tile.IsActive = true);
        }


        ////////////////////////////////////////////////////////////

        public List<Tile> GenerateTiles(CDB.Tile cdbTile)
        {
            var result = new List<Tile>();
            if (cdbTile.LOD < 0)
            {
                var tile = GenerateTile(cdbTile, cdbTile.Bounds);
                result.Add(tile);
                return result;
            }
            result.Add(GenerateSouthWestTile(cdbTile));
            result.Add(GenerateSouthEastTile(cdbTile));
            result.Add(GenerateNorthWestTile(cdbTile));
            result.Add(GenerateNorthEastTile(cdbTile));
            return result;
        }

        Tile GenerateSouthWestTile(CDB.Tile cdbTile)
        {
            var bounds = cdbTile.Bounds;
            bounds.MaximumCoordinates = cdbTile.Bounds.Center;
            return GenerateTile(cdbTile, bounds);
        }

        Tile GenerateSouthEastTile(CDB.Tile cdbTile)
        {
            var bounds = cdbTile.Bounds;
            bounds.MinimumCoordinates.Longitude = cdbTile.Bounds.Center.Longitude;
            bounds.MaximumCoordinates.Latitude = cdbTile.Bounds.Center.Latitude;
            return GenerateTile(cdbTile, bounds);
        }

        Tile GenerateNorthWestTile(CDB.Tile cdbTile)
        {
            var bounds = cdbTile.Bounds;
            bounds.MaximumCoordinates.Longitude = cdbTile.Bounds.Center.Longitude;
            bounds.MinimumCoordinates.Latitude = cdbTile.Bounds.Center.Latitude;
            return GenerateTile(cdbTile, bounds);
        }

        Tile GenerateNorthEastTile(CDB.Tile cdbTile)
        {
            var bounds = cdbTile.Bounds;
            bounds.MinimumCoordinates = cdbTile.Bounds.Center;
            return GenerateTile(cdbTile, bounds);
        }

        Tile GenerateTile(CDB.Tile cdbTile, GeographicBounds geographicBounds)
        {
            GameObject child = Instantiate(TilePrefab, transform);
            var tile = child.GetComponent<Tile>();
            tile.Database = this;
            tile.CDBTile = cdbTile;
            tile.GeographicBounds = geographicBounds;
            return tile;
        }


        ////////////////////////////////////////////////////////////

        public int VertexCount()
        {
            int result = 0;
            foreach (var elem in ActiveTiles)
            {
                Tile tile = elem.Value;
                result += tile.vertices.Length;
            }
            return result;
        }

        public int TriangleCount()
        {
            int result = 0;
            foreach (var elem in ActiveTiles)
            {
                Tile tile = elem.Value;
                result += tile.RasterDimension * tile.RasterDimension * 2;
            }
            return result;
        }

        public int ModelCount()
        {
            int result = 0;
            result += (ManMadeData == null) ? 0 : ManMadeData.ModelCount();
            result += (TreeData == null) ? 0 : TreeData.ModelCount();
            result += (ManMadeDataSpecific == null) ? 0 : ManMadeDataSpecific.ModelCount();
            return result;
        }

        public int TerrainMemory(bool activeOnly)
        {
            int result = 0;
            tiles.ForEach(tile => result += tile.Memory(activeOnly));
            return result;
        }

        public void ApplyCameraPosition(Vector3 position)
        {
            lastCameraPosition = position;
            if (useCameraPositionTask)
            {
                if (cameraPositionTask == null)
                    cameraPositionTask = Task.Run(() => { ApplyCameraPositionFunc(); });
            }
            else
            {
                if (ManMadeData != null)
                    ManMadeData.ApplyCameraPosition(position);
                if (TreeData != null)
                    TreeData.ApplyCameraPosition(position);
                if (ManMadeDataSpecific != null)
                    ManMadeDataSpecific.ApplyCameraPosition(position);
            }
        }

        // Entry method for the acp task
        public void ApplyCameraPositionFunc()
        {
            while (true)
            {
                if (ManMadeData != null)
                    ManMadeData.ApplyCameraPosition(lastCameraPosition);
                if (TreeData != null)
                    TreeData.ApplyCameraPosition(lastCameraPosition);
                if (ManMadeDataSpecific != null)
                    ManMadeDataSpecific.ApplyCameraPosition(lastCameraPosition);
            }
        }
        
        public void SetTileBounds()
        {
            Unity.TouchInput.test = 0;
        }

        public void UpdateTerrain(GeographicBounds changedBounds)
        {
            EdgeCrackElimination.EliminateCracks(new List<Tile>(ActiveTiles.Values));
            if (ManMadeData != null)
                ManMadeData.UpdateElevations(changedBounds);
            if (TreeData != null)
                TreeData.UpdateElevations(changedBounds);
            if (ManMadeDataSpecific != null)
                ManMadeDataSpecific.UpdateElevations(changedBounds);
        }

        ////////////////////////////////////////////////////////////
        // util

        public float[] GetMeshPerimeterY(Vector3[] vertices, int dimension)
        {
            var result = new float[dimension * 4];
            for (int i = 0; i < dimension; ++i)
            {
                result[(dimension * 0) + i] = vertices[i].y; // south (w->e)
                result[(dimension * 1) + i] = vertices[(dimension * i) + dimension - 1].y; // east (s->n)
                result[(dimension * 2) + i] = vertices[(dimension * (dimension - 1)) + i].y; // north (w->e)
                result[(dimension * 3) + i] = vertices[dimension * i].y; // west (s->n)
            }
            return result;
        }

        public void SetMeshPerimeter(Vector3[] vertices, int dimension, float[] perimeter)
        {
            if ((vertices == null) || (vertices.Length == 0))
                return;
            if ((perimeter == null) || (perimeter.Length == 0))
                return;
            for (int i = 0; i < dimension; ++i)
            {
                vertices[i].y = perimeter[(dimension * 0) + i]; // south (w->e)
                vertices[(dimension * i) + dimension - 1].y = perimeter[(dimension * 1) + i]; // east (s->n)
                vertices[(dimension * (dimension - 1)) + i].y = perimeter[(dimension * 2) + i]; // north (w->e)
                vertices[dimension * i].y = perimeter[(dimension * 3) + i]; // west (s->n)
            }
        }
    }
}
