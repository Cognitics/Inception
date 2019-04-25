
using UnityEngine;
using UnityEngine.UI;

public class Options : MonoBehaviour
{
    public Cognitics.UnityCDB.Database Database = null;
    public GameObject UserObject = null;
    public GameObject optionsPanel = null;
    public GameObject debugCheckmark = null;
    public GameObject uiControlsCheckmark = null;
    Slider maxLodSlider = null;
    Text maxLodValue = null;
    public Slider speedSlider = null;
    Text speedValue = null;
    public GameObject debugCanvas = null;
    public GameObject readmeCanvas = null;
    public GameObject uiControlsCanvas = null;
    public GameObject layersCanvas = null;
    public GameObject lodCanvas = null;
    public GameObject geoLayersPrefab;
    public GameObject geoLayerContent;
    public GameObject geoLayerCanvas;

    void Awake()
    {
        //optionsPanel.SetActive(false);
        //maxLodSlider = transform.Find("Panel/MaxLODSlider").GetComponent<Slider>();
        //maxLodValue = transform.Find("Panel/MaxLODValue").GetComponent<Text>();
        //OnMaxLodSliderChanged();
        //OnSpeedSliderChanged();
        geoLayerCanvas.SetActive(false);
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
        }
    }

    public void OnMaxLodSliderChanged()
    {
        //maxLodValue.text = ((int)maxLodSlider.value).ToString();
        //if (Database != null)
            //Database.SetMaxLOD((int)maxLodSlider.value);
    }

    public void OnSpeedSliderChanged()
    {
        return;
        speedValue.text = ((int)speedSlider.value).ToString();
        if (UserObject != null)
            UserObject.GetComponent<User>().SpeedSlider = (int)speedSlider.value;
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
        if (geoLayerContent.transform.childCount != 0)
            return;
        GameObject go;
        var strings = YemenHG.Instance.Layers();
        var text = geoLayersPrefab.GetComponentInChildren<Text>();

        foreach (string s in strings)
        {
            text.text = s;
            go = Instantiate(geoLayersPrefab);
            go.transform.parent = geoLayerContent.transform;
        }

    }
    private void CloseLayers()
    {
        var layers = GameObject.Find("LayersCanvas");
        if (layers != null)
            layers.SetActive(false);
        layers = null;
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
