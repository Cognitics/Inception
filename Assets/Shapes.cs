using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Cognitics.CoordinateSystems;

public class Shapes : MonoBehaviour
{
    public GameObject DatabasePrefab = null;
    public GameObject UserObject = null;
    public GameObject CameraPositionPanel = null;
    public int LOD = 0;
    public string CDBPath = "D:/northwest_cdb_part1";
    public ShapeType[] ShapeTypes = new ShapeType[0];
    public Text AltitudeText = null;
    public bool ShowFeatureTextOnInit = false;
    [Serializable] public class ShapeType
    {
        public GameObject Prefab = null;
        public List<string> Paths = new List<string>();
        public string Filename = null;
        public bool Skip = false;
    }

    private Cognitics.UnityCDB.Database Database = null;

    #region MonoBehaviour

    protected void Awake()
    {
        if (DatabasePrefab != null)
        {
            var databaseGameObject = Instantiate(DatabasePrefab, transform);
            Database = databaseGameObject.GetComponent<Cognitics.UnityCDB.Database>();
            Database.Initialize(CDBPath);
            Database.name = Database.DB.Name;
        }

        foreach (var shapeType in ShapeTypes)
        {
            if (shapeType.Skip)
                continue;

            if (shapeType.Prefab != null)
            {
                foreach (var path in shapeType.Paths)
                {
                    var shapeInstance = Instantiate(shapeType.Prefab, transform, false).GetComponent<Cognitics.UnityCDB.ShapeBase>();
                    if (shapeInstance != null)
                    {
                        shapeInstance.name = path;
                        shapeInstance.transform.parent = transform;
                        shapeInstance.Database = Database;
                        shapeInstance.UserObject = UserObject;
                        shapeInstance.Path = path;
                        shapeInstance.Filename = shapeType.Filename;
                        shapeInstance.gameObject.SetActive(true);
                    }
                    else
                    {
                        Debug.LogErrorFormat("no shape component was found in prefab {0}! cannot instantiate shape instance.", shapeType.Prefab.name);
                    }
                }
            }
        }
    }

    protected void Start()
    {
        Cognitics.UnityCDB.ConsoleRedirector.Apply();

        if (Database != null)
            Database.GenerateTerrainForLOD(LOD);

        ShowFeatureText(ShowFeatureTextOnInit);
    }

    protected void Update()
    {
        if (Database != null && UserObject != null)
        {
            Database.ApplyCameraPosition(UserObject.transform.position);

            if (AltitudeText != null)
            {
                float altitudeInMeters = UserObject.transform.position.y * 10;
                AltitudeText.text = string.Format("m: {0:N2}, ft: {1:N2}, FL: {2:N2}", altitudeInMeters, altitudeInMeters * Cognitics.UnityCDB.VolumetricFeature.feetPerMeter, altitudeInMeters * Cognitics.UnityCDB.VolumetricFeature.feetPerMeter * Cognitics.UnityCDB.VolumetricFeature.flightLevelPerFoot);
            }
        }

        if (CameraPositionPanel != null)
        {
            CameraPosition campos = CameraPositionPanel.GetComponent<CameraPosition>();
            float x = UserObject.transform.position.x;
            float z = UserObject.transform.position.z;
            var cartesianCoordinates = new CartesianCoordinates(x, z);
            var geographicCoordinates = cartesianCoordinates.TransformedWith(Database.Projection);
            campos.position.x = (float)geographicCoordinates.Longitude;
            campos.position.z = (float)geographicCoordinates.Latitude;
            campos.position.y = UserObject.transform.position.y / (float)Database.Projection.Scale;
        }
    }

    #endregion

    #region UI

    public void ShowFeatureText(bool show)
    {
        var volumetricFeatures = transform.GetComponentsInChildren<Cognitics.UnityCDB.VolumetricFeature>();
        foreach (var feature in volumetricFeatures)
        {
            if (feature.textInfo != null)
                feature.textInfo.gameObject.SetActive(show);
        }
    }

    #endregion
}

//#if UNITY_EDITOR
//[CustomEditor(typeof(Shapes))]
//public class ShapesInspector : Editor
//{
//    public override void OnInspectorGUI()
//    {
//        base.OnInspectorGUI();

//        var shapes = (Shapes)target;
//    }
//}
//#endif
