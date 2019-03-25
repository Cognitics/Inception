using System.IO;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using Cognitics.CoordinateSystems;
using Cognitics.OpenFlight;

namespace Cognitics.UnityCDB
{
    // Store off the data that needs to get passed to the newly instantianted game object
    public struct GameObjFeatureData
    {
        public string objName;
        public string fltName;
        public Vector3 position;
        public Quaternion rotation;

        public bool Equals(GameObjFeatureData data)
        {
            return //objName == data.objName &&
                   fltName == data.fltName;
        }
    }

    public class GTFeatureDataNode
    {
        public List<NetTopologySuite.Features.Feature> Features = new List<NetTopologySuite.Features.Feature>();
        public Dictionary<NetTopologySuite.Features.Feature, GameObjFeatureData> gameObjFeatureData = new Dictionary<NetTopologySuite.Features.Feature, GameObjFeatureData>();
        public List<GameObject> gameObjects = new List<GameObject>();
    }

    public class GTFeatureData
    {
        [HideInInspector] public Database Database = null;
        [HideInInspector] public CDB.VectorComponent Component = null;
        [HideInInspector] public CDB.LOD lod;

        public ConcurrentDictionary<QuadTreeNode, GTFeatureDataNode> DataByNode = new ConcurrentDictionary<QuadTreeNode, GTFeatureDataNode>();

        // If a feature uses previously unencountered FLT data, a new game object is created for it and placed in the scene, but also serves as a template for cloning of subsequent matches
        public ConcurrentDictionary<GameObjFeatureData, GameObject> gameObjCache = new ConcurrentDictionary<GameObjFeatureData, GameObject>();

        public void QuadTreeDataUpdate(QuadTreeNode node)       // QuadTreeDelegate
        {
            // TODO: any update operations (regardless of state)
        }

        public void QuadTreeDataLoad(QuadTreeNode node)         // QuadTreeDelegate
        {
            Task.Run(() => TaskLoad(node));
        }

        public void QuadTreeDataLoaded(QuadTreeNode node)       // QuadTreeDelegate
        {
            GTFeatureDataNode featureData = null;
            if (!DataByNode.TryGetValue(node, out featureData))
                return;
            Database.StartCoroutine(Loaded_CR(node, featureData));
        }

        private IEnumerator Loaded_CR(QuadTreeNode node, GTFeatureDataNode featureData)
        {
            // Copy dictionary to avoid modification during processing
            Dictionary<NetTopologySuite.Features.Feature, GameObjFeatureData> dict = new Dictionary<NetTopologySuite.Features.Feature, GameObjFeatureData>(featureData.gameObjFeatureData);

            var timer = new System.Diagnostics.Stopwatch();
            timer.Start();

            foreach (var elem in dict)
            {
                var gameObjectFeatureData = elem.Value;

                bool newObj = false;
                var origGameObj = GetOrAddGameObjectTemplate(gameObjectFeatureData, out newObj);
                // If this is a brand new object, use it; otherwise, instantiate a clone
                GameObject gameObj = newObj ? origGameObj : GameObject.Instantiate(origGameObj, node.Root.transform, false);
                gameObj.name = gameObjectFeatureData.objName;
                gameObj.transform.SetParent(node.Root.transform, false);
                gameObj.transform.localScale = (float)Database.Projection.Scale * Vector3.one;
                gameObj.transform.position = gameObjectFeatureData.position;
                gameObj.transform.rotation = gameObjectFeatureData.rotation;
                featureData.gameObjects.Add(gameObj);
                var fltDB = GetOrAddFltDB(gameObjectFeatureData.fltName);
                var flt = gameObj.GetOrAddComponent<OpenFlightRecordSet>();
                flt.Init(gameObjectFeatureData.fltName, gameObj.transform.parent);
                flt.SetDB(fltDB);
                // If this is a freshly created object, we have to generate its hierarchy, mesh and material assignment, etc.
                if (newObj)
                    flt.Finish();

                if (timer.ElapsedMilliseconds > (1000 / 60))
                {
                    timer.Stop();
                    yield return null;
                    timer.Start();
                }
            }

            //Debug.Log("numHits: " + debugNumHits);
        }

