
using UnityEngine;
using UnityEngine.UI;

public class Options : MonoBehaviour
{
    public Cognitics.UnityCDB.Database Database = null;
    public GameObject UserObject = null;
    GameObject optionsPanel = null;
    GameObject debugCheckmark = null;
    GameObject uiControlsCheckmark = null;
    GameObject showLayersCheckmark = null;
    Slider maxLodSlider = null;
    Text maxLodValue = null;
    Slider speedSlider = null;
    Text speedValue = null;
    public GameObject debugCanvas = null;
    public GameObject readmeCanvas = null;
    public GameObject uiControlsCanvas = null;
    public GameObject layersCanvas = null;

    void Awake()
    {
        optionsPanel = transform.Find("Panel").gameObject;
        optionsPanel.SetActive(false);
        debugCheckmark = transform.Find("Panel/DebugWindowButton/Background/Checkmark").gameObject;
        uiControlsCheckmark = transform.Find("Panel/UIControlsButton/Background/Checkmark").gameObject;
        showLayersCheckmark = transform.Find("Panel/ShowLayersButton/Background/Checkmark").gameObject;
        maxLodSlider = transform.Find("Panel/MaxLODSlider").GetComponent<Slider>();
        maxLodValue = transform.Find("Panel/MaxLODValue").GetComponent<Text>();
        OnMaxLodSliderChanged();
        speedSlider = transform.Find("Panel/SpeedSlider").GetComponent<Slider>();
        speedValue = transform.Find("Panel/SpeedValue").GetComponent<Text>();
        OnSpeedSliderChanged();
    }

    public void OnClick()
    {
        if(optionsPanel != null)
            optionsPanel.SetActive(!optionsPanel.activeSelf);
        if (layersCanvas != null)
            layersCanvas.SetActive(showLayersCheckmark.activeInHierarchy);
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
        if (layersCanvas != null)
        {
            layersCanvas.SetActive(!layersCanvas.activeSelf);
            showLayersCheckmark.SetActive(layersCanvas.activeSelf);
        }
    }

    public void OnMaxLodSliderChanged()
    {
        maxLodValue.text = ((int)maxLodSlider.value).ToString();
        if (Database != null)
            Database.SetMaxLOD((int)maxLodSlider.value);
    }

    public void OnSpeedSliderChanged()
    {
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
}
