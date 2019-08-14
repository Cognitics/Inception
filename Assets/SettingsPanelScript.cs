using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelScript : MonoBehaviour
{
    public GameObject uiControlsCheckmark;
    public GameObject uiControlsCanvas;
    public GameObject debugCanvas;
    public GameObject debugCheckmark;
    public GameObject readmeCanvas;
    public GameObject userObject;
    public GameObject toggleObject;
    public Slider speedSlider;
    public Slider sensitivitySlider;
    public GameObject movementCheckMark;


    public void TouchControls()
    {
        if (uiControlsCanvas != null)
        {
            uiControlsCanvas.SetActive(!uiControlsCanvas.activeSelf);
            uiControlsCheckmark.SetActive(uiControlsCanvas.activeSelf);
        }
    }

    public void MoveMode()
    {
        toggleObject.GetComponent<Toggle>().newMovement = !toggleObject.GetComponent<Toggle>().newMovement;
        movementCheckMark.SetActive(!movementCheckMark.activeSelf);
    }

    public void DebugControl()
    {
        if(debugCanvas != null)
        {
            debugCanvas.SetActive(!debugCanvas.activeSelf);
            debugCheckmark.SetActive(debugCanvas.activeSelf);
        }
    }

    public void ReadmeControl()
    {
        if (readmeCanvas != null)
            readmeCanvas.SetActive(!readmeCanvas.activeSelf);
    }

    public void SpeedSliderControl()
    {
        if (userObject != null)
            userObject.GetComponent<User>().SpeedSlider = (int)speedSlider.value;
    }

    public void SensitivitySliderControl()
    {
        if (userObject != null)
        {
            userObject.GetComponent<User>().MouseSensitivity = 100 * sensitivitySlider.value;
            userObject.GetComponent<User>().TouchSensitivity = (float)0.04 * sensitivitySlider.value;
        }
    }
}
