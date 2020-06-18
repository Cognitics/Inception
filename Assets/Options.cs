
using UnityEngine;
using UnityEngine.UI;

public class Options : MonoBehaviour
{
    public Cognitics.UnityCDB.Database Database = null;
    public GameObject UserObject = null;
    public GameObject optionsPanel = null;
    public GameObject debugCheckmark = null;
    public GameObject uiControlsCheckmark = null;
    public Slider speedSlider = null;
    //Text speedValue = null;
    public GameObject debugCanvas = null;
    public GameObject readmeCanvas = null;
    public GameObject uiControlsCanvas = null;
    public GameObject layersCanvas = null;
    public GameObject lodCanvas = null;
    public GameObject geoLayersPrefab;
    public GameObject geoLayerContent;
    public GameObject geoLayerCanvas;
    public Layers layerScript;

    void Awake()
    {
        uiControlsCanvas.SetActive(false);
        uiControlsCheckmark.SetActive(true);
        geoLayerCanvas.SetActive(false);
        layerScript = layersCanvas.GetComponent<Layers>();
    }

    public void OnClick()
    {
        if(optionsPanel != null)
            optionsPanel.SetActive(!optionsPanel.activeSelf);
    }

    public void OnDebugClick()
    {
        if (debugCanvas != null)
        {
            debugCanvas.SetActive(!debugCanvas.activeSelf);
            debugCheckmark.SetActive(debugCanvas.activeSelf);
        }
    }

    public void ResetPositionClick()
    {
        UserObject.GetComponent<User>().Reset();
    }

    public void OnUIControlsClick()
    {
        if (uiControlsCanvas != null)
        {
            uiControlsCanvas.SetActive(!uiControlsCanvas.activeSelf);
            uiControlsCheckmark.SetActive(uiControlsCanvas.activeSelf);
        }
    }

    public void OnShowLayersClick()
    {
        CloseGeo();
        if (layersCanvas != null)
        {
            layersCanvas.SetActive(!layersCanvas.activeSelf);

            if (layersCanvas.activeSelf)
                layerScript.PopulateList();
            //if (!layersCanvas.activeSelf)
                //layerScript.RemoveList();
        }
    }

    public void OnSpeedSliderChanged()
    {
        
        //speedValue.text = ((int)speedSlider.value).ToString();
        //if (UserObject != null)
           // UserObject.GetComponent<User>().SpeedSlider = (int)speedSlider.value;
    }

    public void OpenReadme()
    {
        if(readmeCanvas != null)
        {
            readmeCanvas.SetActive(!readmeCanvas.activeSelf);
        }
    }
    public void SetGeoCanvas()
    {
        if (geoLayerCanvas != null)
            geoLayerCanvas.SetActive(!geoLayerCanvas.activeSelf);
    }
    
    public void GeoLayers()
    {
        CloseLayers();
        SetGeoCanvas();
        /*
        if (geoLayerContent.transform.childCount != 0)
            return;
        GameObject go;
        var strings = YemenHG.Instance.Layers();
        var text = geoLayersPrefab.GetComponentInChildren<Text>();

        foreach (string s in strings)
        {
            text.text = s;
            go = Instantiate(geoLayersPrefab);
            go.transform.SetParent(geoLayerContent.transform);
            go.transform.localScale = new Vector3(1, 1, 1);
        }
        */

    }
    private void CloseLayers()
    {
        var layers = GameObject.Find("LayersCanvas");
        if (layers != null)
            layers.SetActive(false);
        //layerScript.RemoveList();
    }
    private void CloseGeo()
    {
        var geo = GameObject.Find("GeoLayersCanvas");
        if (geo != null)
            geo.SetActive(false);
        geo = null;
    }
    public void CloseBoth()
    {
        CloseLayers();
        CloseGeo();
    }
}
