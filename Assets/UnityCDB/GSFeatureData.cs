using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using Cognitics.CoordinateSystems;

namespace Cognitics.UnityCDB
{
    public class GSFeatureDataNode
    {
        public List<NetTopologySuite.Features.Feature> Features = new List<NetTopologySuite.Features.Feature>();
        public Dictionary<NetTopologySuite.Features.Feature, CDB.Tile> CDBTileByFeature = new Dictionary<NetTopologySuite.Features.Feature, CDB.Tile>();
        public Dictionary<NetTopologySuite.Features.Feature, Vector3> PositionByFeature = new Dictionary<NetTopologySuite.Features.Feature, Vector3>();
        public List<GameObject> GameObjects = new List<GameObject>();
        public Vector3 CameraPosition;
        public List<GameObject> LoadingGameObjects = new List<GameObject>();

        public int CompareByDistance2(NetTopologySuite.Features.Feature a, NetTopologySuite.Features.Feature b)
        {
            var adist = (PositionByFeature[a] - CameraPosition).sqrMagnitude;
            var bdist = (PositionByFeature[b] - CameraPosition).sqrMagnitude;
            return adist.CompareTo(bdist);
        }

    }

    public class GSFeatureData
    {
        [HideInInspector] public Database Database = null;
        [HideInInspector] public CDB.VectorComponent Component = null;
        [HideInInspector] public CDB.LOD lod;
        [HideInInspector] public int MinLOD = 0;

        public ConcurrentDictionary<QuadTreeNode, GSFeatureDataNode> DataByNode = new ConcurrentDictionary<QuadTreeNode, GSFeatureDataNode>();

        //////////////////////////////////////////////////////////////////////////////// 
        public void QuadTreeDataLoad(QuadTreeNode node)         // QuadTreeDelegate
        {
            Task.Run(() => TaskLoad(node));
        }

