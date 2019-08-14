
using UnityEngine;
using Cognitics.CoordinateSystems;
using System.Reflection;

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
    public int StartLOD = int.MinValue;

    public GameObject UserObject = null;
    public Cognitics.UnityCDB.Database cdbDatabase = null;

    public static float SystemMemoryUtilization => System.GC.GetTotalMemory(false) / (SystemInfo.systemMemorySize * 1024f * 1024f);   // 0-1
    public static float SharedMemoryUtilization => (System.GC.GetTotalMemory(false) + (long)Texture.currentTextureMemory) / (SystemInfo.systemMemorySize * 1024f * 1024f);   // 0-1

    public static ApplicationState Instance => GameObject.Find("Application").GetComponent<ApplicationState>();

    void Start()
    {
        ReduceStackTraceSpam();

        StartPosition.Set(0f, 50f, 0f);

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
                //StartLOD = 0;
                StartLOD = 4;
                GeographicBounds = new GeographicBounds(new GeographicCoordinates(39.04, -85.55), new GeographicCoordinates(39.06, -85.50));
                // GS LOD = 4 (2-3)
                // GT LOD = 1
                break;
            case StartStateValue.DUET:
                StartPosition.Set(0.0f, 40.0f, 0.0f);
                GeographicBounds = new GeographicBounds(new GeographicCoordinates(47.58, -122.39), new GeographicCoordinates(47.64, -122.30));
                StartLOD = 2;
                break;
        }
    }

    void Update()
    {
        Position = UserObject.transform.position;
        Rotation = UserObject.transform.rotation;
        EulerAngles = UserObject.transform.eulerAngles;

        if (Input.GetKeyDown(KeyCode.M))
        {
            Debug.LogFormat("SystemInfo.systemMemorySize: {0} MB", SystemInfo.systemMemorySize);
            Debug.LogFormat("SystemInfo.graphicsMemorySize: {0} MB", SystemInfo.graphicsMemorySize);
            Debug.LogFormat("System.GC.GetTotalMemory(false): {0} MB", System.GC.GetTotalMemory(false) / 1024 / 1024);
            Debug.LogFormat("SystemMemoryUtilization: {0}", SystemMemoryUtilization);
            Debug.LogFormat("SharedMemoryUtilization: {0}", SharedMemoryUtilization);
        }

        if (cdbDatabase)
        {
            cdbDatabase.SystemMemoryUtilization = SystemMemoryUtilization;
            cdbDatabase.SharedMemoryUtilization = SharedMemoryUtilization;
        }

    }

    static void ReduceStackTraceSpam()
    {
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
    }

#if UNITY_EDITOR
    static public void ClearEditorLog()
    {
        var assembly = Assembly.GetAssembly(typeof(UnityEditor.Editor));
        var type = assembly.GetType("UnityEditor.LogEntries");
        var method = type.GetMethod("Clear");
        method.Invoke(new object(), null);
    }
#endif
}
