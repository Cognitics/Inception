using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIButtonScript: MonoBehaviour
{
    public GameObject MenuPanel;
    public GameObject SettingsPanel;
    public GameObject FeaturePanel;
    public GameObject POIPanel;
    public GameObject MagePanel;
    private bool panelVisible;
    private MenuPanel mpScript;
    private MagePanel MagePanelScript;

    void Start()
    {
        mpScript = MenuPanel.GetComponent<MenuPanel>();
        MagePanelScript = MagePanel.GetComponent<MagePanel>();
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

    public void MageClick()
    {
        SetActive(MagePanel);  
    }

    public void HideChildren()
    {
        if(mpScript != null)
            mpScript.DisablePanels();
        MenuPanel.SetActive(false);
        SettingsPanel.SetActive(false);
        FeaturePanel.SetActive(false);
        POIPanel.SetActive(false);
        MagePanel.SetActive(false);
    }

    private void SetActive(GameObject obj)
    {
        panelVisible = obj.activeSelf;
        HideChildren();
        obj.SetActive(!panelVisible);
    }
}
