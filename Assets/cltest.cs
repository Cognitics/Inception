using UnityEngine;
using Cognitics.UnityCDB;
//using Fbx;

public class cltest : MonoBehaviour
{
    public string FLTPath = "D:/OpenFlight";
    public string FLTFile = "flight0_0.flt";
    //public string FBXPath = "D:/FBXTestData/Bench";
    //public string FBXFile = "bench_01.fbx";
    //public GameObject UserObject = null;

    public GameObject DatabasePrefab;
    public GameObject UserObject;
    public int LOD;
    Database Database;

    static string LosAngelesCDB => "D:/LosAngeles_CDB";
    static string NorthwestCDB1 => "D:/northwest_cdb_part1";
    static string NorthwestCDB2 => "D:/northwest_cdb_part2";
    static string YemenCDB => "D:/Yemen_3_0";

    #region MonoBehaviour

    protected void Awake()
    {
        GameObject databaseGameObject = Instantiate(DatabasePrefab, transform);
        Database = databaseGameObject.GetComponent<Cognitics.UnityCDB.Database>();
        //Database.GeographicBounds = new Cognitics.CDB.GeographicBounds(new Cognitics.CDB.GeographicCoordinates(45, -124), new Cognitics.CDB.GeographicCoordinates(46, -123));
        Database.Initialize(NorthwestCDB1);
        Database.name = Database.DB.Name;
    }

    protected void Start()
    {
        ConsoleRedirector.Apply();

        string filename = string.Format("{0}/{1}", FLTPath, FLTFile);
        var flt = gameObject.AddComponent<OpenFlightRecordSet>();
        flt.Init(filename, transform);
        flt.Parse();
        flt.Finish();

        //string filename = string.Format("{0}/{1}", FBXPath, FBXFile);
        //var fbx = FbxIO.ReadAscii(filename);
    }

    //protected void Start()
    //{
    //    ConsoleRedirector.Apply();

    //    Database.GenerateTerrainForLOD(LOD);
    //    //Database.InitializeCDBFeatures(0, LOD + 2);
    //    //Database.InitializeCDBFeatures(1, LOD + 2);
    //}

    protected void Update()
    {
        Database.ApplyCameraPosition(UserObject.transform.position);
    }

    #endregion
}
