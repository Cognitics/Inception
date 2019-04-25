
using System;
using UnityEngine;
using UnityEngine.UI;
using SimpleFileBrowser;
using Cognitics.UnityCDB;
using Cognitics.CoordinateSystems;

#if UNITY_STANDALONE
using UnityEditor;
#endif

public class FilePanel_SelectCDB : MonoBehaviour
{
    public GameObject UserObject;
    public GameObject cdbPrefab;
    private VertexSelector vertexSelector;
    public int cdbLOD;
    public Button directoryViewButton;
    public GameObject NorthwestCDB_Buttons;
    GameObject cdbGameObject;
    Database cdbDatabase;

    public GameObject UserPositionCanvas = null;
    public GameObject OptionsPanel;

    public GameObject OptionsCanvas = null;
    public GameObject LayersCanvas = null;
    public GameObject DebugCanvas = null;
    private Text DebugPanelText = null;
    private DateTime LastDebugUpdate = DateTime.MinValue;

    private ApplicationState ApplicationState = null;

    void Awake()
    {
        ApplicationState = GameObject.Find("Application").GetComponent<ApplicationState>();
    }

    void Start()
    {
        ConsoleRedirector.Apply();
        directoryViewButton.onClick.AddListener(OnClick);
        if (DebugCanvas)
            DebugPanelText = DebugCanvas.transform.Find("DebugPanel").Find("Text").GetComponent<Text>();
        vertexSelector = UserObject.GetComponent<VertexSelector>();
	}

    void OnFolderSelected(string path)
    {
        OnFileSelect(path);
    }

    public Database GetCDBDatabase() { return cdbDatabase; }

    void OnClick()
    {
        FileBrowser.ShowLoadDialog((path) => OnFolderSelected(path),
                                        () => { Debug.Log("Canceled"); },
                                        true, null, "Select Folder", "Select");
    }

    void OnFileSelect(string path)
    {
        if (path.Length == 0)
            return;
        Destroy(cdbGameObject);

        if(false)
        {
            // TODO: testing
            OnGeoPackageSelect(path + "/muscatatuck_models3.gpkg");
            return;
        }



        cdbGameObject = Instantiate(cdbPrefab);
        cdbDatabase = cdbGameObject.GetComponent<Database>();
        if (path.Contains("northwest_cdb"))
        {
            var duetgo = GameObject.Find("DUET");
            var duet = duetgo.GetComponent<DUET>();
            duet.Database = cdbDatabase;
            cdbDatabase.GeographicBounds = new GeographicBounds(new GeographicCoordinates(47, -123), new GeographicCoordinates(48, -122));
        }

        if (ApplicationState.GeographicBounds != GeographicBounds.EmptyValue)
            cdbDatabase.GeographicBounds = ApplicationState.GeographicBounds;
        if (ApplicationState.StartPosition != Vector3.negativeInfinity)
            UserObject.transform.position = ApplicationState.StartPosition;
        if (ApplicationState.StartEulerAngles != Vector3.negativeInfinity)
            UserObject.transform.eulerAngles = ApplicationState.StartEulerAngles;
        if (ApplicationState.StartLOD != int.MinValue)
            cdbLOD = ApplicationState.StartLOD;


        cdbDatabase.Initialize(path);
        cdbDatabase.name = cdbDatabase.DB.Name;
        directoryViewButton.GetComponentsInChildren<Text>()[0].text = cdbDatabase.name;
        cdbDatabase.GenerateTerrainForLOD(cdbLOD);
        if (!cdbDatabase.Exists)
            return;

        if (cdbDatabase.name.Contains("northwest_cdb") && NorthwestCDB_Buttons != null)
            NorthwestCDB_Buttons.SetActive(true);
        UserPositionCanvas.transform.Find("PositionPanel").GetComponent<CameraPosition>().Projection = cdbDatabase.Projection;
        UserPositionCanvas.SetActive(true);
        if (OptionsCanvas != null)
        {
            OptionsCanvas.GetComponent<Options>().Database = cdbDatabase;
            OptionsCanvas.GetComponent<Options>().OnMaxLodSliderChanged();
            OptionsCanvas.GetComponent<Options>().OnSpeedSliderChanged();
        }
        if (LayersCanvas != null)
        {
            LayersCanvas.GetComponent<Layers>().Database = cdbDatabase;
        }
        UserObject.GetComponent<SurfaceCollider>().Database = cdbDatabase;
        UserObject.GetComponent<VertexSelector>().terrainTester.GetComponent<SurfaceCollider>().Database = cdbDatabase;
        GameObject dynamicLOD = GameObject.Find("DynamicLOD");
        if(dynamicLOD)
            dynamicLOD.GetComponent<DynamicLOD>().Database = cdbDatabase;
        vertexSelector.LoadAllFeatures();
    }

