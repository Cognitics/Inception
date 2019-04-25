
using UnityEngine;
using Cognitics.CoordinateSystems;


public class ApplicationState : MonoBehaviour
{
    public enum StartStateValue
    {
        Default,
        AdenHD,
        UAVDemo,
        DUET,
    };

    public StartStateValue StartState = StartStateValue.Default;
    public Vector3 Position = Vector3.zero;
    public Vector3 EulerAngles = Vector3.zero;
    public Quaternion Rotation = Quaternion.identity;

    [HideInInspector] public GeographicBounds GeographicBounds = GeographicBounds.EmptyValue;
    [HideInInspector] public Vector3 StartPosition = Vector3.negativeInfinity;
    [HideInInspector] public Vector3 StartEulerAngles = Vector3.negativeInfinity;
    [HideInInspector] public int StartLOD = int.MinValue;

    private GameObject UserObject = null;


    void Awake()
    {
        UserObject = GameObject.Find("UserObject");
    }

    void Start()
    {
        ReduceStackTraceSpam();

        switch (StartState)
        {
            case StartStateValue.AdenHD:
                StartPosition.Set(240.0f, 20.0f, -290.0f);
                StartEulerAngles.Set(0.0f, -70.0f, 0.0f);
                StartLOD = 4;
                GeographicBounds = new GeographicBounds(new GeographicCoordinates(12.75, 45.00), new GeographicCoordinates(12.85, 45.06));
                break;
            case StartStateValue.UAVDemo:
                StartPosition.Set(-30.0f, 50.0f, -80.0f);
                StartEulerAngles.Set(20.0f, 0.0f, 0.0f);
                StartLOD = 4;
                GeographicBounds = new GeographicBounds(new GeographicCoordinates(39.04, -85.55), new GeographicCoordinates(39.06, -85.50));
                // GS LOD = 4 (2-3)
                // GT LOD = 1
                break;
            case StartStateValue.DUET:
                GeographicBounds = new GeographicBounds(new GeographicCoordinates(47, -123), new GeographicCoordinates(48, -122));
                break;
        }
    }

    void Update()
    {
        Position = UserObject.transform.position;
        Rotation = UserObject.transform.rotation;
        EulerAngles = UserObject.transform.eulerAngles;
    }


    void ReduceStackTraceSpam()
    {
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
    }



}
