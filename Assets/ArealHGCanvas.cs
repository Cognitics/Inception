using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ArealHGCanvas : MonoBehaviour
{
    public GameObject content;
    public GameObject stringPrefab;

    public void FillCanvas(List<string> list)
    {
        ClearCanvas();
        gameObject.SetActive(list.Count > 0);
        foreach(string s in list)
        {
            stringPrefab.GetComponent<Text>().text = s;
            GameObject go = Instantiate(stringPrefab);
            go.transform.SetParent(content.transform);
            go.transform.localScale = new Vector3(1, 1, 1);
        }
    }

    public void ClearCanvas()
    {
        foreach (Transform child in content.transform)
            Destroy(child.gameObject);
    }
}
