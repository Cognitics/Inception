using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cognitics.UnityCDB;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using TMPro;

public class VertexSelector : MonoBehaviour
{
    Camera cam;
    Vector3 mouse;
    Ray ray;
    bool HasCollided = false;
    public GameObject terrainTester;
    public GameObject locationPin;
    public GameObject FilePanelButton;
    public Button PinToggle;
    public Button lineButton;
    public Button polyButton;
    public GameObject dot;
    public GameObject lineObject;
    public GameObject arealObject;
    public GameObject look;
    private Toggle toggle;
    
    AddPin pinScript;
    PolygonButton polyScript;
    LineButton lineScript;
    Database cdbDatabase;

    [HideInInspector] public List<Vector3> linePoints;
    [HideInInspector] public List<Vector3> polyPoints;
    [HideInInspector] public List<GameObject> dots;

    [HideInInspector] public List<Feature> pinFeatures;
    [HideInInspector] public List<Feature> lineFeatures;
    [HideInInspector] public List<Feature> arealFeatures;
    
    FilePanel_SelectCDB fpsCDB;
    SurfaceCollider ttScript;
    SurfaceCollider CamSurfaceCollider;
    float time;
    float lerpValue;
    float speed = 2f;
    Touch touch;
    void Start()
    {
        cam = Camera.main;
        ttScript = terrainTester.GetComponent<SurfaceCollider>();
        touch = new Touch();
        fpsCDB = FilePanelButton.GetComponent<FilePanel_SelectCDB>();
        pinScript = PinToggle.GetComponent<AddPin>();
        lineScript = lineButton.GetComponent<LineButton>();
        polyScript = polyButton.GetComponent<PolygonButton>();

        linePoints = new List<Vector3>();
        polyPoints = new List<Vector3>();
        dots = new List<GameObject>();

        pinFeatures = new List<Feature>();
        lineFeatures = new List<Feature>();
        arealFeatures = new List<Feature>();
        toggle = look.GetComponent<Toggle>();

    }
    void Update()
    {
        fpsCDB.CalledUpdate();
#if UNITY_ANDROID

        if(Input.touchCount > 0)
        {
            if (Input.GetTouch(0).phase == TouchPhase.Began && pinScript.buttonSelected)
            {
                if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                    return;
                var loc = ThrowTerrainTester();
                if (loc == new Vector3(0, 0, 0))
                    return;
                locationPin.SetActive(true);
                Instantiate(locationPin, loc, Quaternion.identity);
                locationPin.SetActive(false);
                pinScript.DisableButton();
            }

            if (Input.GetTouch(0).phase == TouchPhase.Began && lineScript.buttonSelected)
            {
                if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                    return;
                if (Input.touchCount == 1)
                {
                    var loc = ThrowTerrainTester();
                    dot.SetActive(true);
                    dots.Add(Instantiate(dot, loc, Quaternion.identity));
                    dot.SetActive(false);
                    loc.y = loc.y + 10f;
                    linePoints.Add(loc);
                }
            }
            if (Input.GetTouch(0).phase == TouchPhase.Began && polyScript.buttonSelected)
            {
                if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                    return;
                if (Input.touchCount == 1)
                {
                    var loc = ThrowTerrainTester();
                    dot.SetActive(true);
                    dots.Add(Instantiate(dot, loc, Quaternion.identity));
                    dot.SetActive(false);
                    polyPoints.Add(loc);
                }
            }
        }

#else

        if (Input.GetKeyDown(KeyCode.Mouse0) && pinScript.buttonSelected && !EventSystem.current.IsPointerOverGameObject())
        {
            
            var loc = ThrowTerrainTester();
            locationPin.GetComponent<LocationPin>().ClearFields();
            locationPin.SetActive(true);
            Instantiate(locationPin, loc, Quaternion.identity);
            Debug.Log("LocationPin Title:" + locationPin.GetComponent<LocationPin>().title.GetComponent<TMP_InputField>().text);
            locationPin.SetActive(false);
            pinScript.DisableButton();
        }

        if(Input.GetKeyDown(KeyCode.Mouse0) && lineScript.buttonSelected && !EventSystem.current.IsPointerOverGameObject())
        {
            var loc = ThrowTerrainTester();
            dot.SetActive(true);
            dots.Add(Instantiate(dot, loc, Quaternion.identity));
            dot.SetActive(false);
            loc.y = loc.y + 1f;
            linePoints.Add(loc);
        }

        if(Input.GetKeyDown(KeyCode.Mouse0) && polyScript.buttonSelected && !EventSystem.current.IsPointerOverGameObject())
        {
            var loc = ThrowTerrainTester();
            dot.SetActive(true);
            dots.Add(Instantiate(dot, loc, Quaternion.identity));
            dot.SetActive(false);
            loc.y = loc.y + 10f;
            polyPoints.Add(loc);
        }
#endif

        if (pinScript.buttonSelected || lineScript.buttonSelected || polyScript.buttonSelected)
            toggle.enabled = false;
        else
            toggle.enabled = true;
    }

