using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Toggle : MonoBehaviour
{
    public Text btnText;
    public bool btnPressed = false;
    public GameObject UserObject;
    private User userComponent = null;
    private const float DETAIL_MODE_SPEEDFACTOR = .5f;
    private PointerEventData eventDataCurrentPosition = null;
    private List<RaycastResult> results = null;
    private Quaternion rotation;
    public bool newMovement;


    void Start()
    {
        var btn = gameObject.GetComponent<Button>();
        btnText = btn.GetComponentInChildren<Text>();
        eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        results = new List<RaycastResult>();
        userComponent = UserObject.GetComponent<User>();
        rotation = userComponent.transform.rotation;
        newMovement = false;
    }

    void Update()
    {
        if (true)
        {
            if (btnPressed)
            {
                btnText.text = "Pan";
                // Touch 0 should be the 'pan' button,
                // Touch 1 should be the pan movement
                //if (Input.touchCount >= 2)
                    //userComponent.Pan(Input.GetTouch(1));
            }
            else
            {
                btnText.text = "Hold to Pan";
                //if (Input.touchCount == 1)
                    //userComponent.Rotate(Input.GetTouch(0));
                /*else*/ if (Input.touchCount >= 2)
                {
                    /*
                    Touch touch1 = Input.GetTouch(0);
                    Touch touch2 = Input.GetTouch(1);

                    Vector2 touchDelta = (touch1.deltaPosition + touch2.deltaPosition) / 2;
                    Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;
                    Vector2 touch2PrevPos = touch2.position - touch2.deltaPosition;
                    float direction = Vector3.Dot(touch1.deltaPosition, touch2.deltaPosition);
                    Vector2 rotation = touch1.deltaPosition;
                    rotation.y = 0;
                    if (direction > 0)
                        userComponent.transform.Rotate(touch1.deltaPosition.y/10, 0, 0);
                    if (direction < -.25 && direction > - 1)
                        userComponent.transform.Rotate(0, touch1.deltaPosition.x / 10 - touch2.deltaPosition.x/10, 0);
                    GUI.Label(new Rect(0, 0, 100, 100), direction.ToString());
                    if (direction == -1)
                        userComponent.transform.position += Vector3.forward;*/
                        
                    /*
                    double prevTouchDistance = Vector2.Distance(touch1PrevPos, touch2PrevPos);
                    double touchDistance = Vector2.Distance(touch1.position, touch2.position);
                    
                    int touchDirection = (touchDistance - prevTouchDistance) > 0 ? 1 : -1;
                    float translationSpeed = touchDirection * userComponent.SpeedSlider * touchDelta.magnitude * DETAIL_MODE_SPEEDFACTOR;
                    userComponent.MoveForward(translationSpeed);*/
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (newMovement)
        {/*
            if (EventSystem.current.currentSelectedGameObject == null)
            {
                float pinchAmount = 0;
                Quaternion desiredRotation = transform.rotation;

                TouchInput.Calculate();

                if (Mathf.Abs(TouchInput.pinchDistanceDelta) > 0)
                    pinchAmount = TouchInput.pinchDistanceDelta;

                if (Mathf.Abs(TouchInput.turnAngleDelta) > 0)
                {
                    Vector3 rotationDeg = Vector3.zero;
                    rotationDeg.y = -TouchInput.turnAngleDelta;
                    desiredRotation *= Quaternion.Euler(rotationDeg);
                }
                if (TouchInput.touchDirection > 0)
                {
                    Vector3 angle = userComponent.transform.localEulerAngles + new Vector3(TouchInput.twoTouchDelta / lookSensitivity, 0, 0);
                    angle.x = Mathf.Clamp(angle.x, 10, 80);
                    userComponent.transform.localEulerAngles = angle;
                }
                if (TouchInput.pivotPoint != Vector3.positiveInfinity)
                    userComponent.transform.RotateAround(TouchInput.pivotPoint, Vector3.up, desiredRotation.y * rotationSensitivity * -1);
                userComponent.MoveForward(pinchAmount);
                TouchInput.twoTouchDelta = 0f;

                Mathf.Clamp(userComponent.transform.rotation.y, 10, 80);

                if (!TouchInput.checkForPoint)
                {
                    checkForPointTimeout += Time.deltaTime;
                    if (checkForPointTimeout >= .5)
                    {
                        checkForPointTimeout = 0f;
                        TouchInput.checkForPoint = true;
                    }
                }
            }*/
        }
        else
        {/*
            if (!IsPointerOverUIObject())
            {
                if (btnPressed)
                {
                    btnText.text = "Pan";
                    // Touch 0 should be the 'pan' button,
                    // Touch 1 should be the pan movement
                    if (Input.touchCount >= 2)
                        userComponent.Pan(Input.GetTouch(1));
                }
                else
                {
                    btnText.text = "Hold to Pan";
                    if (Input.touchCount == 1)
                        userComponent.Rotate(Input.GetTouch(0));
                    /*else*//*
                    if (Input.touchCount >= 2)
                    {
                        Touch touch1 = Input.GetTouch(0);
                        Touch touch2 = Input.GetTouch(1);

                        Vector2 touchDelta = (touch1.deltaPosition + touch2.deltaPosition) / 2;
                        Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;
                        Vector2 touch2PrevPos = touch2.position - touch2.deltaPosition;
                        double prevTouchDistance = Vector2.Distance(touch1PrevPos, touch2PrevPos);
                        double touchDistance = Vector2.Distance(touch1.position, touch2.position);
                        int touchDirection = (touchDistance - prevTouchDistance) > 0 ? 1 : -1;
                        float translationSpeed = touchDirection * userComponent.SpeedSlider * touchDelta.magnitude * DETAIL_MODE_SPEEDFACTOR;
                        userComponent.MoveForward(translationSpeed);
                    }
                }
            }*/
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
    /*
    private bool IsPointerOverUIObject()
    {
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        results.Clear();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }*/
}
