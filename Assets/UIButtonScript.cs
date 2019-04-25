using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIButtonScript: MonoBehaviour
{
    private bool isOut = false;
    private bool lerpOut = false;
    private bool lerpIn = false;
    private Vector3 inPosition;
    private Vector3 outPosition;
    private float delta;
    private float speed = 300;
    public GameObject menuPanel;
    private MenuPanel menuPanelScript;

    private void Start()
    {
        inPosition = gameObject.transform.position;
        outPosition = new Vector3(0f, inPosition.y, inPosition.z);
        delta = 0f;
        menuPanelScript = menuPanel.GetComponent<MenuPanel>();
    }

    private void Update()
    {
        if (menuPanelScript.isOut)
            return;
        if (lerpOut)
        {
            delta += Time.deltaTime * speed;

            if (delta > 1.0f)
                delta = 1.0f;

            gameObject.transform.position = Vector3.Lerp(inPosition, outPosition, delta);

            if(delta >= 1.0f)
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

            if(delta >= 1.0f)
            {
                delta = 0f;
                lerpIn = false;
                isOut = false;
            }

        }
        
    }
    public void Click()
    {
        if (!isOut)
        {
            lerpOut = true;
            return;
        }
        if (isOut)
        {
            if (menuPanelScript.isOut)
                menuPanelScript.lerpIn = true;
            lerpIn = true;
            return;
        }
    }
}
