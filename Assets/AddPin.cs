using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class AddPin : MonoBehaviour
{
    public Button addPinButton;
    public Button lineButton;
    public Button polyButton;

    [HideInInspector] public bool buttonSelected = false;
    Color buttonColor;
    string buttonString;
    string buttonStringClicked = "Click the Terrain to Place the Pin";
    Text buttonText;

    public void Start()
    {
        addPinButton = gameObject.GetComponent<Button>();
        buttonText = addPinButton.GetComponentInChildren<Text>();
        buttonColor = addPinButton.GetComponent<Image>().color;
        buttonString = buttonText.text;
    }

    public void SwitchButtonState()
    {
        buttonSelected = !buttonSelected;
        lineButton.GetComponent<LineButton>().DisableButton();
        polyButton.GetComponent<PolygonButton>().DisableButton();

        if (buttonSelected)
        {
            addPinButton.GetComponent<Image>().color = addPinButton.colors.pressedColor;
            buttonText.text = buttonStringClicked;
        }

        else
        {
            addPinButton.GetComponent<Image>().color = buttonColor;
            buttonText.text = buttonString;
        }
    }

    public void DisableButton()
    {
        buttonSelected = false;
        buttonText.text = buttonString;
        addPinButton.GetComponent<Image>().color = buttonColor;
    }

}
