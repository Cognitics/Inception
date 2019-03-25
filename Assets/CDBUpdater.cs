
using System;
using UnityEngine;
using UnityEngine.UI;
using SimpleFileBrowser;
using Cognitics.UnityCDB;
using Cognitics.CoordinateSystems;

public class CDBUpdater : MonoBehaviour
{
    public GameObject UserObject;

    public GameObject UserPositionCanvas = null;
    private CameraPosition cameraPosition = null;
    public GameObject OptionsPanel;
    GameObject cdbGameObject;
    Database cdbDatabase;
    public GameObject OptionsCanvas = null;
    public GameObject DebugCanvas = null;
    private Text DebugPanelText = null;
    private DateTime LastDebugUpdate = DateTime.MinValue;

    void Start()
    {
        if (DebugCanvas)
            DebugPanelText = DebugCanvas.transform.Find("DebugPanel").Find("Text").GetComponent<Text>();
        if (UserPositionCanvas)
            cameraPosition = UserPositionCanvas.transform.Find("PositionPanel").GetComponent<CameraPosition>();
    }


    void Update()
    {
        if (!cdbDatabase)
            return;

        UpdatePosition();

        UpdateDebug();

        cdbDatabase.ApplyCameraPosition(UserObject.transform.position);
    }

    void UpdatePosition()
    {
        if (UserPositionCanvas == null)
            return;
        if (cameraPosition == null)
            return;
        if (!UserPositionCanvas.activeInHierarchy)
            return;
        float x = UserObject.transform.position.x;
        float z = UserObject.transform.position.z;
        var cartesianCoordinates = new CartesianCoordinates(x, z);
        var geographicCoordinates = cartesianCoordinates.TransformedWith(cdbDatabase.Projection);
        cameraPosition.position.x = (float)geographicCoordinates.Longitude;
        cameraPosition.position.z = (float)geographicCoordinates.Latitude;
        cameraPosition.position.y = UserObject.transform.position.y / (float)cdbDatabase.Projection.Scale;
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
            debugText += string.Format("Vertices: {0}\n", cdbDatabase.VertexCount());
            debugText += string.Format("Triangles: {0}\n", cdbDatabase.TriangleCount());
            DebugPanelText.text = debugText;
        }
    }

}
