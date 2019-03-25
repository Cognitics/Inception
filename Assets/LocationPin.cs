using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System.IO;
using System;
using UnityEngine.EventSystems;

public class LocationPin : MonoBehaviour
{
    //private LocationPinObject lpo;
    public GameObject pointFeatureCanvas;
    public TMP_InputField title;
    public TMP_InputField description;
    public TextMeshProUGUI location;
    public Text pinText;
    public Vector3 vectLocation;
    private string worldLocation;
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

    //public class LocationPinObject
    //{
    //    string title;
    //    string description;
    //    Vector3 position;

    //    public LocationPinObject(string t, string d, Vector3 pos)
    //    {
    //        title = t;
    //        description = d;
    //        position = pos;
    //    }
    //}

    public void Start()
    {
        vertexSelector = userObject.GetComponent<VertexSelector>();
        feature = new Feature();
        attribute = new AttributesTable();
        fpsCDB = FilePanelCDB.GetComponent<FilePanel_SelectCDB>();
        cdbDatabase = fpsCDB.GetCDBDatabase();
        userScript = userObject.GetComponent<User>();
        SaveToNTS();
    }

    //public LocationPinObject CreateNewPinObject(string title, string description, Vector3 pos)
    //{
    //    return new LocationPinObject(title, description, pos);
    //}
#if UNITY_ANDROID

    public void TouchSelect()
    {
        pointFeatureCanvas.SetActive(true);
        if (vertexSelector.pinFeatures.Contains(feature))
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
            if (vertexSelector.pinFeatures.Contains(feature))
            {
                title.GetComponent<TMP_InputField>().text = feature.Attributes["Title"].ToString();
                description.GetComponent<TMP_InputField>().text = feature.Attributes["Description"].ToString();
            }
            userScript.enabled = false;
        }
    }
#endif



    public void SetPinText()
    {
        pinText.text = title.text;
        SetPinLocationText();
    }

    public void DeletePinObject()
    {
        vertexSelector.pinFeatures.Remove(feature);
        Destroy(gameObject);
        WriteToShapeFile();
        userScript.enabled = true;
    }

    public void SetPinLocation(Vector3 vect)
    {
        vectLocation = vect;
    }
    public void SetPinLocationText()
    {
        vectLocation = gameObject.transform.position;
        PositionToWorldSpace();
        location.text = worldLocation;
    }
    public void PositionToWorldSpace()
    {
        if (vectLocation == null)
            return;
        float x = vectLocation.x;
        float z = vectLocation.z;
        var cartesianCoordinates = new Cognitics.CoordinateSystems.CartesianCoordinates(x, z);
        var geographicCoordinates = cartesianCoordinates.TransformedWith(cdbDatabase.Projection);
        x = (float)geographicCoordinates.Longitude;
        z = (float)geographicCoordinates.Latitude;

        string latitudeString = (z < 0) ? string.Format("S{0:##0.0000}", -z) : string.Format("N{0:##0.0000}", z);
        string longitudeString = (x < 0) ? string.Format("W{0:###0.0000}", -x) : string.Format("E{0:###0.0000}", x);
        string str = string.Format("{0} {1}   {2:###0.00}", latitudeString, longitudeString, vectLocation.y);

        worldLocation = str;
    }

    public void Cancel()
    {
        ClearFields();
        pointFeatureCanvas.SetActive(false);
        userScript.enabled = true;
    }

    public void SaveToNTS()
    {
        SetPinText();
        WriteToNTS();
        pointFeatureCanvas.SetActive(false);
        WriteToShapeFile();
#if UNITY_STANDALONE
        userScript.enabled = true;
#endif
        //ClearFields();

    }

    public void WriteToNTS()
    {
        var cartesianCoordinates = new Cognitics.CoordinateSystems.CartesianCoordinates(vectLocation.x, vectLocation.z);
        var geographicCoordinate = cartesianCoordinates.TransformedWith(cdbDatabase.Projection);
        Geometry g = new Point(geographicCoordinate.Longitude, geographicCoordinate.Latitude, vectLocation.y);
        if (vertexSelector.pinFeatures.Contains(feature))
        {
            attribute["Title"] = title.GetComponent<TMP_InputField>().text;
            attribute["Description"] = description.GetComponent<TMP_InputField>().text;
            feature.Attributes = attribute;
        }
        else
        {
            attribute.Add("Title", title.text);
            attribute.Add("Description", description.text);
            feature.Attributes = attribute;
            feature.Geometry = g;
            vertexSelector.pinFeatures.Add(feature);
        }
        SetPinText();
    }

    public void ClearFields()
    {
        title.GetComponent<TMP_InputField>().text = "";
        description.GetComponent<TMP_InputField>().text = "";
        
    }

    public void ClearTitle()
    {
        title.text = "";
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
        UserShapefile.WriteFeaturesToShapefile(filepath + "/" + databaseName + "Point", vertexSelector.pinFeatures);
        UserShapefile.WriteFeaturesToKML(filepath + "/" + databaseName, vertexSelector.AllFeaturesToList());
    }
}
