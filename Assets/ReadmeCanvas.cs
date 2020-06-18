using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ReadmeCanvas : MonoBehaviour
{
    public TextAsset textAsset;
    public GameObject readmeCanvas;
    void Start()
    {
        if(textAsset != null)
            gameObject.GetComponent<TextMeshProUGUI>().text = textAsset.text;
    }
    public void CloseReadMe()
    {
        readmeCanvas.SetActive(false);
    }
}
