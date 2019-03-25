using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicLOD : MonoBehaviour
{
    public int MaxFPS = 50;
    public int MinFPS = 20;

    public Cognitics.UnityCDB.Database Database;

    public void HandleFrameRate(float fps)
    {
        if (!Database)
            return;
        if (Database.TileDataCache.WaitingRequests.Count > 0)
            return;
        if (Database.TileDataCache.RunningRequests.Count > 0)
            return;
        if (Database.TriangleCount() > 20 * 1000 * 1000)
            return;
        if(fps < MinFPS)
            --Database.PerformanceOffsetLOD;
        if(fps > MaxFPS)
            ++Database.PerformanceOffsetLOD;
    }

}
