using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuPanel : MonoBehaviour
{
    [HideInInspector] public bool isOut = false;
    private bool lerpOut = false;
    [HideInInspector] public bool lerpIn = false;
    private Vector3 inPosition;
    private Vector3 outPosition;
    private float delta;
    private float speed = 300;
    public GameObject addFeature;
    public GameObject settings;
    public GameObject pointOfInterest;
    public GameObject mainMenu;
    public GameObject poiButton;
    private PointofInterest poiScript;
    

    void Start()
    {
        inPosition = gameObject.transform.position;
        outPosition = new Vector3(80f, inPosition.y, inPosition.x);
        delta = 0f;
        poiScript = poiButton.GetComponent<PointofInterest>();
    }

    void Update()
    {
        if (lerpOut)
        {
            delta += Time.deltaTime * speed;

            if (delta > 1.0f)
                delta = 1.0f;

            gameObject.transform.position = Vector3.Lerp(inPosition, outPosition, delta);

            if (delta >= 1.0f)
            {
                delta = 0;
                lerpOut = false;
                isOut = true;
            }
        }
        if (lerpIn)
        {
            delta += Time.deltaTime * speed;

            if (delta > 1.0f)
                delta = 1.0f;

            gameObject.transform.position = Vector3.Lerp(outPosition, inPosition, delta);

            if (delta >= 1.0f)
            {
                delta = 0f;
                lerpIn = false;
                isOut = false;
                RemoveAllPanels();
                poiScript.RemoveAllButtons();
            }

        }
    }

    public void Click(string button)
    {
        if (!isOut)
        {
            switch (button)
            {
                case "Settings":
                    settings.SetActive(true);
                    break;
                case "AddFeature":
                    addFeature.SetActive(true);
                    break;
                case "MainMenu":
                    mainMenu.SetActive(true);
                    break;
                case "PointOfInterest":
                    pointOfInterest.SetActive(true);
                    break;
                default:
                    break;
            }
            lerpOut = true;
            return;
        }
        if (isOut)
        {
            lerpIn = true;
            return;
        }

    }

    private void RemoveAllPanels()
    {
        foreach (Transform child in gameObject.transform)
            child.gameObject.SetActive(false);
    }
}
