
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using SimpleFileBrowser;
using Cognitics.UnityCDB;
using Cognitics.CoordinateSystems;

[Serializable]
public class SetDetailMode : UnityEvent<bool> { }

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
    public SetDetailMode SetDetailMode = new SetDetailMode();

    public GameObject UserPositionCanvas = null;
    public GameObject OptionsPanel;

    public GameObject OptionsCanvas = null;
    public GameObject LayersCanvas = null;
    public GameObject DebugCanvas = null;
    private Text DebugPanelText = null;
    private float LastDebugUpdate = 0f;
    private string path;

    public ModeButton ModeButton;

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
        this.path = path;
        Destroy(cdbGameObject);
        if (ApplicationState.GlobeViewer == null)
            return;

        cdbGameObject = Instantiate(cdbPrefab);
        cdbDatabase = cdbGameObject.GetComponent<Database>();
        cdbDatabase.Initialize(path);
        if (!cdbDatabase.Exists)
        {
            Destroy(cdbGameObject);
            cdbGameObject = null;
            return;
        }
        cdbDatabase.name = cdbDatabase.DB.Name;
        directoryViewButton.GetComponentsInChildren<Text>()[0].text = cdbDatabase.name;


        if (ApplicationState.GeographicBounds != GeographicBounds.EmptyValue)
            cdbDatabase.GeographicBounds = ApplicationState.GeographicBounds;
        if (ApplicationState.StartPosition != Vector3.negativeInfinity)
            UserObject.transform.position = ApplicationState.StartPosition;
        if (ApplicationState.StartEulerAngles != Vector3.negativeInfinity)
            UserObject.transform.eulerAngles = ApplicationState.StartEulerAngles;
        if (ApplicationState.StartLOD != int.MinValue)
            cdbLOD = ApplicationState.StartLOD;

        var globeViewer = ApplicationState.GlobeViewer.GetComponent<Cognitics.Unity.BlueMarble.GlobeViewer>();
        globeViewer.CenterAndFit(
            cdbDatabase.GeographicBounds.MinimumCoordinates.Latitude, cdbDatabase.GeographicBounds.MinimumCoordinates.Longitude,
            cdbDatabase.GeographicBounds.MaximumCoordinates.Latitude, cdbDatabase.GeographicBounds.MaximumCoordinates.Longitude,
            2.0f);
        /*
        globeViewer.FitBoundsToScreen(cdbDatabase.GeographicBounds.MinimumCoordinates.Latitude, cdbDatabase.GeographicBounds.MinimumCoordinates.Longitude,
            cdbDatabase.GeographicBounds.MaximumCoordinates.Latitude, cdbDatabase.GeographicBounds.MaximumCoordinates.Longitude);
            */

        int max_lod = 6;
        int dim = (int)Math.Floor(Math.Pow(2, max_lod));

        var inventory = cdbDatabase.DB.Tiles.GeocellInventory();
        var gsta = new Cognitics.Unity.BlueMarble.GeocellSparseTextureArray(inventory, dim);
        globeViewer.Model.GetComponent<MeshRenderer>().material.SetTexture("_DensityTex", gsta.MapTexture);
        globeViewer.Model.GetComponent<MeshRenderer>().material.SetTexture("_DensityTexArray", gsta.TextureArray);

        var androidColor = new Color32(255, 255, 255, 96);
        for (int row = 0; row < 180; ++row)
        {
            for (int col = 0; col < 360; ++col)
            {
                int gsta_index = gsta.TextureIndex(row - 90, col - 180);
                if (gsta_index == 0)
                    continue;
                var bounds = new GeographicBounds(new GeographicCoordinates(row - 90, col - 180), new GeographicCoordinates(row - 90 + 1, col - 180 + 1));
                var tiles = cdbDatabase.DB.Tiles.Generate(bounds, 0);
                var ds_inventory = cdbDatabase.DB.Tiles.DatasetInventory(tiles[0], cdbDatabase.DB.Imagery.YearlyVstiRepresentation, max_lod);
                var ds_pixels = new Color32[dim * dim];
                for (int i = 0, c = dim * dim; i < c; ++i)
                {
                    byte exp = (byte)Math.Floor(Math.Pow(2, ds_inventory[i]));
                    byte value = (byte)(31 + exp);
                    ds_pixels[i] = new Color32(value, value, value, 255);
                }
                gsta.SetTexturePixels(gsta_index, ds_pixels);
#if UNITY_ANDROID
                globeViewer.GeocellTexture.Set(row - 90, col - 180, androidColor);
#endif
            }
        }
        gsta.Apply();

        //OnFileSelect2(path);
    }

    void OnFileSelect2(string path)
    {


        /*
        var yemen_hg_go = GameObject.Find("Yemen HG");
        if (yemen_hg_go != null)
        {
            var yemen_hg = yemen_hg_go.GetComponent<YemenHG>();
            yemen_hg.UDatabase = cdbDatabase;
        }
        */





        cdbDatabase.GenerateTerrainForLOD(cdbLOD);

        if (cdbDatabase.name.Contains("northwest_cdb") && NorthwestCDB_Buttons != null)
            NorthwestCDB_Buttons.SetActive(true);


        ApplicationState.cdbDatabase = cdbDatabase;


        UserPositionCanvas.transform.Find("PositionPanel").GetComponent<CameraPosition>().Projection = cdbDatabase.Projection;
        UserPositionCanvas.SetActive(true);
        if (LayersCanvas != null)
        {
            LayersCanvas.GetComponent<Layers>().Database = cdbDatabase;
        }
        if (OptionsCanvas != null)
        {
            OptionsCanvas.GetComponent<Options>().Database = cdbDatabase;
            OptionsCanvas.GetComponent<Options>().OnSpeedSliderChanged();
        }
        UserObject.GetComponent<SurfaceCollider>().Database = cdbDatabase;
        UserObject.GetComponent<VertexSelector>().terrainTester.GetComponent<SurfaceCollider>().Database = cdbDatabase;
        vertexSelector.LoadAllFeatures();

        /*
        var mage_go = GameObject.Find("MAGE");
        if(mage_go)
        {
            var mage = mage_go.GetComponent<MAGE>();
            mage.WorldPlane.SetOrigin(cdbDatabase.OriginLatitude, cdbDatabase.OriginLongitude);
            mage.Database = cdbDatabase;
        }
        */
    }

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
            if (Time.time - LastDebugUpdate < 1f)
                return;
            LastDebugUpdate = Time.time;
            string debugText = "";
            debugText += string.Format("SYSMEMF: {0:0.000}\n", cdbDatabase.SystemMemoryUtilization);
            debugText += string.Format("SHMEMF: {0:0.000}\n", cdbDatabase.SharedMemoryUtilization);
            debugText += string.Format("TMATM MEM: {0}\n", ByteCountString(cdbDatabase.TerrainMaterialManager.Memory()));
            debugText += string.Format("MMATM MEM: {0}\n", ByteCountString(cdbDatabase.ModelMaterialManager.Memory()));
            debugText += string.Format("MSHM MEM: {0}\n", ByteCountString(cdbDatabase.MeshManager.Memory()));
            debugText += string.Format("GC MEM: {0}\n", ByteCountString(System.GC.GetTotalMemory(false)));
            debugText += string.Format("Tex MEM: {0}\n", ByteCountString((long)Texture.currentTextureMemory));
            debugText += string.Format("TMEM ALL: {0}\n", ByteCountString(cdbDatabase.TerrainMemory(false)));
            debugText += string.Format("TMEM ACT: {0}\n", ByteCountString(cdbDatabase.TerrainMemory(true)));
            debugText += string.Format("Vertices: {0}\n", cdbDatabase.VertexCount());
            debugText += string.Format("Triangles: {0}\n", cdbDatabase.TriangleCount());
            debugText += string.Format("TDC Waiting: {0}\n", cdbDatabase.TileDataCache.WaitingRequests.Count);
            debugText += string.Format("TDC Running: {0}\n", cdbDatabase.TileDataCache.RunningRequests.Count);
            debugText += string.Format("TDC Entries: {0}\n", cdbDatabase.TileDataCache.Entries.Count);
            if (cdbDatabase.ManMadeDataSpecific != null)
            {
                debugText += string.Format("GSS Features: {0}\n", cdbDatabase.ManMadeDataSpecific.FeatureCount());
                debugText += string.Format("GSS Models: {0}\n", cdbDatabase.ManMadeDataSpecific.ModelCount());
            }
            if (cdbDatabase.ManMadeData != null)
            {
                debugText += string.Format("GTS Features: {0}\n", cdbDatabase.ManMadeData.FeatureCount());
                debugText += string.Format("GTS Models: {0}\n", cdbDatabase.ManMadeData.ModelCount());
            }
            if (cdbDatabase.TreeData != null)
            {
                debugText += string.Format("GTT Features: {0}\n", cdbDatabase.TreeData.FeatureCount());
                debugText += string.Format("GTT Models: {0}\n", cdbDatabase.TreeData.ModelCount());
            }
            debugText += string.Format("Valid Models: {0}\n", cdbDatabase.ModelManager.ValidCount);
            DebugPanelText.text = debugText;
        }
    }

    public void DisablePanel()
    {
        OptionsPanel.SetActive(false);
    }

    public void HandleGlobeClick(GameObject sender, double lat, double lon)
    {
        if (cdbDatabase == null)
            return;
        if (lat < cdbDatabase.GeographicBounds.MinimumCoordinates.Latitude)
            return;
        if (lat > cdbDatabase.GeographicBounds.MaximumCoordinates.Latitude)
            return;
        if (lon < cdbDatabase.GeographicBounds.MinimumCoordinates.Longitude)
            return;
        if (lon > cdbDatabase.GeographicBounds.MaximumCoordinates.Longitude)
            return;

        ModeButton.UpdateState(2);
        OnFileSelect2(path);
        var geographicCoordinates = new GeographicCoordinates(lat, lon);
        var cartesianCoordinates = geographicCoordinates.TransformedWith(cdbDatabase.Projection);
        UserObject.transform.position = new Vector3((float)cartesianCoordinates.X, 20f, (float)cartesianCoordinates.Y);
        cdbDatabase.SetLODBracketsForDetail();
        SetDetailMode.Invoke(true);
        if (Input.touchSupported)
            OptionsCanvas.GetComponent<Options>().uiControlsCanvas.SetActive(OptionsCanvas.GetComponent<Options>().uiControlsCheckmark.activeSelf);
        else
            OptionsCanvas.GetComponent<Options>().uiControlsCheckmark.SetActive(false);

        sender.SetActive(false);
    }
    
}
