using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuButtonScript : MonoBehaviour
{
    public GameObject UIButtonPanel;
    private UIButtonScript buttonScript;

    private void Start()
    {
        buttonScript = UIButtonPanel.GetComponent<UIButtonScript>();
        UIButtonPanel.SetActive(false);
    }

    public void OnClick()
    {
        UIButtonPanel.SetActive(!UIButtonPanel.activeSelf);
        buttonScript.HideChildren();
    }
}
