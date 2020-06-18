using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class User : MonoBehaviour
{
    private Vector3 DragStart;
    private Cognitics.UnityCDB.SurfaceCollider SurfaceCollider;
    private GameObject TerrainTester; 
    private bool isDragging = false;
    public GameObject ModeButtonGameObject;
    private ModeButton ModeButton;
    private Cognitics.UnityCDB.SurfaceCollider surfaceCollider = null;
    private bool isTouch;
    private float checkForPointTimeout = 0f;
    private float rotationSensitivity = 60f;
    private float lookSensitivity = 75f;
    private Toggle toggle;
    private PointerEventData eventDataCurrentPosition;
    public bool newMovement;
    private const float DETAIL_MODE_SPEEDFACTOR = .02f;

    public float MouseSensitivity = 100.0f;
    public float TouchSensitivity = 0.04f;
    public GameObject toggleObject;
    private float lastX, lastY;

    public float NormalSpeed = 1.0f;
    public float ShiftSpeed = 10.0f;
    public float RotationSpeed = 20f;

    public float ClampAngle = 80.0f;

    public float MinimumElevation = 50.0f;

    public int SpeedSlider = 10;

    //public float SpeedFactor => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? ShiftSpeed * SpeedSlider : NormalSpeed * SpeedSlider;
    public void MoveForward() => transform.position += SpeedFactor() * Time.deltaTime * transform.forward;
    public void MoveForward(float distance) => transform.position += distance * transform.forward;
    public void MoveBackward() => transform.position += SpeedFactor() * Time.deltaTime * -transform.forward;
    public void MoveRight() => transform.position += SpeedFactor()  * Time.deltaTime * transform.right;
    public void MoveLeft() => transform.position += SpeedFactor() * Time.deltaTime * -transform.right;
    public void MoveUp() => transform.position += SpeedFactor() * Time.deltaTime * transform.up;
    public void MoveDown() => transform.position += SpeedFactor() * Time.deltaTime * -transform.up;
    public void MoveHigher() => transform.position += SpeedFactor() * Time.deltaTime * Vector3.up;
    public void MoveLower() => transform.position += SpeedFactor() * Time.deltaTime * -Vector3.up;

    public void Rotate(Touch touch)
    {
        Vector3 rotation = transform.rotation.eulerAngles;
        rotation.y += touch.deltaPosition.x * TouchSensitivity * -1;
        rotation.x += touch.deltaPosition.y * TouchSensitivity;
        transform.rotation = Quaternion.Euler(rotation);
    }

    public float SpeedFactor()
    {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            if (ModeButton.isDetailMode)
                return ShiftSpeed * SpeedSlider * 0.2f;
            else
                return ShiftSpeed * SpeedSlider;
        }
        else
        {
            if (ModeButton.isDetailMode)
                return NormalSpeed * SpeedSlider * 0.2f;
            else
                return NormalSpeed * SpeedSlider;
        }
    }

    public void Pan(Touch touch)
    {
        Vector3 offset = Camera.main.transform.TransformVector(new Vector3(-touch.deltaPosition.x, 0.0f, -touch.deltaPosition.y));
        Vector3 position = transform.position;
        transform.position = new Vector3(position.x + offset.x, position.y, position.z + offset.z);
    }

    //private IEnumerator SurfaceCollider_CR()
    //{
    //    var surfaceCollider = GetComponent<Cognitics.UnityCDB.SurfaceCollider>();
    //    if (surfaceCollider != null)
    //    {
    //        yield return new WaitUntil(() => surfaceCollider.Database != null);
    //        this.surfaceCollider = surfaceCollider;
    //    }
    //    yield return null;
    //}

    public void Reset()
    {
        rotationY = 0.0f;
        rotationX = 0.0f;
        transform.SetPositionAndRotation(new Vector3(0f, 1000.0f, 0f), new Quaternion(0f, 0f, 0f, 0f));
    }

    #region implementation

    private float rotationY = 0.0f; // rotation around the up/y axis
    private float rotationX = 0.0f; // rotation around the right/x axis

    private void Awake()
    {
        toggle = toggleObject.GetComponent<Toggle>();
    }

    private void Start()
    {
        Input.simulateMouseWithTouches = false;
        ModeButton = ModeButtonGameObject.GetComponent<ModeButton>();
        //StartCoroutine(SurfaceCollider_CR());
        surfaceCollider = GetComponent<Cognitics.UnityCDB.SurfaceCollider>();;
        TerrainTester = GameObject.Find("TerrainTester");
        SurfaceCollider = TerrainTester.GetComponent<Cognitics.UnityCDB.SurfaceCollider>();
        eventDataCurrentPosition = new PointerEventData(EventSystem.current);
    }

   private void InitDrag()
    {
        if (EventSystem.current.currentSelectedGameObject != null)
            return;
        DragStart = Cognitics.Unity.TouchInput.singleTouchPoint;
        if (DragStart == Vector3.zero)
            return;
        isDragging = true;
    }

    private void MoveCamera()
    {
        if (DragStart == Vector3.zero)
            return;
        if (EventSystem.current.currentSelectedGameObject != null)
            return;
        Vector3 actualPos = Vector3.zero;
        actualPos = Cognitics.Unity.TouchInput.singleTouchPoint;
        if (actualPos == Vector3.zero)
            return;
        Vector3 dragDelta = actualPos - DragStart;
        dragDelta.y = 0;
        gameObject.transform.position -= dragDelta;
    }

    private void FinishDrag()
    {
        isDragging = false;
    }

    private void Update()
    {
        if (!IsPointerOverUIObject() || !(EventSystem.current
            && EventSystem.current.currentSelectedGameObject
            && EventSystem.current.currentSelectedGameObject.GetComponentInChildren<InputField>()))
        {
            if (toggle.newMovement)
            {
                float pinchAmount = 0f;
                Quaternion desiredRotation = Quaternion.identity;

                Cognitics.Unity.TouchInput.Calculate();

                if (Input.touchCount == 1)
                    isTouch = true;
                else
                    isTouch = false;
                if (!isDragging && isTouch && (Input.GetTouch(0).phase == TouchPhase.Began))
                    InitDrag();
                if (isDragging && isTouch && (Input.GetTouch(0).phase == TouchPhase.Moved))
                    MoveCamera();
                if (isDragging && isTouch && (Input.GetTouch(0).phase == TouchPhase.Ended))
                    FinishDrag();

                if (Cognitics.Unity.TouchInput.pivotPoint == Vector3.zero)
                    Cognitics.Unity.TouchInput.pivotPoint = Camera.main.transform.position;
                if (Mathf.Abs(Cognitics.Unity.TouchInput.pinchDistanceDelta) > 0)
                    pinchAmount = Cognitics.Unity.TouchInput.pinchDistanceDelta;

                if (Mathf.Abs(Cognitics.Unity.TouchInput.turnAngleDelta) > 0)
                {
                    Vector3 rotationDeg = Vector3.zero;
                    rotationDeg.y = -Cognitics.Unity.TouchInput.turnAngleDelta;
                    desiredRotation *= Quaternion.Euler(rotationDeg);
                }
                if (Cognitics.Unity.TouchInput.touchDirection > 0)
                {
                    Vector3 angle = gameObject.transform.localEulerAngles + new Vector3(Cognitics.Unity.TouchInput.twoTouchDelta / lookSensitivity, 0, 0);
                    angle.x = Mathf.Clamp(angle.x, 10, 80);
                    transform.localEulerAngles = angle;
                }
                if (Cognitics.Unity.TouchInput.pivotPoint != Vector3.zero)
                    transform.RotateAround(Cognitics.Unity.TouchInput.pivotPoint, Vector3.up, desiredRotation.y * rotationSensitivity * -1);
                MoveForward(pinchAmount);
                Cognitics.Unity.TouchInput.twoTouchDelta = 0f;

                //Mathf.Clamp(transform.rotation.y, 10, 80);

                if (!Cognitics.Unity.TouchInput.checkForPoint)
                {
                    checkForPointTimeout += Time.deltaTime;
                    if (checkForPointTimeout >= .5f)
                    {
                        checkForPointTimeout = 0f;
                        Cognitics.Unity.TouchInput.checkForPoint = true;
                    }
                }
            }
            else
            {
                if (toggle.btnPressed)
                {
                    toggle.btnText.text = "Pan";
                    // Touch 0 should be the 'pan'button.
                    // Touch 1 should be the pan movement
                    if (Input.touchCount >= 2)
                        Pan(Input.GetTouch(1));
                }
                else
                {
                    toggle.btnText.text = "Hold to Pan";
                    if (Input.touchCount == 1)
                        Rotate(Input.GetTouch(0));
                    if (Input.touchCount >= 2)
                    {
                        Touch touch1 = Input.GetTouch(0);
                        Touch touch2 = Input.GetTouch(1);

                        Vector2 TouchDelta = (touch1.deltaPosition + touch2.deltaPosition) / 2;
                        Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;
                        Vector2 touch2PrevPos = touch2.position - touch2.deltaPosition;
                        double prevTouchDistance = Vector2.Distance(touch1PrevPos, touch2PrevPos);
                        double touchDistance = Vector2.Distance(touch1.position, touch2.position);
                        int touchDirection = (touchDistance - prevTouchDistance) > 0 ? 1 : -1;
                        float translationSpeed = touchDirection * SpeedSlider * TouchDelta.magnitude * DETAIL_MODE_SPEEDFACTOR;
                        MoveForward(translationSpeed);
                    }
                }
            }
            if (Input.GetMouseButton(1))
            {
                float currentX = Mathf.Lerp(lastX, Input.GetAxis("Mouse X"), MouseSensitivity);
                float currentY = Mathf.Lerp(lastY, -Input.GetAxis("Mouse Y"), MouseSensitivity);
                lastX = currentX;
                lastY = currentY;
                transform.localEulerAngles += new Vector3(currentY, currentX, 0f);
            }
            if (Input.GetMouseButton(2))
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = -Input.GetAxis("Mouse Y");
                transform.position += new Vector3(-SpeedFactor() * 10.0f * mouseX * Time.deltaTime, SpeedFactor() * 10.0f * mouseY * Time.deltaTime, 0.0f);
            }
        }


        if (Input.GetKey(KeyCode.LeftArrow))
        {
            rotationY -= RotationSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Euler(rotationX, rotationY, 0.0f);
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            rotationY += RotationSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Euler(rotationX, rotationY, 0.0f);
        }
        if (Input.GetKey(KeyCode.UpArrow))
        {
            rotationX -= RotationSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Euler(rotationX, rotationY, 0.0f);
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            rotationX += RotationSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Euler(rotationX, rotationY, 0.0f);
        }
        if (!(EventSystem.current
            && EventSystem.current.currentSelectedGameObject
            && EventSystem.current.currentSelectedGameObject.GetComponentInChildren<InputField>()))
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.I))
                MoveForward();
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.K))
                MoveBackward();
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.J))
                MoveLeft();
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.L))
                MoveRight();
            if (Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.Space))
                MoveUp();
            if (Input.GetKey(KeyCode.Z))
                MoveDown();
            if (Input.GetKey(KeyCode.KeypadMinus))
                MoveHigher();
            if (Input.GetKey(KeyCode.KeypadPlus))
                MoveLower();
            if (Input.GetKeyDown(KeyCode.R))
                Reset();
        }

        if (surfaceCollider != null)
        {
            surfaceCollider.TerrainElevationGetter();
            float diffY = transform.position.y - ((float)surfaceCollider.minCameraElevation + MinimumElevation * 0.1f);
            if (diffY < 0.0f)
                transform.Translate(Vector3.up * -diffY);
        }
    }
    private bool IsPointerOverUIObject()
    {
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }

    #endregion

}
