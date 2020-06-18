using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Cognitics.UnityCDB;

public class LineButton : MonoBehaviour
{
    private LineObject lineObjectScript;
    public Button addPinButton;
    public Button lineButton;
    public Button polyButton;
    public GameObject Camera;
    public GameObject lineObject;
    public Material mat;
    private VertexSelector vs;
    [HideInInspector] public bool buttonSelected = false;
    Color buttonColor;
    string buttonString;
    string buttonStringClicked = "Finish Linear (2+ points)";
    Text buttonText;

    void Initialize()
    {
        lineButton = gameObject.GetComponent<Button>();
        buttonText = lineButton.GetComponentInChildren<Text>();
        buttonColor = lineButton.GetComponent<Image>().color;
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

    // TODO: the two DrawLine methods need to be consolidated because there's too much duplicate code

    public void DrawLine()
    {
        // HACK: this method is known to be called on components whose game objects have never been activated, so manually call Initialize here
        // TODO: handle this more elegantly
        Initialize();

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
        var lo = lineObject.GetComponent<LineObject>();
        lo.ClearFields();
        lo.ClearTitle();
        var lr = lineObject.GetOrAddComponent<LineRenderer>();
        lr.material = mat;
        lo.vectLocations = vs.linePoints;
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

        lo.ClearFields();
        lineObject.SetActive(true);
        Instantiate(lineObject, vs.linePoints[0], Quaternion.identity);
        lineObject.SetActive(false);
        vs.linePoints.Clear();
    }

    public void DrawLine(ref GameObject lineObject)
    {
        // HACK: this method is known to be called on components whose game objects have never been activated, so manually call Initialize here
        // TODO: handle this more elegantly
        Initialize();

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
        var lr = lineObject.GetOrAddComponent<LineRenderer>();
        var lo = lineObject.GetComponent<LineObject>();
        //lo.title.GetComponent<TMP_InputField>().text = "";
        //lo.description.GetComponent<TMP_InputField>().text = "";
        lr.material = mat;
        lo.vectLocations = vs.linePoints;
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
