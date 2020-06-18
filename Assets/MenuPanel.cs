using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuPanel : MonoBehaviour
{
    [HideInInspector] public bool isOut = false;
    [HideInInspector] public bool lerpOut = false;
    [HideInInspector] public bool lerpIn = false;
    private Vector3 inPosition;
    private Vector3 outPosition;

    private Vector3 sliderInPos, sliderOutPos;
    public GameObject poiButton;
    private PointofInterest poiScript;
    public GameObject LayersPanel;
    public GameObject GeoPanel;
    public GameObject geolayerContent;
    public GameObject geoLayersPrefab;
    public GameObject databaseButton;
    public GameObject userObject;
    public GameObject GLTFButton;
    public GameObject GLTFPanel;

    void Start()
    {
        inPosition = gameObject.transform.position;
        outPosition = new Vector3(80f, inPosition.y, 0);
        poiScript = poiButton.GetComponent<PointofInterest>();
    }

    public void HideChildren()
    {
        foreach (Transform child in gameObject.transform)
            child.gameObject.SetActive(false);
    }

    public void SetEnabled(bool value)
    {
        gameObject.SetActive(value);
    }

    public void LerpOut(out bool lerpStatus, out bool isOutStatus)
    {
        gameObject.transform.position = outPosition;
        lerpStatus = false;
        isOutStatus = true;
    }

    public void LerpIn(out bool lerpStatus, out bool isOutStatus)
    {
        gameObject.transform.position = inPosition;
        lerpStatus = false;
        isOutStatus = false;
        HideChildren();
        poiScript.RemoveAllButtons();
    }

    public void DisablePanels()
    {
        LayersPanel.SetActive(false);
        GeoPanel.SetActive(false);
        ClearGeoChildren();
    }

    public void ClickLayers()
    {
        if (GeoPanel.activeSelf)
            GeoPanel.SetActive(false);
        LayersPanel.SetActive(!LayersPanel.activeSelf);
        ClearGeoChildren();
    }

    public void ClickGeo()
    {
        /*
        if (LayersPanel.activeSelf)
            LayersPanel.SetActive(false);
        GeoPanel.SetActive(!GeoPanel.activeSelf);
        string databaseName;
        try
        {
            databaseName = databaseButton.GetComponent<FilePanel_SelectCDB>().GetCDBDatabase().name;
        }catch(System.NullReferenceException)
        {
            databaseName = "";
        }
        if (geolayerContent.transform.childCount > 0 || !databaseName.Contains("Yemen"))
        {
            ClearGeoChildren();
            return;
        }
        GameObject go;
        var strings = YemenHG.Instance.Layers();
        var text = geoLayersPrefab.GetComponentInChildren<Text>();
        foreach(string s in strings)
        {
            text.text = s;
            go = Instantiate(geoLayersPrefab);
            go.transform.SetParent(geolayerContent.transform);
            go.transform.localScale = Vector3.one;
        }
        */
        
    }

    private void ClearGeoChildren()
    {
        foreach (Transform child in geolayerContent.transform)
            Destroy(child.gameObject);
    }

    public void ResetPositionControl()
    {
        userObject.GetComponent<User>().Reset();
    }

    public void ClickGLTF()
    {
        if (GLTFPanel == null)
            return;
        GLTFPanel.SetActive(!GLTFPanel.activeSelf);
    }
}
