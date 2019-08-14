using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIButtonScript: MonoBehaviour
{
    public GameObject MenuPanel;
    public GameObject SettingsPanel;
    public GameObject FeaturePanel;
    public GameObject POIPanel;
    private bool panelVisible;
    private MenuPanel mpScript;

    void Start()
    {
        mpScript = MenuPanel.GetComponent<MenuPanel>();
    }

    public void MenuClick()
    {
        SetActive(MenuPanel);
    }
    
    public void SettingsClick()
    {
        SetActive(SettingsPanel);
    }

    public void FeatureClick()
    {
        SetActive(FeaturePanel);
    }

    public void POIClick()
    {
        SetActive(POIPanel);
    }

    public void HideChildren()
    {
        if(mpScript != null)
            mpScript.DisablePanels();
        MenuPanel.SetActive(false);
        SettingsPanel.SetActive(false);
        FeaturePanel.SetActive(false);
        POIPanel.SetActive(false);
    }

    private void SetActive(GameObject obj)
    {
        panelVisible = obj.activeSelf;
        HideChildren();
        obj.SetActive(!panelVisible);
    }
}
