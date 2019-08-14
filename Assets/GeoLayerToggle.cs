using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GeoLayerToggle : MonoBehaviour
{
    private bool FeatureLoaded = false;
    public GameObject label;

    public void GeoLayerToggled()
    {
        FeatureLoaded = !FeatureLoaded;

        if (!FeatureLoaded)
            YemenHG.Instance.DestroyLayer(label.GetComponent<Text>().text);
        else
            YemenHG.Instance.CreateLayer(label.GetComponent<Text>().text);
        
    }
}
