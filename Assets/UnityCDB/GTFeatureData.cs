using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using Cognitics.CoordinateSystems;

namespace Cognitics.UnityCDB
{
    public class GTFeatureDataNode
    {
        public List<NetTopologySuite.Features.Feature> Features = new List<NetTopologySuite.Features.Feature>();
        public Dictionary<NetTopologySuite.Features.Feature, Vector3> PositionByFeature = new Dictionary<NetTopologySuite.Features.Feature, Vector3>();
        public List<GameObject> GameObjects = new List<GameObject>();
        public Vector3 CameraPosition;

        public int CompareByDistance2(NetTopologySuite.Features.Feature a, NetTopologySuite.Features.Feature b)
        {
            if (a == b)
                return 0;
            if (a == null)
                return 1;
            if (b == null)
                return -1;

            Vector3 avec = Vector3.zero, bvec = Vector3.zero;
            bool afound = PositionByFeature.TryGetValue(a, out avec);
            bool bfound = PositionByFeature.TryGetValue(b, out bvec);

            if (!afound && !bfound)
                return 0;
            if (!afound)
                return 1;
            if (!bfound)
                return -1;

            float adist = (avec - CameraPosition).sqrMagnitude;
            float bdist = (bvec - CameraPosition).sqrMagnitude;
            return adist.CompareTo(bdist);
        }

        public int CompareByDistance2_Reverse(NetTopologySuite.Features.Feature a, NetTopologySuite.Features.Feature b)
        {
            int result = CompareByDistance2(a, b);
            return -1 * result;
        }
    }

    public class GTFeatureData
    {
        [HideInInspector] public Database Database = null;
        [HideInInspector] public CDB.VectorComponent Component = null;
        [HideInInspector] public CDB.LOD lod;
        [HideInInspector] public int MinLOD = 4;

        public ConcurrentDictionary<QuadTreeNode, GTFeatureDataNode> DataByNode = new ConcurrentDictionary<QuadTreeNode, GTFeatureDataNode>();

        //////////////////////////////////////////////////////////////////////////////// 

        public void QuadTreeDataLoad(QuadTreeNode node)         // QuadTreeDelegate
        {
            var token = node.TaskLoadToken.Token;
            Task t = Task.Run(() => { TaskLoad(node); }, token);
        }

        private void TaskLoad(QuadTreeNode node)
        {
            try
            {
                var featureData = new GTFeatureDataNode();
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
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            node.IsLoaded = true;
            node.IsLoading = false;
        }

        private List<NetTopologySuite.Features.Feature> ReadFeatures(QuadTreeNode node)
        {
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
                if(Component.PointClassAttributes.Exists(cdbTile))
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
                result.AddRange(tileFeatures);
            }
            return result;
        }

        public void QuadTreeDataLoaded(QuadTreeNode node)       // QuadTreeDelegate
        {
            var featureData = DataByNode[node];

            // RemoveDuplicateModels(node);

            Debug.LogFormat("[GTFeatureData:{0}] Loaded: {1} features.", node.Depth, featureData.Features.Count);
        }

        private void RemoveDuplicateModels(QuadTreeNode node)
        {
            var GameObjectsToDestroy = new List<GameObject>();
            var AncestorNodes = DataByNode.Keys.ToList();

            AncestorNodes.RemoveAll(n => DataByNode[n].Features.Count == 0);
            AncestorNodes.RemoveAll(n => n.Depth >= node.Depth);
            AncestorNodes = GetNodesWithOverlappingBounds(node, AncestorNodes);

            foreach (var AncestorNode in AncestorNodes)
            {
                foreach (var featurePosition in DataByNode[node].PositionByFeature)
                {
                    var GameObjects = DataByNode[AncestorNode].GameObjects;
                    foreach (GameObject go in GameObjects)
                    {
                        if (featurePosition.Value.x == go.transform.position.x && featurePosition.Value.z == go.transform.position.z)
                        {
                            var model = go.GetComponent<Model>();
                            DataByNode[AncestorNode].Features.Add(model.Feature);
                            UnityEngine.Object.Destroy(model);
                            GameObjectsToDestroy.Add(go);
                        }
                    }
                }

                for (int i = 0; i < GameObjectsToDestroy.Count; ++i)
                {
                    DataByNode[AncestorNode].GameObjects.Remove(GameObjectsToDestroy[i]);
                    UnityEngine.Object.Destroy(GameObjectsToDestroy[i]);
                }

                GameObjectsToDestroy.Clear();
            }
        }

        private List<QuadTreeNode> GetNodesWithOverlappingBounds(QuadTreeNode node, List<QuadTreeNode> candidates)
        {
            if (candidates.Count == 0)
                return candidates;

            List<QuadTreeNode> NodesWithOverlappingBounds = new List<QuadTreeNode>();

            Vector2 nodeBoundsMax = new Vector2((float)node.GeographicBounds.MaximumCoordinates.Latitude, (float)node.GeographicBounds.MaximumCoordinates.Longitude);
            Vector2 nodeBoundsMin = new Vector2((float)node.GeographicBounds.MinimumCoordinates.Latitude, (float)node.GeographicBounds.MinimumCoordinates.Longitude);


            foreach (var candidate in candidates)
            {
                Vector2 candidateBoundsMax = new Vector2((float)candidate.GeographicBounds.MaximumCoordinates.Latitude, (float)candidate.GeographicBounds.MaximumCoordinates.Longitude);
                Vector2 candidateBoundsMin = new Vector2((float)candidate.GeographicBounds.MinimumCoordinates.Latitude, (float)candidate.GeographicBounds.MinimumCoordinates.Longitude);

                if (BoundsOverlap(candidateBoundsMin, candidateBoundsMax, nodeBoundsMin, nodeBoundsMax))
                    NodesWithOverlappingBounds.Add(candidate);
            }

            return NodesWithOverlappingBounds;
        }