        public void QuadTreeDataUnload(QuadTreeNode node)       // QuadTreeDelegate
        {
            GTFeatureDataNode featureData = null;
            if (!DataByNode.TryGetValue(node, out featureData))
                return;

            foreach (var gameObj in featureData.gameObjects)
            {
                GameObject.Destroy(gameObj);
            }
            featureData.gameObjects.Clear();
            featureData.gameObjFeatureData.Clear();
        }

        private List<NetTopologySuite.Features.Feature> ReadFeatures(QuadTreeNode node)
        {
            var result = new List<NetTopologySuite.Features.Feature>();
            var cdbTiles = Database.DB.Tiles.Generate(node.GeographicBounds, lod + node.Depth);
            foreach (var cdbTile in cdbTiles)
            {
                var data = DataByNode[node];
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

        private void TaskLoad(QuadTreeNode node)
        {
            var featureData = new GTFeatureDataNode();
            DataByNode[node] = featureData;

            var features = ReadFeatures(node);
            foreach (var feature in features)
            {
                try
                {
                    var cnam = feature.Attributes["CNAM"].ToString();

                    string facc = feature.Attributes["FACC"].ToString();
                    int fsc = int.Parse(feature.Attributes["FSC"].ToString());
                    string modl = feature.Attributes["MODL"].ToString();
                    string subdir = Database.DB.GTModelGeometry.Subdirectory(facc);
                    var rotation = feature.Attributes["AO1"].ToString();

                    string databasePath = Database.Path;
                    string geomPath = Database.DB.GTModelGeometry.Subdirectory(facc);
                    string entryFilename = Database.DB.GTModelGeometry.GeometryEntryFile.Filename(facc, fsc, modl);
                    string str = Path.Combine(databasePath, geomPath, entryFilename + Database.DB.GTModelGeometry.GeometryEntryFile.Extension);

                    var geographicCoordinates = new GeographicCoordinates(feature.Geometry.Centroid.Y, feature.Geometry.Centroid.X);
                    var cartesianCoordinates = geographicCoordinates.TransformedWith(Database.Projection);
                    float elev = Database.TerrainElevationAtLocation(geographicCoordinates);
                    Vector3 v = new Vector3((float)cartesianCoordinates.X, elev, (float)cartesianCoordinates.Y);

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

                    var gameObjectFeatureData = new GameObjFeatureData() { objName = entryFilename, fltName = str, position = v, rotation = Quaternion.Euler(0f, heading, 0f) };
                    featureData.gameObjFeatureData[feature] = gameObjectFeatureData;
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }

            }

            node.IsLoaded = true;
            node.IsLoading = false;
        }

        //int debugNumHits = 0;

        private GameObject GetOrAddGameObjectTemplate(GameObjFeatureData gameObjFeatureData, out bool newObj)
        {
            newObj = false;
            GameObject gameObj = null;
            foreach (var elem in gameObjCache)
            {
                if (elem.Key.Equals(gameObjFeatureData)) // NOTE: uses Equals override
                {
                    //debugNumHits++;
                    gameObj = elem.Value;
                    break;
                }
            }

            if (gameObj == null)
            {
                gameObj = gameObjCache[gameObjFeatureData] = new GameObject();
                newObj = true;
            }

            return gameObj;
        }

        private FltDatabase GetOrAddFltDB(string filename)
        {
            if (!File.Exists(filename))
            {
                Debug.LogErrorFormat("could not find flt db {0}", filename);
                return null;
            }

            FltDatabase fltDB = null;
            if (!Database.FltDBs.TryGetValue(filename, out fltDB))
            {
                Stream stream = File.OpenRead(filename);
                var reader = new FltReader(stream);
                fltDB = new FltDatabase(null, RecordType.Invalid, reader, filename);
                fltDB.Parse();

                Database.FltDBs[filename] = fltDB;
            }

            return fltDB;
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
                foreach (var go in node.Value.gameObjects)
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
                result += node.Value.gameObjects.Count;
            return result;
        }


    }


}