    public void ClickedButton()
    {
        
    }

    private Vector3 ThrowTerrainTester()
    {
#if UNITY_ANDROID
        ray = cam.ScreenPointToRay(Input.GetTouch(0).position);
#else
        ray = cam.ScreenPointToRay(Input.mousePosition);
#endif
        mouse = ray.direction;
        terrainTester.transform.position = cam.transform.position;
        while (!HasCollided)
        {
            terrainTester.transform.position += mouse * (Time.deltaTime * 15f);
            terrainTester.GetComponent<SurfaceCollider>().TerrainElevationGetter();
            if (terrainTester.transform.position.y < terrainTester.GetComponent<SurfaceCollider>().minCameraElevation)
                HasCollided = true;
            if (Vector3.Distance(terrainTester.transform.position, cam.transform.position) > 50000f)
                return new Vector3(0,0,0);
        }
        HasCollided = false;
        return terrainTester.transform.position;
    }

    private void MoveToNearestVertex(Vector3[] vertices)
    {
        Vector3 position = terrainTester.transform.position;
        float minDist = float.MaxValue;
        foreach(Vector3 vert in vertices)
        {
            if(Vector3.Distance(position, vert) < minDist)
            {
                minDist = Vector3.Distance(position, vert);
                terrainTester.transform.position = vert;
            }
        }
    }

    private void LoadPointFeatures()
    {
        cdbDatabase = fpsCDB.GetCDBDatabase();
#if UNITY_ANDROID
        string filepath = UnityEngine.Application.persistentDataPath;
#else
        string filepath = cdbDatabase.Path;
#endif
        cdbDatabase = fpsCDB.GetCDBDatabase();
        string databaseName = cdbDatabase.name;
        databaseName = databaseName.Replace('.', '_');

        var feats = Cognitics.CDB.Shapefile.ReadFeatures(filepath + "/" + databaseName + "point.shp");
        foreach(Feature f in feats)
        {
            GeoAPI.Geometries.Coordinate[] coords = f.Geometry.Coordinates;
            var geoCoords = new Cognitics.CoordinateSystems.GeographicCoordinates();
            geoCoords.Longitude = coords[0].X;
            geoCoords.Latitude = coords[0].Y;
            var cartCoords = geoCoords.TransformedWith(cdbDatabase.Projection);
            Vector3 loc = new Vector3((float)cartCoords.X, (float)coords[0].Z, (float)cartCoords.Y);
            LocationPin lp = locationPin.GetComponent<LocationPin>();
            lp.title.GetComponent<TMP_InputField>().text = f.Attributes["Title"].ToString();
            lp.description.GetComponent<TMP_InputField>().text = f.Attributes["Description"].ToString();
            lp.location.GetComponent<TextMeshProUGUI>().text = f.Geometry.ToString();
            lp.SetPinText();
            locationPin.SetActive(true);
            Instantiate(locationPin, loc, Quaternion.identity);
            locationPin.SetActive(false);
            lp.ClearFields();
        }
    }

