using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrustumPlanes : MonoBehaviour {
    public static Plane[] planes;

    void Update ()
    {
        //0 = left, 1 = right, 2 = down, 3 = up, 4 = near, 5 = far
        planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log(planes[0].ToString());
        }
    }

    public static Plane GetPlane(string direction)
    {
        string plane = direction.ToLower();
        switch (plane)
        {
            case "left":
                return planes[0];
            case "right":
                return planes[1];
            case "down":
                return planes[2];
            case "up":
                return planes[3];
        }
        return new Plane();
    }
    public static Plane[] GetPlaneArray()
    {
        return planes;
    }
}