        private void TaskLoad(QuadTreeNode node)
        {
            try
            {
                var featureData = new GSFeatureDataNode();
                DataByNode[node] = featureData;
                featureData.Features = ReadFeatures(node);
                foreach (var feature in featureData.Features)
                {
                    var geographicCoordinates = new GeographicCoordinates(feature.Geometry.Centroid.Y, feature.Geometry.Centroid.X);
                    var cartesianCoordinates = geographicCoordinates.TransformedWith(Database.Projection);
                    float elev = Database.TerrainElevationAtLocation(geographicCoordinates);
                    Vector3 position = new Vector3((float)cartesianCoordinates.X, elev, (float)cartesianCoordinates.Y);
                    featureData.PositionByFeature[feature] = position;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }

            node.IsLoaded = true;
            node.IsLoading = false;
        }

        private List<NetTopologySuite.Features.Feature> ReadFeatures(QuadTreeNode node)
        {
            var featureData = DataByNode[node];

            var result = new List<NetTopologySuite.Features.Feature>();

            if (node.Depth < MinLOD)
                return result;

            var cdbTiles = Database.DB.Tiles.Generate(node.GeographicBounds, lod + node.Depth);
            foreach (var cdbTile in cdbTiles)
            {
                var tileFeatures = Component.PointFeatures.Exists(cdbTile) ? Component.PointFeatures.Read(cdbTile) : null;
                if (tileFeatures == null)
                    continue;
                if (tileFeatures.Count == 0)
                    continue;
                if (Component.PointClassAttributes.Exists(cdbTile))
                {
                    var attr = Component.PointClassAttributes.Read(cdbTile);
                    foreach (var feature in tileFeatures)
                    {
                        var cnam = feature.Attributes["CNAM"].ToString();
                        foreach (var name in attr[cnam].GetNames())
                        {
                            if (feature.Attributes.Exists(name))
                                feature.Attributes[name] = attr[cnam][name];
                            else
                                feature.Attributes.AddAttribute(name, attr[cnam][name]);
                        }
                    }
                }
                // TODO: xattr
                foreach(var feature in tileFeatures)
                    featureData.CDBTileByFeature[feature] = cdbTile;
                result.AddRange(tileFeatures);
            }
            return result;
        }

        public void QuadTreeDataLoaded(QuadTreeNode node)       // QuadTreeDelegate
        {
            var featureData = DataByNode[node];
            Debug.LogFormat("[GSFeatureData:{0}] Loaded: {1} features.", node.Depth, featureData.Features.Count);
        }

        ////////////////////////////////////////////////////////////////////////////////

        public void QuadTreeDataUpdate(QuadTreeNode node)       // QuadTreeDelegate
        {
#if UNITY_EDITOR
            if (node.IsActive)
            {
                var sw = node.GeographicBounds.MinimumCoordinates;
                var ne = node.GeographicBounds.MaximumCoordinates;
                var se = new GeographicCoordinates(sw.Latitude, ne.Longitude);
                var nw = new GeographicCoordinates(ne.Latitude, sw.Longitude);
                var swc = sw.TransformedWith(Database.Projection);
                var sec = se.TransformedWith(Database.Projection);
                var nwc = nw.TransformedWith(Database.Projection);
                var nec = ne.TransformedWith(Database.Projection);
                var elev = 100.0f;
                var swv = new Vector3((float)swc.X, elev, (float)swc.Y);
                var sev = new Vector3((float)sec.X, elev, (float)sec.Y);
                var nwv = new Vector3((float)nwc.X, elev, (float)nwc.Y);
                var nev = new Vector3((float)nec.X, elev, (float)nec.Y);
                Debug.DrawLine(swv, sev);
                Debug.DrawLine(swv, nwv);
                Debug.DrawLine(nwv, nev);
                Debug.DrawLine(sev, nev);
            }
#endif

            if (!node.IsActive)
                return;

            if (!node.IsLoaded)
                return;

            GSFeatureDataNode featureData = null;
            if (!DataByNode.TryGetValue(node, out featureData))
                return;
            for (int i = 0; i < 1; ++i)
            {
                if (featureData.Features.Count < 1)
                    return;

                if (Time.frameCount % 10 != 0)
                {
                    var tempList = new List<GameObject>();
                    foreach (var go in featureData.LoadingGameObjects)
                    {
                        if (!go.GetComponent<Model>().Loaded)
                            tempList.Add(go);
                    }
                    featureData.LoadingGameObjects = tempList;
                }

                if (featureData.LoadingGameObjects.Count > 4)
                    return;

                var feature = featureData.Features[0];
                var dist2 = (featureData.PositionByFeature[feature] - featureData.CameraPosition).sqrMagnitude;
                if (dist2 > 100.0f * 100.0f)
                    return;

                featureData.Features.RemoveAt(0);

                try
                {
                    var modelGameObject = GenerateModel(node, feature);
                    featureData.GameObjects.Add(modelGameObject);
                    featureData.LoadingGameObjects.Add(modelGameObject);
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        GameObject GenerateModel(QuadTreeNode node, NetTopologySuite.Features.Feature feature)
        {
            var featureData = DataByNode[node];

            var cnam = feature.Attributes["CNAM"].ToString();
            string facc = feature.Attributes["FACC"].ToString();
            int fsc = int.Parse(feature.Attributes["FSC"].ToString());
            string modl = feature.Attributes["MODL"].ToString();
            var rotation = feature.Attributes["AO1"].ToString();

            var cdbTile = featureData.CDBTileByFeature[feature];
            string fullZipPath = Database.DB.GSModelGeometry.GeometryArchive.Filename(cdbTile);
            string zipPath = Path.GetDirectoryName(fullZipPath);
            string zipFilename = Path.GetFileName(fullZipPath);
            string fltFilename = Database.DB.GSModelGeometry.GeometryArchive.Filename(cdbTile, facc, fsc, modl) + ".flt";

            float heading = 0f;
            if (float.TryParse(rotation, out heading))
            {
                if (heading < 0f || heading >= 360f)
                    Debug.LogWarningFormat("feature rotation: {0} is outside of established range [0-360)", heading);
            }
            else
            {
                Debug.LogError("could not find/parse rotation of feature - using default rotation");
            }

            var gameObj = new GameObject(modl);
            var model = gameObj.AddComponent<Model>();
            model.Path = Path.GetDirectoryName(fullZipPath);
            model.ZipFilename = zipFilename;
            model.FltFilename = Path.GetFileName(fltFilename);
            model.ModelManager = Database.ModelManager;
            model.ModelManager.Models[model] = new ModelEntry();
            model.MaterialManager = Database.MaterialManager;
            model.MeshManager = Database.MeshManager;
            gameObj.transform.SetParent(node.Root.transform, false);
            gameObj.transform.localScale = (float)Database.Projection.Scale * Vector3.one;
            gameObj.transform.position = featureData.PositionByFeature[feature];
            gameObj.transform.rotation = Quaternion.Euler(0f, heading, 0f);
            return gameObj;
        }

        ////////////////////////////////////////////////////////////////////////////////

        public void QuadTreeDataUnload(QuadTreeNode node)       // QuadTreeDelegate
        {
            GSFeatureDataNode featureData = null;
            if (!DataByNode.TryGetValue(node, out featureData))
                return;
            foreach (var gameObj in featureData.GameObjects)
                GameObject.Destroy(gameObj);
            featureData.Features.Clear();
            featureData.CDBTileByFeature.Clear();
            featureData.GameObjects.Clear();
        }

        public void Unload()
        {
            foreach (var node in DataByNode.Keys)
                QuadTreeDataUnload(node);
        }

        ////////////////////////////////////////////////////////////////////////////////

        public void ApplyCameraPosition(Vector3 position)
        {
            if (Time.frameCount % 60 != 0)
                return;
            foreach (var kv in DataByNode)
            {
                kv.Value.CameraPosition = position;
                if (kv.Key.IsActive)
                    kv.Value.Features.Sort(kv.Value.CompareByDistance2);
            }
        }

        public void UpdateElevations(GeographicBounds bounds)
        {
            // iterate through all models in bounds and update elevation
            var coords = new CartesianCoordinates();
            foreach(var node in DataByNode)
            {
                if (node.Key.GeographicBounds.MinimumCoordinates.Latitude > bounds.MaximumCoordinates.Latitude)
                    continue;
                if (node.Key.GeographicBounds.MinimumCoordinates.Longitude > bounds.MaximumCoordinates.Longitude)
                    continue;
                if (node.Key.GeographicBounds.MaximumCoordinates.Latitude < bounds.MinimumCoordinates.Latitude)
                    continue;
                if (node.Key.GeographicBounds.MaximumCoordinates.Longitude < bounds.MinimumCoordinates.Longitude)
                    continue;
                foreach (var go in node.Value.GameObjects)
                {
                    var position = go.transform.position;
                    coords.X = position.x;
                    coords.Y = position.z;
                    var location = coords.TransformedWith(Database.Projection);

                    float elev = Database.TerrainElevationAtLocation(location);
                    go.transform.position = new Vector3(position.x, elev, position.z);
                }
            }
        }

        public int ModelCount()
        {
            int result = 0;
            foreach (var node in DataByNode)
                result += node.Value.GameObjects.Count;
            return result;
        }



    }


}
