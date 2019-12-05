#if UNITY_EDITOR && !UNITY_ANDROID && !UNITY_IOS
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Cognitics.UnityCDB;

public class OpenFlightTester : MonoBehaviour
{
    public delegate void WalkDelegate(string filename);

    public GameObject DatabasePrefab = null;

    private Database Database = null;
    private ModelManager modelManager = null;
    private Cognitics.Unity.MaterialManager materialManager = null;
    private MeshManager meshManager = null;
    private List<GameObject> gameObjects = new List<GameObject>();
    private Dictionary<string, Cognitics.OpenFlight.Texture> fltTextures = new Dictionary<string, Cognitics.OpenFlight.Texture>();
    private bool stop = false;
    private const int stopCount = 1000;
    private bool randomPlacement = false;

    private static string LosAngelesCDB => "D:/LosAngeles_CDB";
    private static string NorthwestCDB => "D:/northwest_cdb_part1";
    private static string YemenCDB => "D:/CDB_Yemen_4.0.0";
    private static string MuscatatuckCDB => "D:/MUTC_CDB";

    #region MonoBehaviour

    protected void Start()
    {
        ConsoleRedirector.Apply();

        //var userObject = GameObject.Find("UserObject");

        string databasePath = YemenCDB;
        var databaseGameObject = Instantiate(DatabasePrefab);
        Database = databaseGameObject.GetComponent<Database>();
        Database.Path = databasePath;
        modelManager = Database.ModelManager;
        materialManager = Database.MaterialManager;
        meshManager = Database.MeshManager;

        var path = databasePath + "/GTModel/500_GTModelGeometry";
        //var path = databasePath + "/Tiles";
        var di = new DirectoryInfo(path);
        WalkDirectoryTree(di, LoadFLT);
        int count = gameObjects.Count;
        if (count >= 1)
        {
            int width = (int)Mathf.Sqrt(count);
            int length = count / width;
            int remainder = count % width;
            int separation = 100;
            if (randomPlacement)
            {
                foreach (var gameObject in gameObjects)
                {
                    gameObject.transform.position = new Vector3(Random.Range(-width * separation / 2, width * separation / 2), 
                                                                Random.Range(-separation / 2f, separation / 2f), 
                                                                Random.Range(-length * separation / 2, length * separation / 2));
                }
            }
            else
            {
                for (int w = 0; w < width; ++w)
                {
                    for (int h = 0; h < length; ++h)
                        gameObjects[w * length + h].transform.position = new Vector3(w * separation, 0f, h * separation);
                }
                for (int r = 0; r < remainder; ++r)
                    gameObjects[width * length + r].transform.position = new Vector3(width * separation, 0f, r * separation);
            }
        }

        foreach (var gameObject in gameObjects)
            modelManager.Add(gameObject.GetComponent<Model>());

        //userObject.transform.position = new Vector3(-100f, 100f, -100f);
        //userObject.transform.rotation = Quaternion.LookRotation(new Vector3(1f, -1f, 1f), Vector3.up);
    }

    #endregion

    #region Misc

    private void WalkDirectoryTree(DirectoryInfo root, WalkDelegate walkDelegate)
    {
        if (stop)
            return;

        var files = root.GetFiles("*.flt");
        //var files = root.GetFiles("*.zip");
        if (files != null)
        {
            foreach (FileInfo fi in files)
            {
                if (stop)
                    return;

                walkDelegate(fi.FullName);
            }

            var subDirs = root.GetDirectories();
            foreach (DirectoryInfo dirInfo in subDirs)
                WalkDirectoryTree(dirInfo, walkDelegate);
        }
    }

    private void LoadFLT(string filename)
    {
        //if (!filename.Contains("D500_S001_T001_AL015_000_cdb_swa_gt_billboard_03"))
        //    return;
        //if (!filename.Contains("D500_S001_T002_AL015_000_9c71e7b4"))
        //    return;
        //if (!filename.Contains("D500_S001_T001_AL015_000_1942694a"))
        //    return;
        //if (!filename.Contains("D500_S001_T001_AL015_000_cdb_swa_gt_silo_03"))
        //    return;
        //if (!filename.Contains("silo_05"))
        //    return;
        //if (!filename.Contains("D500_S001_T001_AL130_000_cdb_swa_gt_mig17_02"))
        //    return;
        //if (!filename.Contains("D500_S001_T001_AL110_000_cdb_swa_gt_lightpole_01"))
        //    return;
        //if (!filename.Contains("D500_S001_T001_AL015_000_cdb_swa_gt_silo_01"))
        //    return;

        var gameObj = new GameObject(filename);
        var model = gameObj.AddComponent<Model>();
        model.Path = Path.GetDirectoryName(filename);
        model.ZipFilename = null;
        model.FltFilename = Path.GetFileName(filename);
        model.ModelManager = modelManager;
        model.MaterialManager = materialManager;
        model.MeshManager = meshManager;

        gameObjects.Add(gameObj);
        if (gameObjects.Count == stopCount)
            stop = true;

        // If you just want to clone a specific object a lot of times, add a filter to the top of this method, and then use this instead of the block above
        //for (int i = 0; i < stopCount; i++)
        //{
        //    var gameObj = new GameObject(filename);
        //    var model = gameObj.AddComponent<Model>();
        //    model.Path = Path.GetDirectoryName(filename);
        //    model.ZipFilename = null;
        //    model.FltFilename = Path.GetFileName(filename);
        //    model.ModelManager = modelManager;
        //    model.MaterialManager = materialManager;
        //    model.MeshManager = meshManager;
        //    gameObjects.Add(gameObj);
        //    //if (gameObjects.Count == stopCount)
        //    //    stop = true;
        //}
    }

    #endregion
}
#endif
