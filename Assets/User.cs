
using UnityEngine;
using UnityEngine.UI;

public class User : MonoBehaviour
{
    private ModeButton mb;
    public float MouseSensitivity = 100.0f;
    public float TouchSensitivity = 0.04f;

    public float NormalSpeed = 10.0f;
    public float ShiftSpeed = 100.0f;

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
            if (mb.isDetailMode)
                return ShiftSpeed * SpeedSlider * 0.2f;
            else
                return ShiftSpeed * SpeedSlider;
        }
        else
        {
            if (mb.isDetailMode)
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

    public void Reset()
    {
        rotationY = 0.0f;
        rotationX = 0.0f;
        transform.SetPositionAndRotation(new Vector3(0f, 1000.0f, 0f), new Quaternion(0f, 0f, 0f, 0f));
    }

    #region implementation

    private float rotationY = 0.0f; // rotation around the up/y axis
    private float rotationX = 0.0f; // rotation around the right/x axis
    private YesNoDialog yesNoDialog = null;

    private void Awake()
    {
        Input.simulateMouseWithTouches = false;
        yesNoDialog = YesNoDialog.Instance();
        yesNoDialog.ClosePanel();
        mb = GameObject.Find("Mode").GetComponent<ModeButton>();
    }

    private void CancelQuit()
    {
    }

    private void ConfirmQuit()
    {
#if UNITY_ANDROID
            // Get the unity player activity
            AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
            // call activity's boolean moveTaskToBack(boolean nonRoot) function
            // documentation: http://developer.android.com/reference/android/app/Activity.html#moveTaskToBack(boolean)
            activity.Call<bool>("moveTaskToBack", true);   //To suspend
#else
        Application.Quit();
#endif
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.G))
        {
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
        }


        if (Input.GetKeyUp(KeyCode.Escape))
        {
            yesNoDialog.Choice("Are you sure you wish to quit?", ConfirmQuit, CancelQuit);
            return;
        }

        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = -Input.GetAxis("Mouse Y");
            rotationY += mouseX * MouseSensitivity * Time.deltaTime;
            rotationX += mouseY * MouseSensitivity * Time.deltaTime;
            rotationX = Mathf.Clamp(rotationX, -ClampAngle, ClampAngle);
            transform.rotation = Quaternion.Euler(rotationX, rotationY, 0.0f);
        }
        if (Input.GetMouseButton(2))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = -Input.GetAxis("Mouse Y");
            transform.position += new Vector3(-SpeedFactor() * 10.0f * mouseX * Time.deltaTime, SpeedFactor() * 10.0f * mouseY * Time.deltaTime, 0.0f);
        }
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
        if (Input.GetKey(KeyCode.R))
            Reset();

        var surfaceCollider = GetComponent<Cognitics.UnityCDB.SurfaceCollider>();
        if (surfaceCollider != null)
        {
            float diffY = transform.position.y - ((float)surfaceCollider.minCameraElevation + MinimumElevation * 0.1f);
            if (diffY < 0.0f)
                transform.Translate(Vector3.up * -diffY);
        }
    }



    #endregion

}