        private bool BoundsOverlap(Vector2 BottomLeft1, Vector2 TopRight1, Vector2 BottomLeft2, Vector2 TopRight2)
        {
            if (TopRight2.x < BottomLeft1.x || TopRight1.x < BottomLeft2.x)
                return false;

            if (TopRight2.y < BottomLeft1.y || TopRight1.y < BottomLeft2.y)
                return false;

            return true;
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

            if (Database.SystemMemoryLimitExceeded)
                return;

            //if (HasLoadedDecendents(node))
            //    return;

            var maxdist = Database.LODSwitchByObject[this].MaxDistance * Database.Projection.Scale;

            GTFeatureDataNode featureData = null;
            if (!DataByNode.TryGetValue(node, out featureData))
                return;
            for (int i = 0; i < 1; ++i)
            {
                if (featureData.Features.Count < 1)
                    return;

                int index = featureData.Features.Count - 1;
                var feature = featureData.Features[index];
                var dist2 = (featureData.PositionByFeature[feature] - featureData.CameraPosition).sqrMagnitude;
                if (dist2 > maxdist * maxdist)
                    return;

                lock (featureData.Features)
                {
                    featureData.Features.RemoveAt(index);
                }

                try
                {
                    var modelGameObject = GenerateModel(node, feature);
                    if (modelGameObject != null)
                    {
                        featureData.GameObjects.Add(modelGameObject);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

        private bool HasLoadedDecendents(QuadTreeNode node)
        {
            if (node.ChildrenLoaded())
                return true;
            else
                foreach (var child in node.Children)
                    if (HasLoadedDecendents(child))
                        return true;

            return false;
        }

        GameObject GenerateModel(QuadTreeNode node, NetTopologySuite.Features.Feature feature)
        {
            var featureData = DataByNode[node];

            var cnam = feature.Attributes["CNAM"].ToString();
            string facc = feature.Attributes["FACC"].ToString();
            int fsc = int.Parse(feature.Attributes["FSC"].ToString());
            string modl = feature.Attributes["MODL"].ToString();
            string subdir = Database.DB.GTModelGeometry.Subdirectory(facc);
            var rotation = feature.Attributes["AO1"].ToString();

            string geomPath = Database.DB.GTModelGeometry.Subdirectory(facc);
            string entryFilename = Database.DB.GTModelGeometry.GeometryEntryFile.Filename(facc, fsc, modl);
            string fltFilename = Path.Combine(Database.Path, geomPath, entryFilename + Database.DB.GTModelGeometry.GeometryEntryFile.Extension);

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
            model.Path = Path.GetDirectoryName(fltFilename);
            model.ZipFilename = null;
            model.FltFilename = Path.GetFileName(fltFilename);
            model.ModelManager = Database.ModelManager;
            model.MaterialManager = Database.MaterialManager;
            model.MeshManager = Database.MeshManager;
            model.Feature = feature;
            gameObj.transform.SetParent(node.Root.transform, false);
            gameObj.transform.localScale = (float)Database.Projection.Scale * Vector3.one;
            gameObj.transform.position = featureData.PositionByFeature[feature];
            gameObj.transform.rotation = Quaternion.Euler(0f, heading, 0f);
            model.ModelManager.Add(model);
            model.StartCoroutine("Load");
            return gameObj;
        }

        ////////////////////////////////////////////////////////////////////////////////

        public void QuadTreeDataUnload(QuadTreeNode node)       // QuadTreeDelegate
        {
            GTFeatureDataNode featureData = null;
            if (!DataByNode.TryGetValue(node, out featureData))
                return;
            foreach (var gameObj in featureData.GameObjects)
                GameObject.Destroy(gameObj);
            lock (featureData.Features)
            {
                featureData.Features.Clear();
            }
            featureData.GameObjects.Clear();
        }

        public void Unload()
        {
            foreach (var node in DataByNode.Keys)
            {
                node.TaskLoadToken.Cancel();
                node.TaskLoadToken.Dispose();
                QuadTreeDataUnload(node);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////


        public void ApplyCameraPosition(Vector3 position)
        {
            try
            {
                foreach (var kv in DataByNode)
                {
                    kv.Value.CameraPosition = position;
                    if (kv.Key.IsActive)
                    {
                        lock (kv.Value.Features)
                        {
                            kv.Value.Features.Sort(kv.Value.CompareByDistance2_Reverse);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
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

        public int FeatureCount()
        {
            int result = 0;
            foreach (var node in DataByNode)
                result += node.Value.Features.Count;
            return result;
        }


    }


}
