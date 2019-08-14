
using Cognitics.UnityCDB;
using UnityEngine;
using UnityEngine.UI;
using NetTopologySuite;
using NetTopologySuite.Features;
public class PointofInterest : MonoBehaviour
{
    public GameObject panel;
    public Button buttonPrefab;
    public GameObject contentPane;
    public GameObject cdbButton;
    public Database database;
    private string filepath;
    private string title = "Title";
    private bool isFirstTimeLoaded = true;

    private void Start()
    {
        panel.SetActive(false);
    }

    public void PanelController()
    {
        panel.SetActive(!panel.activeSelf);
        if (panel.activeSelf == false)
            RemoveAllButtons();
        if (panel.activeSelf == true)
            LoadPoints();
        
    }

    private void LoadPoints()
    {
        database = cdbButton.GetComponent<FilePanel_SelectCDB>().GetCDBDatabase();
        if (database == null)
            return;
#if UNITY_ANDROID
        filepath = UnityEngine.Application.persistentDataPath;
#else
        filepath = database.Path;
#endif
        database = cdbButton.GetComponent<FilePanel_SelectCDB>().GetCDBDatabase();
        string databasename = database.name;
        databasename = databasename.Replace('.', '_');
        string name = filepath + "/" + databasename + "POI.shp";
        var feats = Cognitics.CDB.Shapefile.ReadFeatures(name);
        foreach(Feature f in feats)
        {
            GeoAPI.Geometries.Coordinate[] coords = f.Geometry.Coordinates;
            var geoCoords = new Cognitics.CoordinateSystems.GeographicCoordinates();
            geoCoords.Longitude = coords[0].X;
            geoCoords.Latitude = coords[0].Y;
            var cartCoords = geoCoords.TransformedWith(database.Projection);

            var poi = buttonPrefab.GetComponent<POIButton>();
            poi.locationPosition = new Vector3((float)cartCoords.X, (float)coords[0].Z, (float)cartCoords.Y);
            poi.buttonText = f.Attributes[title].ToString();
            Instantiate(buttonPrefab, contentPane.transform);
        }
        //for (int i = 0; i < 20; ++i)
        //   Instantiate(buttonPrefab, contentPane.transform);
        isFirstTimeLoaded = false;
    }

    public void RemoveAllButtons()
    {
        foreach (Transform child in contentPane.transform)
            Destroy(child.gameObject);
    }
}
