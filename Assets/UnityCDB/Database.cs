
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using Cognitics.CoordinateSystems;
using Cognitics.OpenFlight;

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

        internal Dictionary<object, LODSwitch> LODSwitchByObject = new Dictionary<object, LODSwitch>();
        internal Dictionary<object, QuadTree> QuadTreeByObject = new Dictionary<object, QuadTree>();
        [HideInInspector] public GTFeatureData ManMadeData = null;
        [HideInInspector] public GTFeatureData TreeData = null;
        [HideInInspector] public GSFeatureData ManMadeDataSpecific = null;

        [HideInInspector] public int MaxLOD = 23;

        internal Vector3 lastCameraPosition = new Vector3(0.0f, 0.0f, 0.0f);

        public Dictionary<CDB.LOD, ValueTuple<float, float>> LODBrackets = new Dictionary<CDB.LOD, ValueTuple<float, float>>();

        [HideInInspector] public CDB.Database DB = null;

        [HideInInspector] public GeoPackage.Database GPKG = null;
        [HideInInspector] public GeoPackage.RasterLayer GPKG_Elevation = null;
        [HideInInspector] public GeoPackage.RasterLayer GPKG_Imagery = null;
        TerrainTile GPKG_Terrain = null;

        List<Tile> tiles = new List<Tile>();
        public TileDataCache TileDataCache = new TileDataCache();

        [HideInInspector] public ModelManager ModelManager = new ModelManager();
        [HideInInspector] public MaterialManager MaterialManager = new MaterialManager();
        [HideInInspector] public MeshManager MeshManager = new MeshManager();

        private void OnLowMemory()
        {
            //SetMaxLOD(MaxLOD - 1);
            Resources.UnloadUnusedAssets();
        }

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

        /*
        public float DistanceForBoundsGeoPackage(GeographicBounds bounds)
        {
            Vector3 position = lastCameraPosition;
            var activeTiles = ActiveTilesGeoPackage();
            var tilesToCheck = new List<QuadTreeNode>();

            var cartesianBounds = bounds.TransformedWith(Projection);

            // LeftLowerBound and boundsMin both provide the same data and can be refactored later. This is also true of RightUpperBound and boundsMax.
            var LeftLowerBound = new Vector3((float)cartesianBounds.MinimumCoordinates.X, 0, (float)cartesianBounds.MinimumCoordinates.Y);
            var RightUpperBound = new Vector3((float)cartesianBounds.MaximumCoordinates.X, 0, (float)cartesianBounds.MaximumCoordinates.Y);
            var boundsMin = new Vector2(LeftLowerBound.x, LeftLowerBound.z);
            var boundsMax = new Vector2(RightUpperBound.x, RightUpperBound.z);

            foreach (var tile in activeTiles)
            {
                var tileBounds = tile.GeographicBounds.TransformedWith(Projection);

                var tileBoundsMin = new Vector2((float)tileBounds.MinimumCoordinates.X, (float)tileBounds.MinimumCoordinates.Y);
                var tileBoundsMax = new Vector2((float)tileBounds.MaximumCoordinates.X, (float)tileBounds.MaximumCoordinates.Y);

                if (BoundsOverlap(tileBoundsMin, tileBoundsMax, boundsMin, boundsMax))
                    tilesToCheck.Add(tile);
            }

            var nearestVertex = new NearestVertex();
            float closestVertexDistance = float.MaxValue;

            foreach (var tile in tilesToCheck)
            {
                var data = GPKG_Terrain.DataByTile[tile];

                int candidateIndex = nearestVertex.GetNearestVertexIndex(position, data.vertices, LeftLowerBound, RightUpperBound);
                float candidateDistance = Vector3.Distance(data.vertices[candidateIndex], position);
                if (candidateDistance < closestVertexDistance)
                    closestVertexDistance = candidateDistance;
            }

            return closestVertexDistance;
        }
        */

        public float DistanceForBounds(GeographicBounds bounds)
        {
            /*
            if (GPKG_Terrain != null)
                return DistanceForBoundsGeoPackage(bounds);
                */

            Vector3 position = lastCameraPosition;
            List<Tile> activeTiles = new List<Tile>();
            List<Tile> tilesToCheck = new List<Tile>();
            activeTiles = ActiveTiles();

            CartesianBounds cartesianBounds = bounds.TransformedWith(Projection);

            // LeftLowerBound and boundsMin both provide the same data and can be refactored later. This is also true of RightUpperBound and boundsMax.
            Vector3 LeftLowerBound = new Vector3((float)cartesianBounds.MinimumCoordinates.X, 0, (float)cartesianBounds.MinimumCoordinates.Y);
            Vector3 RightUpperBound = new Vector3((float)cartesianBounds.MaximumCoordinates.X, 0, (float)cartesianBounds.MaximumCoordinates.Y);
            Vector2 boundsMin = new Vector2(LeftLowerBound.x, LeftLowerBound.z);
            Vector2 boundsMax = new Vector2(RightUpperBound.x, RightUpperBound.z);

            foreach (Tile t in activeTiles)
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

            var lodSwitch = new LODSwitch(this);

            /*
            float entryDistance = 50;
            for (int i = 6; i > 0; --i)
            {
                lodSwitch.EntryDistanceByLOD[i] = entryDistance;
                lodSwitch.ExitDistanceByLOD[i] = entryDistance * (float)2.5;
                entryDistance *= 2;
            }
            */


            lodSwitch.EntryDistanceByLOD[0] = 600; lodSwitch.ExitDistanceByLOD[0] = 660;
            lodSwitch.EntryDistanceByLOD[1] = 500; lodSwitch.ExitDistanceByLOD[1] = 550;
            lodSwitch.EntryDistanceByLOD[2] = 400; lodSwitch.ExitDistanceByLOD[2] = 440;
            lodSwitch.EntryDistanceByLOD[3] = 300; lodSwitch.ExitDistanceByLOD[3] = 330;
            //lodSwitch.EntryDistanceByLOD[4] = 200; lodSwitch.ExitDistanceByLOD[4] = 220;
            //lodSwitch.EntryDistanceByLOD[5] = 100; lodSwitch.ExitDistanceByLOD[5] = 110;
            //lodSwitch.EntryDistanceByLOD[6] = 50; lodSwitch.ExitDistanceByLOD[6] = 60;

            /*
#if UNITY_ANDROID
            lodSwitch.EntryDistanceByLOD[0] = 100; lodSwitch.ExitDistanceByLOD[0] = 120;
            lodSwitch.EntryDistanceByLOD[1] = 50; lodSwitch.ExitDistanceByLOD[1] = 60;
#else
            lodSwitch.EntryDistanceByLOD[0] = 200; lodSwitch.ExitDistanceByLOD[0] = 220;
            lodSwitch.EntryDistanceByLOD[1] = 100; lodSwitch.ExitDistanceByLOD[1] = 110;
            lodSwitch.EntryDistanceByLOD[2] = 50; lodSwitch.ExitDistanceByLOD[2] = 60;
#endif
*/

            //lodSwitch.EntryDistanceByLOD[0] = 50; lodSwitch.ExitDistanceByLOD[0] = 60;
            //lodSwitch.EntryDistanceByLOD[1] = 25; lodSwitch.ExitDistanceByLOD[1] = 40;


            //lodSwitch.EntryDistanceByLOD[0] = 200; lodSwitch.ExitDistanceByLOD[0] = 2420;
            //lodSwitch.EntryDistanceByLOD[1] = 100; lodSwitch.ExitDistanceByLOD[1] = 120;

            //lodSwitch.EntryDistanceByLOD[2] = 50;
            //lodSwitch.ExitDistanceByLOD[2] = 100;

            /*
            lodSwitch.EntryDistanceByLOD[3] = 50;
            lodSwitch.ExitDistanceByLOD[3] = 100;

            lodSwitch.EntryDistanceByLOD[4] = 10;
            lodSwitch.ExitDistanceByLOD[4] = 50;

            lodSwitch.EntryDistanceByLOD[5] = 0;
            lodSwitch.ExitDistanceByLOD[5] = 10;
            */

            LODSwitchByObject[featureData] = lodSwitch;

            var childGameObject = new GameObject();
            childGameObject.transform.SetParent(gameObject.transform);
            childGameObject.name = component.GetType().Name;

            var childQuadTree = childGameObject.AddComponent<QuadTree>();
            childQuadTree.Initialize(this, GeographicBounds);
            childQuadTree.SwitchDelegate = lodSwitch.QuadTreeSwitchUpdate;
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

            var lodSwitch = new LODSwitch(this);

            /*
            float entryDistance = 50;
            for (int i = 6; i > 0; --i)
            {
                lodSwitch.EntryDistanceByLOD[i] = entryDistance;
                lodSwitch.ExitDistanceByLOD[i] = entryDistance * (float)2.5;
                entryDistance *= 2;
            }
            */


            lodSwitch.EntryDistanceByLOD[0] = 600; lodSwitch.ExitDistanceByLOD[0] = 660;
            lodSwitch.EntryDistanceByLOD[1] = 500; lodSwitch.ExitDistanceByLOD[1] = 550;
            lodSwitch.EntryDistanceByLOD[2] = 400; lodSwitch.ExitDistanceByLOD[2] = 440;
            lodSwitch.EntryDistanceByLOD[3] = 300; lodSwitch.ExitDistanceByLOD[3] = 330;
            lodSwitch.EntryDistanceByLOD[4] = 200; lodSwitch.ExitDistanceByLOD[4] = 220;
            lodSwitch.EntryDistanceByLOD[5] = 100; lodSwitch.ExitDistanceByLOD[5] = 110;
            //lodSwitch.EntryDistanceByLOD[6] = 50; lodSwitch.ExitDistanceByLOD[6] = 60;

            /*
#if UNITY_ANDROID
            lodSwitch.EntryDistanceByLOD[0] = 100; lodSwitch.ExitDistanceByLOD[0] = 120;
            lodSwitch.EntryDistanceByLOD[1] = 50; lodSwitch.ExitDistanceByLOD[1] = 60;
#else
            lodSwitch.EntryDistanceByLOD[0] = 200; lodSwitch.ExitDistanceByLOD[0] = 220;
            lodSwitch.EntryDistanceByLOD[1] = 100; lodSwitch.ExitDistanceByLOD[1] = 110;
            lodSwitch.EntryDistanceByLOD[2] = 50; lodSwitch.ExitDistanceByLOD[2] = 60;
#endif
*/

            //lodSwitch.EntryDistanceByLOD[0] = 50; lodSwitch.ExitDistanceByLOD[0] = 60;
            //lodSwitch.EntryDistanceByLOD[1] = 25; lodSwitch.ExitDistanceByLOD[1] = 40;


            //lodSwitch.EntryDistanceByLOD[0] = 200; lodSwitch.ExitDistanceByLOD[0] = 2420;
            //lodSwitch.EntryDistanceByLOD[1] = 100; lodSwitch.ExitDistanceByLOD[1] = 120;

            //lodSwitch.EntryDistanceByLOD[2] = 50;
            //lodSwitch.ExitDistanceByLOD[2] = 100;

            /*
            lodSwitch.EntryDistanceByLOD[3] = 50;
            lodSwitch.ExitDistanceByLOD[3] = 100;

            lodSwitch.EntryDistanceByLOD[4] = 10;
            lodSwitch.ExitDistanceByLOD[4] = 50;

            lodSwitch.EntryDistanceByLOD[5] = 0;
            lodSwitch.ExitDistanceByLOD[5] = 10;
            */

            LODSwitchByObject[featureData] = lodSwitch;

            var childGameObject = new GameObject();
            childGameObject.transform.SetParent(gameObject.transform);
            childGameObject.name = component.GetType().Name;

            var childQuadTree = childGameObject.AddComponent<QuadTree>();
            childQuadTree.Initialize(this, GeographicBounds);
            childQuadTree.SwitchDelegate = lodSwitch.QuadTreeSwitchUpdate;
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
            foreach (var node in featureData.DataByNode.Keys)
                featureData.QuadTreeDataUnload(node);
            featureData = null;
        }

        ~Database()
        {
            foreach (var tile in tiles)
                tile.DestroyTile(tile);
        }

        public bool Exists => DB.Exists;

        public void InitializeGeoPackage(string path)
        {
            /*
            Path = path;
            //DB = new CDB.Database(Path);
            GPKG = new GeoPackage.Database(Path);
            GPKG_Terrain = new TerrainTile(GPKG);
            if (GPKG_Terrain.Elevation == null)
                return;
            if (GeographicBounds == GeographicBounds.EmptyValue)
            {
                GeographicBounds.MinimumCoordinates.Latitude = Math.Min(GeographicBounds.MinimumCoordinates.Latitude, GPKG_Terrain.Elevation.MinY);
                GeographicBounds.MinimumCoordinates.Longitude = Math.Min(GeographicBounds.MinimumCoordinates.Longitude, GPKG_Terrain.Elevation.MinX);
                GeographicBounds.MaximumCoordinates.Latitude = Math.Max(GeographicBounds.MinimumCoordinates.Latitude, GPKG_Terrain.Elevation.MaxY);
                GeographicBounds.MaximumCoordinates.Longitude = Math.Max(GeographicBounds.MinimumCoordinates.Longitude, GPKG_Terrain.Elevation.MaxX);
            }
            if ((OriginLatitude == 0.0f) && (OriginLongitude == 0.0f))
            {
                OriginLatitude = GeographicBounds.Center.Latitude;
                OriginLongitude = GeographicBounds.Center.Longitude;
            }
            Projection = new ScaledFlatEarthProjection(new GeographicCoordinates(OriginLatitude, OriginLongitude), 0.1f);
            GPKG_Terrain.Projection = Projection;

            // campbell_dem30m is 0-4
            var lodSwitch = new LODSwitch(this);
            lodSwitch.EntryDistanceByLOD[1] = 9999; lodSwitch.ExitDistanceByLOD[1] = 99999;
            lodSwitch.EntryDistanceByLOD[2] = 999; lodSwitch.ExitDistanceByLOD[2] = 9999;
            lodSwitch.EntryDistanceByLOD[3] = 99; lodSwitch.ExitDistanceByLOD[3] = 999;
            lodSwitch.EntryDistanceByLOD[4] = 9; lodSwitch.ExitDistanceByLOD[4] = 99;
            LODSwitchByObject[GPKG_Terrain] = lodSwitch;

            {
                var childGameObject = new GameObject();
                childGameObject.transform.SetParent(gameObject.transform);
                var child = childGameObject.AddComponent<QuadTree>();
                child.Initialize(GeographicBounds);
                child.SwitchDelegate = lodSwitch.QuadTreeSwitchUpdate;
                child.LoadDelegate = GPKG_Terrain.QuadTreeDataLoad;
                child.LoadedDelegate = GPKG_Terrain.QuadTreeDataLoaded;
                child.UnloadDelegate = GPKG_Terrain.QuadTreeDataUnload;
                child.UpdateDelegate = GPKG_Terrain.QuadTreeDataUpdate;
                QuadTreeByObject[GPKG_Terrain] = new List<QuadTree>();
                QuadTreeByObject[GPKG_Terrain].Add(child);
            }
            */

        }

        public void Initialize()
        {
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
            DefineLODBrackets();
        }

        private void LogLODBrackets()
        {
            string logstr = "LOD BRACKETS:";
            foreach (var bracket in LODBrackets)
                logstr += string.Format(" {0}:({1:0.0},{2:0.0})", bracket.Key, bracket.Value.Item1, bracket.Value.Item2);
            Debug.Log(logstr);
        }

        public void SetLODBracketsForGround()
        {
            LODBrackets.Clear();
            LODBrackets.Add(-10, new ValueTuple<float, float>(float.MaxValue, float.MaxValue));
            LODBrackets.Add(-9, new ValueTuple<float, float>(1000000, 1100000));
            LODBrackets.Add(-8, new ValueTuple<float, float>(500000, 550000));
            LODBrackets.Add(-7, new ValueTuple<float, float>(250000, 275000));
            LODBrackets.Add(-6, new ValueTuple<float, float>(100000, 110000));
            LODBrackets.Add(-5, new ValueTuple<float, float>(50000, 55000));
            LODBrackets.Add(-4, new ValueTuple<float, float>(25000, 27500));
            LODBrackets.Add(-3, new ValueTuple<float, float>(10000, 22000));
            LODBrackets.Add(-2, new ValueTuple<float, float>(500, 550));
            LODBrackets.Add(-1, new ValueTuple<float, float>(300, 330));
            LODBrackets.Add(0, new ValueTuple<float, float>(300, 310));
            LODBrackets.Add(1, new ValueTuple<float, float>(200, 210));
            LODBrackets.Add(2, new ValueTuple<float, float>(100, 110));
            LODBrackets.Add(3, new ValueTuple<float, float>(80, 90));
            LODBrackets.Add(4, new ValueTuple<float, float>(60, 70));
            LODBrackets.Add(5, new ValueTuple<float, float>(40, 50));
            LODBrackets.Add(6, new ValueTuple<float, float>(30, 40));
            LODBrackets.Add(7, new ValueTuple<float, float>(20, 30));
            LODBrackets.Add(8, new ValueTuple<float, float>(15, 20));
            LogLODBrackets();
            for (int i = 9; i <= 23; ++i)
                LODBrackets.Add(i, new ValueTuple<float, float>(0, 0));
        }

        public void SetLODBracketsForDetail()
        {
            LODBrackets.Clear();
            LODBrackets.Add(-10, new ValueTuple<float, float>(float.MaxValue, float.MaxValue));
            LODBrackets.Add(-9, new ValueTuple<float, float>(100000, 110000));
            LODBrackets.Add(-8, new ValueTuple<float, float>(50000, 55000));
            LODBrackets.Add(-7, new ValueTuple<float, float>(25000, 27500));
            LODBrackets.Add(-6, new ValueTuple<float, float>(10000, 11000));
            LODBrackets.Add(-5, new ValueTuple<float, float>(5000, 5500));
            LODBrackets.Add(-4, new ValueTuple<float, float>(2500, 2750));
            LODBrackets.Add(-3, new ValueTuple<float, float>(1000, 2200));
            LODBrackets.Add(-2, new ValueTuple<float, float>(500, 550));
            LODBrackets.Add(-1, new ValueTuple<float, float>(300, 330));

#if UNITY_ANDROID
            LODBrackets.Add(0, new ValueTuple<float, float>(100, 110));
            LODBrackets.Add(1, new ValueTuple<float, float>(80, 70));
            LODBrackets.Add(2, new ValueTuple<float, float>(60, 70));
            LODBrackets.Add(3, new ValueTuple<float, float>(40, 50));
            LODBrackets.Add(4, new ValueTuple<float, float>(30, 35));
            LODBrackets.Add(5, new ValueTuple<float, float>(20, 25));
            //LODBrackets.Add(6, new ValueTuple<float, float>(15, 20));
            LogLODBrackets();
            for (int i = 6; i <= 23; ++i)
                LODBrackets.Add(i, new ValueTuple<float, float>(0, 0));
#else
            LODBrackets.Add(0, new ValueTuple<float, float>(300, 310));
            LODBrackets.Add(1, new ValueTuple<float, float>(200, 210));
            LODBrackets.Add(2, new ValueTuple<float, float>(100, 110));
            LODBrackets.Add(3, new ValueTuple<float, float>(80, 90));
            LODBrackets.Add(4, new ValueTuple<float, float>(60, 70));
            LODBrackets.Add(5, new ValueTuple<float, float>(40, 50));
            LODBrackets.Add(6, new ValueTuple<float, float>(30, 40));
            LODBrackets.Add(7, new ValueTuple<float, float>(20, 30));
            LODBrackets.Add(8, new ValueTuple<float, float>(15, 20));
            LogLODBrackets();
            for(int i = 9; i <= 23; ++i)
                LODBrackets.Add(i, new ValueTuple<float, float>(0, 0));
#endif
        }

        public void SetLODBracketsForOverview()
        {
            LODBrackets.Clear();
            LODBrackets.Add(-10, new ValueTuple<float, float>(float.MaxValue, float.MaxValue));
            LODBrackets.Add(-9, new ValueTuple<float, float>(100000, 110000));
            LODBrackets.Add(-8, new ValueTuple<float, float>(50000, 55000));
            LODBrackets.Add(-7, new ValueTuple<float, float>(25000, 27500));
            LODBrackets.Add(-6, new ValueTuple<float, float>(10000, 11000));
            LODBrackets.Add(-5, new ValueTuple<float, float>(5000, 5500));
            LODBrackets.Add(-4, new ValueTuple<float, float>(2500, 2750));
            LODBrackets.Add(-3, new ValueTuple<float, float>(1000, 2200));
            LODBrackets.Add(-2, new ValueTuple<float, float>(500, 550));
            LODBrackets.Add(-1, new ValueTuple<float, float>(250, 275));
            LogLODBrackets();
            for(int i = 0; i <= 23; ++i)
                LODBrackets.Add(i, new ValueTuple<float, float>(0, 0));
        }

        private void SetLODBracketsForDesktop()
        {
            LODBrackets.Clear();
            float minDistance = 50.0f;
            int highestLOD = 23;
            float minExponent = MaxLOD - highestLOD + 6;
            float exponent = minExponent;
            float stretch = 1.0f;
            for (int i = highestLOD; i >= -10; --i)
            {
                float exponent_mod = 1.0f; // (i > MaxLOD - 10) ? 1.0f : 0.1f;
                exponent += exponent_mod;
                float inDistance = minDistance + (float)Math.Pow(2, exponent);
                float outDistance = minDistance + (float)Math.Pow(2, exponent + 0.5f);
                if (i < MaxLOD)
                {
                    stretch *= 0.9f;
                }
                if (i > MaxLOD)
                {
                    inDistance = 0.0f;
                    outDistance = 0.0f;
                }
                inDistance *= (float)Projection.Scale * stretch;
                outDistance *= (float)Projection.Scale * stretch;
                LODBrackets.Add(i, new ValueTuple<float, float>(inDistance, outDistance));
            }
            LogLODBrackets();
        }
        
        private void DefineLODBrackets()
        {
            //SetLODBracketsForOverview();
            SetLODBracketsForGround();
            return;
#if UNITY_ANDROID
            SetLODBracketsForOverview();
#else
            SetLODBracketsForDesktop();
#endif
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
            TileDataCache.Run();
            ModelManager.Run();
        }

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

        public List<QuadTree> ActiveTilesGeoPackage()
        {
            var result = new List<QuadTree>();
            //result.AddRange(QuadTreeByObject[GPKG_Terrain][0].ActiveTiles());
            return result;
        }

        public List<Tile> ActiveTiles()
        {
            var result = new List<Tile>();
            tiles.ForEach(tile => result.AddRange(tile.ActiveTiles()));
            return result;
        }

        public int VertexCount()
        {
            int result = 0;
            foreach (var tile in ActiveTiles())
                result += tile.vertices.Length;
            return result;
        }

        public int TriangleCount()
        {
            int result = 0;
            foreach (var tile in ActiveTiles())
                result += tile.RasterDimension * tile.RasterDimension * 2;
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
            if (ManMadeData != null)
                ManMadeData.ApplyCameraPosition(position);
            if (TreeData != null)
                TreeData.ApplyCameraPosition(position);
            if (ManMadeDataSpecific != null)
                ManMadeDataSpecific.ApplyCameraPosition(position);
        }

        public void UpdateTerrain(GeographicBounds changedBounds)
        {
            EdgeCrackElimination.EliminateCracks(ActiveTiles());
            if (ManMadeData != null)
                ManMadeData.UpdateElevations(changedBounds);
            if (TreeData != null)
                TreeData.UpdateElevations(changedBounds);
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
