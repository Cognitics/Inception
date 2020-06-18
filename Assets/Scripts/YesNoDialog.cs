using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class YesNoDialog : MonoBehaviour {
    public Text question;
    public Button yesButton;
    public Button noButton;
    public GameObject yesNoPanelObject;
    public static YesNoDialog yesNoPanel;

    public static YesNoDialog Instance()
    {
        if (!yesNoPanel)
        {
            yesNoPanel = FindObjectOfType(typeof(YesNoDialog)) as YesNoDialog;
            if (!yesNoPanel)
                Debug.LogError("There needs to be one active yesNoPanel script on a GameObject in your scene.");
        }
        
        return yesNoPanel;
    }

    // Yes/No: A string, a Yes event and a No event
    public void Choice(string question, UnityAction yesEvent, UnityAction noEvent)
    {
        
        yesNoPanelObject.SetActive(true);

        yesButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(yesEvent);
        yesButton.onClick.AddListener(ClosePanel);

        noButton.onClick.RemoveAllListeners();
        noButton.onClick.AddListener(noEvent);
        noButton.onClick.AddListener(ClosePanel);

        this.question.text = question;
        yesButton.gameObject.SetActive(true);
        noButton.gameObject.SetActive(true);

    }

    public void ClosePanel()
    {
        yesNoPanelObject.SetActive(false);
    }
}
