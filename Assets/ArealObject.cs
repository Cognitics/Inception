using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

public class ArealObject : MonoBehaviour
{
    private Areal areal;
    public GameObject pointFeatureCanvas;
    public TMP_InputField title;
    public TMP_InputField description;
    public TextMeshProUGUI location;
    public Text arealText;
    public List<Vector3> vectLocations;
    private string worldLocations;
    public Cognitics.UnityCDB.Database cdbDatabase;
    [HideInInspector] public List<Feature> featureList;
    private VertexSelector vertexSelector;
    public GameObject userObject;
    private Feature feature;
    private AttributesTable attribute;
    private Geometry geometry;
    public GameObject FilePanelCDB;
    private FilePanel_SelectCDB fpsCDB;
    private User userScript;

    public class Areal
    {
        string title;
        string description;
        Vector3[] positions;
        public Areal(string t, string d, Vector3[] pos)
        {
            title = t;
            description = d;
            positions = pos;
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

    public Areal CreateNewArealObject(string title, string description, Vector3[] pos)
    {
        return new Areal(title, description, pos);
    }
#if UNITY_ANDROID
    public void TouchSelect()
    {
        pointFeatureCanvas.SetActive(true);
        if (vertexSelector.arealFeatures.Contains(feature))
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
            if (vertexSelector.arealFeatures.Contains(feature))
            {
                title.GetComponent<TMP_InputField>().text = feature.Attributes["Title"].ToString();
                description.GetComponent<TMP_InputField>().text = feature.Attributes["Description"].ToString();
            }
            userScript.enabled = false;
        }
    }
#endif

    public void SetArealText()
    {
        arealText.text = title.GetComponent<TMP_InputField>().text;
    }

    public void DeleteArealObject()
    {
        vertexSelector.arealFeatures.Remove(feature);
        Destroy(gameObject);
        WriteToShapeFile();
        userScript.enabled = true;
    }

    public void SetArealLocation(Vector3 vect, int pos)
    {

    }

    public void SetArealLocationText()
    {
        vectLocations[0] = gameObject.transform.position;
        PositionToWorldSpace();
        location.text = worldLocations;
    }

    public void PositionToWorldSpace()
    {
        //TODO: Figure out how to show the areal's position in world space. 
    }
    
    public void Cancel()
    {
        ClearFields();
        pointFeatureCanvas.SetActive(false);
        userScript.enabled = true;

    }

    public void SaveToNTS()
    {
        SetArealText();
        WriteToNTS();
        pointFeatureCanvas.SetActive(false);
        WriteToShapeFile();

#if UNITY_STANDALONE
        userScript.enabled = true;
#endif
    }

    private void WriteToNTS()
    {
        if (vectLocations.Contains(new Vector3(0, 0, 0)))
            return;
        if (vertexSelector.arealFeatures.Contains(feature))
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
                var geographicCoordinate = cartesianCoordinates.TransformedWith(cdbDatabase.Projection);
                coord[i] = new GeoAPI.Geometries.Coordinate(geographicCoordinate.Longitude, geographicCoordinate.Latitude, v.y);
                ++i;
            }
            LinearRing lr = new LinearRing(coord);
            Polygon poly = new Polygon(lr);
            feature.Geometry = poly;

            vertexSelector.arealFeatures.Add(feature);
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
        arealText.text = "";
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
        UserShapefile.WriteFeaturesToShapefile(filepath + "/" + databaseName + "Areal", vertexSelector.arealFeatures);
        UserShapefile.WriteFeaturesToKML(filepath + "/" + databaseName, vertexSelector.AllFeaturesToList());
    }
}
