using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class PolygonButton : MonoBehaviour
{
    public Button addPinButton;
    public GameObject arealObject;
    public Button lineButton;
    public Button polyButton;
    private LineRenderer lr;
    public GameObject camera;
    private VertexSelector vs;
    public Material mat;
    private ArealObject arealObjectScript;
    

    [HideInInspector] public bool buttonSelected = false;
    Color buttonColor;
    string buttonString;
    string buttonStringClicked = "Finish Areal (3+ points)";
    Text buttonText;

    void Start()
    {
        polyButton = gameObject.GetComponent<Button>();
        buttonText = polyButton.GetComponentInChildren<Text>();
        buttonColor = polyButton.GetComponent<Image>().color;
        buttonString = buttonText.text;
        vs = camera.GetComponent<VertexSelector>();
        arealObjectScript = arealObject.GetComponent<ArealObject>();
    }

    public void SwitchButtonState()
    {
        buttonSelected = !buttonSelected;
        lineButton.GetComponent<LineButton>().DisableButton();
        addPinButton.GetComponent<AddPin>().DisableButton();
        if (buttonSelected)
        {
            polyButton.GetComponent<Image>().color = polyButton.colors.pressedColor;
            buttonText.text = buttonStringClicked;
        }

        else
        {
            polyButton.GetComponent<Image>().color = buttonColor;
            buttonText.text = buttonString;
            DrawPoly();
        }
    }

    public void DisableButton()
    {
        buttonSelected = false;
        buttonText.text = buttonString;
        polyButton.GetComponent<Image>().color = buttonColor;
        foreach (GameObject g in vs.dots)
            Destroy(g);
        vs.polyPoints.Clear();
    }

    private void DrawPoly()
    {
        if (vs.polyPoints.Count < 3)
        {
            vs.polyPoints.Clear();
            return;
        }
        vs.polyPoints.Add(vs.polyPoints[0]);
        arealObject.transform.position = vs.polyPoints[0];
        arealObject.AddComponent<LineRenderer>();
        arealObject.GetComponent<ArealObject>().ClearFields();
        arealObject.GetComponent<ArealObject>().ClearTitle();
        lr = arealObject.GetComponent<LineRenderer>();
        arealObjectScript.vectLocations = vs.polyPoints;
        lr.material = mat;
        lr.positionCount = vs.polyPoints.Count;
        lr.startColor = new Color(0, 1, 0);
        lr.endColor = new Color(0, 1, 0);
        lr.startWidth = 10f;
        lr.endWidth = 10f;
        lr.numCapVertices = 5; //Smooths out the line angles. 
        int i = 0;
        foreach (Vector3 v in vs.polyPoints)
        {
            lr.SetPosition(i, v);
            ++i;
        }
        foreach (GameObject g in vs.dots)
            Destroy(g);
        arealObject.SetActive(true);
        Instantiate(arealObject, vs.polyPoints[0], Quaternion.identity);
        arealObject.SetActive(false);
        vs.polyPoints.Clear();
    }

    public void DrawPoly(ref GameObject arealObject)
    {
        if (vs == null)
            vs = camera.GetComponent<VertexSelector>();
        if (vs.polyPoints.Count < 3)
        {
            vs.polyPoints.Clear();
            return;
        }
        if(vs.polyPoints.Contains(new Vector3(0, 0, 0)))
        {
            vs.polyPoints.Clear();
            return;
        }
        vs.polyPoints.Add(vs.polyPoints[0]);
        arealObject.transform.position = vs.polyPoints[0];
        arealObject.AddComponent<LineRenderer>();
        lr = arealObject.GetComponent<LineRenderer>();
        arealObjectScript.vectLocations = vs.polyPoints;
        lr.material = mat;
        lr.positionCount = vs.polyPoints.Count;
        lr.startColor = new Color(0, 1, 0);
        lr.endColor = new Color(0, 1, 0);
        lr.startWidth = 10f;
        lr.endWidth = 10f;
        lr.numCapVertices = 5; //Smooths out the line angles. 
        int i = 0;
        foreach (Vector3 v in vs.polyPoints)
        {
            lr.SetPosition(i, v);
            ++i;
        }
        foreach (GameObject g in vs.dots)
            Destroy(g);
        arealObject.SetActive(true);
        Instantiate(arealObject, vs.polyPoints[0], Quaternion.identity);
        arealObject.SetActive(false);
        vs.polyPoints.Clear();
    }

}
