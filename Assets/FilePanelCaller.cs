using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FilePanelCaller : MonoBehaviour
{
    public Button cdbSelectButton;
    private FilePanel_SelectCDB script;

    private void Start()
    {
        script = cdbSelectButton.GetComponent<FilePanel_SelectCDB>();
    }

    void Update()
    {
        script.CalledUpdate();
    }
}
