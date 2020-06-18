using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class POIPanelScript : MonoBehaviour
{
    public GameObject POIButton;
    private PointofInterest poiScript;

    void Start()
    {
        poiScript = POIButton.GetComponent<PointofInterest>();
    }


}
