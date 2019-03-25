
using UnityEngine;

public class ApplicationState : MonoBehaviour
{


    void Start()
    {
        ReduceStackTraceSpam();
    }


    void ReduceStackTraceSpam()
    {
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
    }


}
