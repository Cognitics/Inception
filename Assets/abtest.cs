using System;
using System.Diagnostics;
using UnityEngine;
using Cognitics.CoordinateSystems;

public class abtest : MonoBehaviour
{
    public GameObject DatabasePrefab;
    public GameObject UserObject;
    public int LOD;
    Cognitics.UnityCDB.Database Database;

    static string LosAngelesCDB => "D:/LosAngeles_CDB";
    static string NorthwestCDB1 => "D:/northwest_cdb_part1";
    static string NorthwestCDB2 => "D:/northwest_cdb_part2";
    static string YemenCDB => "D:/Yemen_3_0";

    Material LineMaterial;

    Stopwatch stopWatch = new Stopwatch();
    void StartTimer() { stopWatch.Reset(); stopWatch.Start(); }
    void StopTimer(string name)
    {
        stopWatch.Stop();
        TimeSpan ts = stopWatch.Elapsed;
        UnityEngine.Debug.Log(String.Format("{0}: {1:00}:{2:00}:{3:00}.{4:0000}", name, ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10));
    }


    void Awake()
    {
        GameObject databaseGameObject = Instantiate(DatabasePrefab, transform);
        Database = databaseGameObject.GetComponent<Cognitics.UnityCDB.Database>();
        //Database.GeographicBounds = new Cognitics.CDB.GeographicBounds(new Cognitics.CDB.GeographicCoordinates(45, -124), new Cognitics.CDB.GeographicCoordinates(46, -123));
        Database.Initialize(LosAngelesCDB);
        Database.name = Database.DB.Name;
    }

    void LARoads()
    {
        var tiles = Database.DB.Tiles.Generate(Database.GeographicBounds, 4);
        tiles.ForEach(tile =>
        {
            if (Database.DB.RoadNetwork.Roads.LinealFeatures.Exists(tile))
            {
                var features = Database.DB.RoadNetwork.Roads.LinealFeatures.Read(tile);
                features.ForEach(f => AddFeature(f, Color.red, 2.0f));
            }
        });
    }

    void LAPowerLines()
    {
        var tiles = Database.DB.Tiles.Generate(Database.GeographicBounds, 3);
        tiles.ForEach(tile =>
        {
            if (Database.DB.PowerLineNetwork.PowerLines.LinealFeatures.Exists(tile))
            {
                var features = Database.DB.PowerLineNetwork.PowerLines.LinealFeatures.Read(tile);
                features.ForEach(f => AddFeature(f, Color.yellow, 3.0f));
            }
        });
    }

    void AddFeature(NetTopologySuite.Features.Feature feature, Color color, float elev)
    {
        Vector3[] points = null;
        if (feature.Geometry is NetTopologySuite.Geometries.LineString)
        {
            var linear = (NetTopologySuite.Geometries.LineString)feature.Geometry;
            if (!linear.IsValid)
                return;
            points = new Vector3[linear.Count];
            for (int i = 0; i < points.Length; ++i)
            {
                var geographicCoordinates = new GeographicCoordinates(linear[i].Y, linear[i].X);
                var cartesianCoordinates = geographicCoordinates.TransformedWith(Database.Projection);
                points[i] = new Vector3((float)cartesianCoordinates.X, elev, (float)cartesianCoordinates.Y);
            }
        }
        if (points == null)
            return;
        var featureGameObject = new GameObject();
        featureGameObject.transform.SetParent(Database.gameObject.transform);
        LineRenderer lineRenderer = featureGameObject.AddComponent<LineRenderer>();
        lineRenderer.positionCount = points.Length;
        lineRenderer.SetPositions(points);
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.sharedMaterial = LineMaterial;
    }

    void Start()
    {
        Cognitics.UnityCDB.ConsoleRedirector.Apply();

        //if (LoadFLTs)
            //LAGTFeature();

        //Database.InitializeCDBFeatures(0, LOD + 2);
        //Database.InitializeCDBFeatures(1, LOD + 2);


        //Stream fltStream = File.Open("test.flt", FileMode.Open);
        //var flt = new Cognitics.OpenFlight.Reader(fltStream);



        /*
        Database.GenerateTerrainForLOD(LOD);
        LineMaterial = GetComponent<LineRenderer>().materials[0];
        LARoads();
        LAPowerLines();

        if(false)
        {
            var db = new Cognitics.CDB.Database("D:/northwest_cdb_part2");
            var tiles = db.Tiles.Generate(db.ExistingBounds(), 0);
            tiles.ForEach(tile =>
            {
                if (!db.GeoPolitical.Location.PolygonFeatures.Exists(tile))
                    return;
                var features = db.GeoPolitical.Location.PolygonFeatures.Read(tile);

                Console.WriteLine(features.Count);
            });
        }
        if (false)
        {
            var db = new Cognitics.CDB.Database("D:/LosAngeles_CDB");
            var tiles = db.Tiles.Generate(db.ExistingBounds(), 3);
            tiles.ForEach(tile =>
            {
                if (!db.RoadNetwork.Roads.LinealFeatures.Exists(tile))
                    return;
                var features = db.RoadNetwork.Roads.LinealFeatures.Read(tile);

                Console.WriteLine(features.Count);
            });
        }
        if (false)
        {
            var db = new Cognitics.CDB.Database("D:/LosAngeles_CDB");
            var tiles = db.Tiles.Generate(db.ExistingBounds(), 0);
            tiles.ForEach(tile =>
            {
                int featureCount = 0;
                int classAttributeCount = 0;
                if (db.GSFeature.ManMade.PointFeatures.Exists(tile))
                {
                    var features = db.GSFeature.ManMade.PointFeatures.Read(tile);
                    featureCount = features.Count;
                }
                if (db.GSFeature.ManMade.PointClassAttributes.Exists(tile))
                {
                    var classAttributes = db.GSFeature.ManMade.PointClassAttributes.Read(tile);
                    classAttributeCount = classAttributes.Count;
                }
                Console.WriteLine(string.Format("[{0}] GSFeature.ManMade featureCount={1} classAttributes={2}", tile.Name, featureCount, classAttributeCount));
            });
        }
        */


        /*
        var features_fn = "D:/northwest_cdb_part2/Tiles/N45/W120/102_GeoPolitical/L00/U0/N45W120_D102_S002_T005_L00_U0_R0.shp";
        var class_fn = "D:/northwest_cdb_part2/Tiles/N45/W120/102_GeoPolitical/L00/U0/N45W120_D102_S002_T006_L00_U0_R0.dbf";
        Console.WriteLine(features_fn);
        var features = Cognitics.CDB.Shapefile.ReadFeatures(features_fn);
        Cognitics.CDB.Shapefile.DumpFeatures(features);
        Console.WriteLine(class_fn);
        var class_attr = Cognitics.CDB.Shapefile.ReadClassAttributes(class_fn);
        foreach (var k in class_attr.Keys)
            Cognitics.CDB.Shapefile.DumpAttributes(k, class_attr[k]);
            */

        return;
        /*
        if (!Database.Exists)
            return;
        Database.GenerateTerrainForLOD(LOD);
        */
    }

    ////////////////////////////////////////////////////////////

    void Update()
    {
        Database.ApplyCameraPosition(UserObject.transform.position);
    }




}