    void OnGeoPackageSelect(string filename)
    {
        if(filename.Length == 0)
            return;
        Destroy(cdbGameObject);
        cdbGameObject = Instantiate(cdbPrefab);
        cdbDatabase = cdbGameObject.GetComponent<Database>();
        cdbDatabase.InitializeGeoPackage(filename);


        //cdbDatabase.name = cdbDatabase.DB.Name;
        //cdbDatabase.GenerateTerrainForLOD(cdbLOD);

        /*
        UserPositionCanvas.SetActive(true);
        if (OptionsCanvas != null)
        {
            OptionsCanvas.GetComponent<Options>().Database = cdbDatabase;
            OptionsCanvas.GetComponent<Options>().OnMaxLodSliderChanged();
            OptionsCanvas.GetComponent<Options>().OnSpeedSliderChanged();
        }
        UserObject.GetComponent<SurfaceCollider>().Database = cdbDatabase;
        UserObject.GetComponent<VertexSelector>().terrainTester.GetComponent<SurfaceCollider>().Database = cdbDatabase;
        */

    }

    /*
    void Update()
    {
        CalledUpdate();
    }*/

    public void CalledUpdate()
    {
        if (!cdbDatabase)
            return;

        UpdateDebug();

        cdbDatabase.ApplyCameraPosition(UserObject.transform.position);
    }

    string ByteCountString(long byteCount)
    {
        char[] suffix = { 'B', 'K', 'M', 'G', 'T', 'P', 'E' };
        if (byteCount == 0)
            return "0B";
        long bytes = Math.Abs(byteCount);
        int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
        double num = Math.Round(bytes / Math.Pow(1024, place), 1);
        return (Math.Sign(byteCount) * num).ToString() + suffix[place];
    }

    void UpdateDebug()
    {
        if (!cdbDatabase)
            return;
        if (DebugCanvas == null)
            return;

        if (DebugCanvas.activeInHierarchy)
        {
            if ((DateTime.Now - LastDebugUpdate).TotalSeconds < 1)
                return;
            LastDebugUpdate = DateTime.Now;
            string debugText = "";
            debugText += string.Format("GC MEM: {0}\n", ByteCountString(System.GC.GetTotalMemory(false)));
            debugText += string.Format("Tex MEM: {0}\n", ByteCountString((long)Texture.currentTextureMemory));
            debugText += string.Format("TMEM ALL: {0}\n", ByteCountString(cdbDatabase.TerrainMemory(false)));
            debugText += string.Format("TMEM ACT: {0}\n", ByteCountString(cdbDatabase.TerrainMemory(true)));
            debugText += string.Format("Vertices: {0}\n", cdbDatabase.VertexCount());
            debugText += string.Format("Triangles: {0}\n", cdbDatabase.TriangleCount());
            debugText += string.Format("Models: {0}\n", cdbDatabase.ModelCount());
            debugText += string.Format("TDC Waiting: {0}\n", cdbDatabase.TileDataCache.WaitingRequests.Count);
            debugText += string.Format("TDC Running: {0}\n", cdbDatabase.TileDataCache.RunningRequests.Count);
            debugText += string.Format("TDC Entries: {0}\n", cdbDatabase.TileDataCache.Entries.Count);
            DebugPanelText.text = debugText;
        }
    }

    public void DisablePanel()
    {
        OptionsPanel.SetActive(false);
    }

    
}
