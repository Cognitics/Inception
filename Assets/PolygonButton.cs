using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cognitics.UnityCDB;

public class PolygonButton : MonoBehaviour
{
    public Button addPinButton;
    public GameObject arealObject;
    public Button lineButton;
    public Button polyButton;
    public GameObject Camera;
    private VertexSelector vs;
    public Material mat;
    private ArealObject arealObjectScript;
    

    [HideInInspector] public bool buttonSelected = false;
    Color buttonColor;
    string buttonString;
    string buttonStringClicked = "Finish Areal (3+ points)";
    Text buttonText;

    void Initialize()
    {
        polyButton = gameObject.GetComponent<Button>();
        buttonText = polyButton.GetComponentInChildren<Text>();
        buttonColor = polyButton.GetComponent<Image>().color;
        buttonString = buttonText.text;
        vs = Camera.GetComponent<VertexSelector>();
    }

    void Start()
    {
        Initialize();
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

    // TODO: the two DrawPoly methods need to be consolidated because there's too much duplicate code

    private void DrawPoly()
    {
        // HACK: this method is known to be called on components whose game objects have never been activated, so manually call Initialize here
        // TODO: handle this more elegantly
        Initialize();

        if (vs.polyPoints.Count < 3)
        {
            vs.polyPoints.Clear();
            return;
        }
        vs.polyPoints.Add(vs.polyPoints[0]);
        arealObject.transform.position = vs.polyPoints[0];
        var lr = arealObject.GetOrAddComponent<LineRenderer>();
        var ao = arealObject.GetComponent<ArealObject>();
        ao.ClearFields();
        ao.ClearTitle();
        ao.vectLocations = vs.polyPoints;
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
        // HACK: this method is known to be called on components whose game objects have never been activated, so manually call Initialize here
        // TODO: handle this more elegantly
        Initialize();

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
        var lr = arealObject.GetOrAddComponent<LineRenderer>();
        var ao = arealObject.GetComponent<ArealObject>();
        try
        {
            ao.vectLocations = vs.polyPoints;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
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
