using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class LineButton : MonoBehaviour
{
    private LineObject lineObjectScript;
    public Button addPinButton;
    public Button lineButton;
    public Button polyButton;
    public GameObject camera;
    public GameObject lineObject;
    public Material mat;
    private VertexSelector vs;
    private LineRenderer lr;
    [HideInInspector] public bool buttonSelected = false;
    Color buttonColor;
    string buttonString;
    string buttonStringClicked = "Finish Linear (2+ points)";
    Text buttonText;
    
    void Start()
    {
        lineButton = gameObject.GetComponent<Button>();
        buttonText = lineButton.GetComponentInChildren<Text>();
        buttonColor = lineButton.GetComponent<Image>().color;
        buttonString = buttonText.text;
        vs = camera.GetComponent<VertexSelector>();
        lineObjectScript = lineObject.GetComponent<LineObject>();
    }

    public void SwitchButtonState()
    {
        buttonSelected = !buttonSelected;
        addPinButton.GetComponent<AddPin>().DisableButton();
        polyButton.GetComponent<PolygonButton>().DisableButton();

        if (buttonSelected)
        {
            lineButton.GetComponent<Image>().color = lineButton.colors.pressedColor;
            buttonText.text = buttonStringClicked;
        }

        else
        {
            lineButton.GetComponent<Image>().color = buttonColor;
            buttonText.text = buttonString;
            DrawLine();
        }
    }

    public void DisableButton()
    {
        buttonSelected = false;
        buttonText.text = buttonString;
        lineButton.GetComponent<Image>().color = buttonColor;
        foreach (GameObject g in vs.dots)
            Destroy(g);
        vs.linePoints.Clear();
    }

    public void DrawLine()
    {
        if (vs.linePoints.Count < 2)
        {
            vs.linePoints.Clear();
            return;
        }
        if(vs.linePoints.Contains(new Vector3(0, 0, 0)))
        {
            vs.linePoints.Clear();
            return;
        }
        lineObject.transform.position = vs.linePoints[0];
        lineObject.GetComponent<LineObject>().ClearFields();
        lineObject.GetComponent<LineObject>().ClearTitle();
        lineObject.AddComponent<LineRenderer>();
        lr = lineObject.GetComponent<LineRenderer>();
        lr.material = mat;
        lineObjectScript.vectLocations = vs.linePoints;
        lr.positionCount = vs.linePoints.Count;
        lr.startColor = new Color(1, 0, 0);
        lr.endColor = new Color(1, 0, 0);
        lr.startWidth = 10f;
        lr.endWidth = 10f;
        lr.numCapVertices = 5; //Smooths out the line angles. 
        int i = 0;
        foreach(Vector3 v in vs.linePoints)
        {
            lr.SetPosition(i, v);
            ++i;
        }

        foreach (GameObject g in vs.dots)
            Destroy(g);

        lineObject.GetComponent<LineObject>().ClearFields();
        lineObject.SetActive(true);
        Instantiate(lineObject, vs.linePoints[0], Quaternion.identity);
        lineObject.SetActive(false);
        vs.linePoints.Clear();
    }

    public void DrawLine(ref GameObject lineObject)
    {
        if (vs.linePoints.Count < 2)
        {
            vs.linePoints.Clear();
            return;
        }

        lineObject.transform.position = vs.linePoints[0];
        lineObject.AddComponent<LineRenderer>();
        //lineObject.GetComponent<LineObject>().title.GetComponent<TMP_InputField>().text = "";
        //lineObject.GetComponent<LineObject>().description.GetComponent<TMP_InputField>().text = "";
        lr = lineObject.GetComponent<LineRenderer>();
        lr.material = mat;
        lineObjectScript.vectLocations = vs.linePoints;
        lr.positionCount = vs.linePoints.Count;
        lr.startColor = new Color(1, 0, 0);
        lr.endColor = new Color(1, 0, 0);
        lr.startWidth = 10f;
        lr.endWidth = 10f;
        lr.numCapVertices = 5; //Smooths out the line angles. 
        int i = 0;
        foreach (Vector3 v in vs.linePoints)
        {
            lr.SetPosition(i, v);
            ++i;
        }

        foreach (GameObject g in vs.dots)
            Destroy(g);
        lineObject.SetActive(true);
        Instantiate(lineObject, vs.linePoints[0], Quaternion.identity);
        lineObject.SetActive(false);
        vs.linePoints.Clear();
    }
}
