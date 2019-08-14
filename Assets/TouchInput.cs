using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchInput : MonoBehaviour
{
    static GameObject terrainTester;
    static Cognitics.UnityCDB.SurfaceCollider SurfaceCollider;
    static public bool checkForPoint = true;
    const float pinchTurnRatio = Mathf.PI / 2;
    const float minTurnAngle = 1;
    const float pinchRatio = 1;
    const float minPinchDistance = 1;
    const float panRatio = 1;
    const float minPanDistance = 0;

    static public float turnAngleDelta;
    static public float turnAngle;
    static public float pinchDistanceDelta;
    static public float pinchDistance;
    static public float touchDirection;
    static public float twoTouchDelta;
    static public Vector3 pivotPoint;

    static public void Calculate()
    {
        pinchDistance = pinchDistanceDelta = 0;
        turnAngle = turnAngleDelta = 0;
        terrainTester = GameObject.Find("TerrainTester");
        SurfaceCollider = terrainTester.GetComponent<Cognitics.UnityCDB.SurfaceCollider>();

        if (Input.touchCount == 2)
        {
            Touch touch1 = Input.touches[0];
            Touch touch2 = Input.touches[1];
            
            if(touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved || Input.GetKey(KeyCode.F))
            {
                Ray ray = Camera.main.ViewportPointToRay(new Vector2(.5f, .5f));
                terrainTester.transform.position = Camera.main.transform.position;
                
                while (true)
                {
                    if (!checkForPoint)
                        break;
                    terrainTester.transform.position += ray.direction * 15f;
                    SurfaceCollider.TerrainElevationGetter();
                    if (terrainTester.transform.position.y < SurfaceCollider.minCameraElevation)
                        break;
                    if (Vector3.SqrMagnitude(terrainTester.transform.position - Camera.main.transform.position) > (50000f * 50000f))
                    {
                        pivotPoint = Vector3.positiveInfinity;
                        checkForPoint = false;
                    }
                }

                pivotPoint = terrainTester.transform.position;

                touchDirection = Vector2.Dot(touch1.deltaPosition, touch2.deltaPosition);
                twoTouchDelta = touch1.deltaPosition.y + touch2.deltaPosition.y;
                pinchDistance = Vector2.Distance(touch1.position, touch2.position);
                float prevDistance = Vector2.Distance(touch1.position - touch1.deltaPosition, touch2.position - touch2.deltaPosition);

                pinchDistanceDelta = pinchDistance - prevDistance;

                if (Mathf.Abs(pinchDistanceDelta) > minPinchDistance)
                    pinchDistanceDelta *= pinchRatio;
                else
                    pinchDistance = pinchDistanceDelta = 0;

                turnAngle = Angle(touch1.position, touch2.position);
                float prevTurn = Angle(touch1.position - touch1.deltaPosition, touch2.position - touch2.deltaPosition);
                turnAngleDelta = Mathf.DeltaAngle(prevTurn, turnAngle);

                if (Mathf.Abs(turnAngleDelta) > minTurnAngle)
                    turnAngleDelta *= pinchTurnRatio;
                else
                    turnAngle = turnAngleDelta = 0;
            }
        }
    }

    static private float Angle(Vector2 pos1, Vector2 pos2)
    {
        Vector2 from = pos2 - pos1;
        Vector2 to = new Vector2(1, 0);

        float result = Vector2.Angle(from, to);
        Vector3 cross = Vector3.Cross(from, to);

        if (cross.z > 0)
            result = 360f - result;
        return result;
    }
}
