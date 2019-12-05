using Cognitics.CoordinateSystems;
using Cognitics.UnityCDB;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AddObservation : MonoBehaviour
{
    public GameObject TerrainTester;
    public GameObject MageObject;

    private MAGE MageScript;
    private bool buttonSelected;
    Color buttonColor;
    string buttonString;

    private void Start()
    {
        buttonColor = gameObject.GetComponent<Image>().color;
        buttonString = gameObject.GetComponentInChildren<Text>().text;
        MageScript = MageObject.GetComponent<MAGE>();
    }

    private void Update()
    {
        if (buttonSelected && Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            PlaceObservation();
    }

    public void SwitchButtonState()
    {
        var button = gameObject.GetComponent<Button>();
        buttonSelected = !buttonSelected;
        if(buttonSelected)
        {
            button.GetComponent<Image>().color = button.colors.pressedColor;
            button.GetComponentInChildren<Text>().text = "Click to place a new Observation";
        }
        else
        {
            button.GetComponent<Image>().color = buttonColor;
            button.GetComponentInChildren<Text>().text = buttonString;
        }
    }

    public void PlaceObservation()
    {
        var pos = ThrowTerrainTester();

        float x = pos.x;
        float z = pos.z;
        var cartesianCoordinates = new Cognitics.CoordinateSystems.CartesianCoordinates(x, z);
        var geoCoords = cartesianCoordinates.TransformedWith(ApplicationState.Instance.cdbDatabase.Projection);
        MageScript.AddObservation(geoCoords.Latitude, geoCoords.Longitude);

        gameObject.GetComponent<Button>().GetComponent<Image>().color = buttonColor;
        gameObject.GetComponent<Button>().GetComponentInChildren<Text>().text = buttonString;
        buttonSelected = false;
    }

    private Vector3 ThrowTerrainTester()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var rayDirection = ray.direction;
        var surfaceCollider = TerrainTester.GetComponent<SurfaceCollider>();
        TerrainTester.transform.position = Camera.main.transform.position;
        bool hasCollided = false;
        while(!hasCollided)
        {
            TerrainTester.transform.position += rayDirection * (Time.deltaTime * 15f);
            surfaceCollider.TerrainElevationGetter();
            if (TerrainTester.transform.position.y < surfaceCollider.minCameraElevation)
                hasCollided = true;
            if (Vector3.Distance(TerrainTester.transform.position, Camera.main.transform.position) > 50000f)
                return Vector3.zero;
        }
        return TerrainTester.transform.position;
    }
}
