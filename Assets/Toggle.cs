using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Toggle : MonoBehaviour
{
    private Button btn;
    private bool btnPressed = false;
    public GameObject UserObject;


    void Start()
    {
        btn = gameObject.GetComponent<Button>();
    }

    void Update()
    {
        if (!IsPointerOverUIObject())
        {
            if (btnPressed)
            {
                btn.GetComponentInChildren<Text>().text = "Pan";
                // Touch 0 should be the 'pan' button,
                // Touch 1 should be the pan movement
                if (Input.touchCount >= 2)
                    UserObject.GetComponent<User>().Pan(Input.GetTouch(1));
            }
            else
            {
                btn.GetComponentInChildren<Text>().text = "Hold to Pan";
                if (Input.touchCount == 1)
                    UserObject.GetComponent<User>().Rotate(Input.GetTouch(0));
                else if (Input.touchCount >= 2)
                {
                    Touch touch1 = Input.GetTouch(0);
                    Touch touch2 = Input.GetTouch(1);

                    Vector2 touchDelta = (touch1.deltaPosition + touch2.deltaPosition) / 2;
                    Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;
                    Vector2 touch2PrevPos = touch2.position - touch2.deltaPosition;
                    double prevTouchDistance = Vector2.Distance(touch1PrevPos, touch2PrevPos);
                    double touchDistance = Vector2.Distance(touch1.position, touch2.position);
                    int touchDirection = (touchDistance - prevTouchDistance) > 0 ? 1 : -1;
                    float translationSpeed = touchDirection * UserObject.GetComponent<User>().SpeedSlider * touchDelta.magnitude;
                    UserObject.GetComponent<User>().MoveForward(translationSpeed);
                }
            }
        }
    }

    public void ButtonPressed()
    {
        btnPressed = true;
    }
    public void ButtonReleased()
    {
        btnPressed = false;
    }
    private bool IsPointerOverUIObject()
    {
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }
}
