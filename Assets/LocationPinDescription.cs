using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocationPinDescription : MonoBehaviour
{
    private GameObject thisObject;
    public Canvas pinObjectCanvas;
    public Canvas abovePinCanvas;

    public void OnMouseDown()
    {
        Debug.Log("Clicked");
    }

    public void DeleteObject()
    {
        Destroy(gameObject);
    }

    public void SaveObject()
    {

    }
}