    private void LoadLinearFeatures()
    {
        cdbDatabase = fpsCDB.GetCDBDatabase();
#if UNITY_ANDROID
        string filepath = UnityEngine.Application.persistentDataPath;
#else
        string filepath = cdbDatabase.Path;
#endif
        cdbDatabase = fpsCDB.GetCDBDatabase();
        string databaseName = cdbDatabase.name;
        databaseName = databaseName.Replace('.', '_');

        var feats = Cognitics.CDB.Shapefile.ReadFeatures(filepath + "/" + databaseName + "Line.shp");
        if (linePoints.Count != 0)
            linePoints.Clear();
        foreach(Feature f in feats)
        {
            GeoAPI.Geometries.Coordinate[] coords = f.Geometry.Coordinates;
            foreach(GeoAPI.Geometries.Coordinate c in coords)
            {
                var geoCoords = new Cognitics.CoordinateSystems.GeographicCoordinates();
                geoCoords.Longitude = c.X;
                geoCoords.Latitude = c.Y;
                var cartCoords = geoCoords.TransformedWith(cdbDatabase.Projection);
                linePoints.Add(new Vector3((float)cartCoords.X, (float)c.Z, (float)cartCoords.Y));                
            }
            LineObject lo = lineObject.GetComponent<LineObject>();
            lo.title.GetComponent<TMP_InputField>().text = f.Attributes["Title"].ToString();
            lo.description.GetComponent<TMP_InputField>().text = f.Attributes["Description"].ToString();
            lo.SetLineText();
            
            lineScript.DrawLine(ref lineObject);
        }

    }

    private void LoadArealFeatures()
    {
        cdbDatabase = fpsCDB.GetCDBDatabase();
#if UNITY_ANDROID

        string filepath = UnityEngine.Application.persistentDataPath;
#else
        string filepath = cdbDatabase.Path;
#endif
        string databaseName = cdbDatabase.name;
        databaseName = databaseName.Replace('.', '_');
        var feats = Cognitics.CDB.Shapefile.ReadFeatures(filepath + "/" + databaseName + "Areal.shp");
        if (polyPoints.Count != 0)
            polyPoints.Clear();
        foreach (Feature f in feats)
        {
            GeoAPI.Geometries.Coordinate[] coords = f.Geometry.Coordinates;
            foreach (GeoAPI.Geometries.Coordinate c in coords)
            {
                var geoCoords = new Cognitics.CoordinateSystems.GeographicCoordinates();
                geoCoords.Longitude = c.X;
                geoCoords.Latitude = c.Y;
                var cartCoords = geoCoords.TransformedWith(cdbDatabase.Projection);
                polyPoints.Add(new Vector3((float)cartCoords.X, (float)c.Z, (float)cartCoords.Y));
            }
            ArealObject ao = arealObject.GetComponent<ArealObject>();
            ao.title.GetComponent<TMP_InputField>().text = f.Attributes["Title"].ToString();
            ao.description.GetComponent<TMP_InputField>().text = f.Attributes["Description"].ToString();
            ao.SetArealText();
            polyScript.DrawPoly(ref arealObject);
        }

    }
    public void LoadAllFeatures()
    {
        LoadPointFeatures();
        LoadArealFeatures();
        LoadLinearFeatures();
    }

    public List<Feature> AllFeaturesToList()
    {
        List<Feature> AllFeatures = new List<Feature>();
        if (pinFeatures.Count > 0)
            AllFeatures.AddRange(pinFeatures);
        if (lineFeatures.Count > 0)
            AllFeatures.AddRange(lineFeatures);
        if (arealFeatures.Count > 0)
            AllFeatures.AddRange(arealFeatures);

        return AllFeatures;
    }
}
