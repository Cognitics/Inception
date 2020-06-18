using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

public class LineObject : MonoBehaviour
{
    private Line line;
    public GameObject pointFeatureCanvas;
    public TMP_InputField title;
    public TMP_InputField description;
    public TextMeshProUGUI location;
    public Text lineText;
    public List<Vector3> vectLocations;
    private string worldLocation = "";
    private Cognitics.UnityCDB.Database cdbDatabase;
    [HideInInspector] public List<Feature> featureList;
    private VertexSelector vertexSelector;
    public GameObject userObject;
    private Feature feature;
    private AttributesTable attribute;
    private Geometry geometry;
    public GameObject FilePanelCDB;
    private FilePanel_SelectCDB fpsCDB;
    private User userScript;
    public GameObject drone;
    //Drone droneScript;

    public class Line
    {
        string title;
        string description;
        Vector3 position;

        public Line(string t, string d, Vector3 pos)
        {
            title = t;
            description = d;
            position = pos;
        }
    }
    void Start()
    {
        vertexSelector = userObject.GetComponent<VertexSelector>();
        feature = new Feature();
        attribute = new AttributesTable();
        fpsCDB = FilePanelCDB.GetComponent<FilePanel_SelectCDB>();
        cdbDatabase = fpsCDB.GetCDBDatabase();
        userScript = userObject.GetComponent<User>();
        SaveToNTS();
    }

    public Line CreateNewLineObject(string title, string description, Vector3 pos)
    {
        return new Line(title, description, pos);
    }

    //TODO: Add a way to select this Line
#if UNITY_ANDROID
    public void TouchSelect()
    {
        pointFeatureCanvas.SetActive(true);
        if (vertexSelector.lineFeatures.Contains(feature))
        {
            title.GetComponent<TMP_InputField>().text = feature.Attributes["Title"].ToString();
            description.GetComponent<TMP_InputField>().text = feature.Attributes["Description"].ToString();
        }
    }
#else
    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(0))
        {
            pointFeatureCanvas.SetActive(true);
            if (vertexSelector.lineFeatures.Contains(feature))
            {
                title.GetComponent<TMP_InputField>().text = feature.Attributes["Title"].ToString();
                description.GetComponent<TMP_InputField>().text = feature.Attributes["Description"].ToString();
            }
            userScript.enabled = false;
        }

    }
#endif
    public void SetLineText()
    {
        lineText.text = title.GetComponent<TMP_InputField>().text;
        //SetLineLocationText();
    }

    public void DeleteLineObject()
    {
        vertexSelector.lineFeatures.Remove(feature);
        Destroy(gameObject);
        WriteToShapeFile();
        userScript.enabled = true;
    }

    public void SetLineLocation(Vector3 vect, int pos)
    {
        vectLocations[pos] = vect;
    }

    public void SetLineLocationText()
    {
        vectLocations[0] = gameObject.transform.position;
        PositionToWorldSpace();
        location.text = worldLocation;
    }

    public void Cancel()
    {
        ClearFields();
        pointFeatureCanvas.SetActive(false);
#if UNITY_STANDALONE
        userScript.enabled = true;
#endif
    }

    public void PositionToWorldSpace()
    {

        //TODO: Figure out how to show the line's position in worldspace. 
        /*
        if (vectLocations[0] == null)
            return;
        float x = vectLocations[0].x;
        float z = vectLocations[0].z;
        var cartesianCoordinates = new Cognitics.CoordinateSystems.CartesianCoordinates(x, z);
        var geographicCoordinates = cartesianCoordinates.TransformedWith(cdbDatabase.Projection);
        vectLocations[0].x = (float)geographicCoordinates.Longitude;
        vectLocations[0].z = (float)geographicCoordinates.Latitude;

        string latitudeString = (vectLocation.z < 0) ? string.Format("S{0:##0.0000}", -vectLocation.z) : string.Format("N{0:##0.0000}", vectLocation.z);
        string longitudeString = (vectLocation.x < 0) ? string.Format("W{0:###0.0000}", -vectLocation.x) : string.Format("E{0:###0.0000}", vectLocation.x);
        string str = string.Format("{0} {1}   {2:###0.00}", latitudeString, longitudeString, vectLocation.y);

        worldLocation = str;*/
    }

    public void SaveToNTS()
    {
        SetLineText();
        WriteToNTS();
        pointFeatureCanvas.SetActive(false);
        WriteToShapeFile();
#if UNITY_STANDALONE
        userScript.enabled = true;
#endif

    }

    public void WriteToNTS()
    {
        if (vectLocations.Contains(new Vector3(0, 0, 0)))
            return;
        if (vertexSelector.lineFeatures.Contains(feature))
        {
            attribute["Title"] = title.GetComponent<TMP_InputField>().text;
            attribute["Description"] = description.GetComponent<TMP_InputField>().text;
            feature.Attributes = attribute;
        }
        else
        {
            attribute.Add("Title", title.GetComponent<TMP_InputField>().text);
            attribute.Add("Description", description.GetComponent<TMP_InputField>().text);
            feature.Attributes = attribute;
            GeoAPI.Geometries.Coordinate[] coord = new GeoAPI.Geometries.Coordinate[vectLocations.Count];
            int i = 0;
            foreach (Vector3 v in vectLocations)
            {

                var cartesianCoordinates = new Cognitics.CoordinateSystems.CartesianCoordinates(v.x, v.z);
                var geographicCoordinates = cartesianCoordinates.TransformedWith(cdbDatabase.Projection);
                coord[i] = new GeoAPI.Geometries.Coordinate(geographicCoordinates.Longitude, geographicCoordinates.Latitude, v.y);
                ++i;
            }

            LineString ls = new LineString(coord);
            feature.Geometry = ls;
            vertexSelector.lineFeatures.Add(feature);
        }
        ClearFields();
    }

    public void ClearFields()
    {
        title.GetComponent<TMP_InputField>().text = "";
        description.GetComponent<TMP_InputField>().text = "";
    }

    public void ClearTitle()
    {
        lineText.text = "";
    }

    private void WriteToShapeFile()
    {
        string databaseName = cdbDatabase.name;
        databaseName = databaseName.Replace('.', '_');
#if UNITY_ANDROID
        string filepath = UnityEngine.Application.persistentDataPath;
#else
        string filepath = cdbDatabase.Path;
#endif
        UserShapefile.WriteFeaturesToShapefile(filepath + "/" + databaseName + "Line", vertexSelector.lineFeatures);
        UserShapefile.WriteFeaturesToKML(filepath + "/" + databaseName, vertexSelector.AllFeaturesToList());
    }

    /*
    public void StartDrone()
    {
        droneScript = drone.GetComponent<Drone>();
        droneScript.path = vectLocations;
        droneScript.lo = this;
        Instantiate(drone, vectLocations[0], Quaternion.identity);
        gameObject.GetComponent<LineRenderer>().enabled = false;
        pointFeatureCanvas.SetActive(false);
#if UNITY_STANDALONE
        userScript.enabled = true;
#endif
    }
    */

    public void EnableLineRenderer()
    {
        gameObject.GetComponent<LineRenderer>().enabled = true;
    }

}
