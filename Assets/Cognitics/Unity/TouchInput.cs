using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitics.Unity
{
    public class TouchInput : MonoBehaviour
    {
        static GameObject terrainTester;
        static public bool checkForPoint = true;
        const float pinchTurnRatio = Mathf.PI / 2;
        const float minTurnAngle = 1;
        const float pinchRatio = 1;
        const float minPinchDistance = 1;
        const float panRatio = 1;
        const float minPanDistance = 0;

        static private Plane WestWall;
        static private Plane EastWall;
        static private Plane NorthWall;
        static private Plane SouthWall;
        static private Plane Floor;
        static private float dist;

        static public List<Plane> Walls = new List<Plane>();
        static public List<Plane> IntersectedWalls = new List<Plane>();

        static public float turnAngleDelta;
        static public float turnAngle;
        static public float pinchDistanceDelta;
        static public float pinchDistance;
        static public float touchDirection;
        static public float twoTouchDelta;
        static public Vector3 pivotPoint;
        static public int test;
        static public Vector3 singleTouchPoint;
        static public bool cameraInBounds = true;

        static public void Calculate()
        {
            cameraInBounds = IsPointInBounds(Camera.main.transform.position);
            pinchDistance = pinchDistanceDelta = 0;
            turnAngle = turnAngleDelta = 0;
            terrainTester = GameObject.Find("TerrainTester");
            if (Input.touchCount == 2)
            {
                Touch touch1 = Input.touches[0];
                Touch touch2 = Input.touches[1];

                if (touch1.phase == TouchPhase.Moved || touch2.phase == TouchPhase.Moved || Input.GetKey(KeyCode.F))
                {
                    FindPivotPoint();
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
            if(Input.touchCount == 1)
            {
                CalculateOneTouchPosition();
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
        
        static private void CalculateOneTouchPosition()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            float dist = 0f;
            if (cameraInBounds)
            {
                if (PopulateIntersectedWalls(ray))
                {
                    Floor.Raycast(ray, out dist);
                    singleTouchPoint = ray.GetPoint(dist);
                }
                else
                    singleTouchPoint = Vector3.zero;
            }
            else
            {
                CalculateCameraOutOfBounds(ray);
            }
        }

        static public void CreateWalls(CoordinateSystems.CartesianBounds tileBounds)
        {
            WestWall = new Plane(Vector3.right, new Vector3((float)tileBounds.MinimumCoordinates.X, 0, 0));
            EastWall = new Plane(Vector3.left, new Vector3((float)tileBounds.MaximumCoordinates.X, 0, 0));
            NorthWall = new Plane(Vector3.back, new Vector3(0, 0, (float)tileBounds.MaximumCoordinates.Y));
            SouthWall = new Plane(Vector3.forward, new Vector3(0, 0, (float)tileBounds.MinimumCoordinates.Y));

            Walls.Add(WestWall);
            Walls.Add(EastWall);
            Walls.Add(SouthWall);
            Walls.Add(NorthWall);

            Floor = new Plane(Vector3.up, Vector3.zero);
        }

        static private bool PopulateIntersectedWalls(Ray ray)
        {
            bool isOnTerrain = true;
            IntersectedWalls.Clear();
            foreach(Plane wall in Walls)
            {
                if (wall.Raycast(ray, out dist))
                {
                    IntersectedWalls.Add(wall);
                    if (ray.GetPoint(dist).y > 0f)
                        isOnTerrain = false;
                }
            }
            return isOnTerrain;
        }

        static private void CalculateCameraOutOfBounds(Ray ray)
        {
            Floor.Raycast(ray, out dist);
            if (IsPointInBounds(ray.GetPoint(dist)))
                singleTouchPoint = ray.GetPoint(dist);
            else
                singleTouchPoint = Vector3.zero;
        }

        static private void FindPivotPoint()
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector2(.5f, .5f));
            float dist = 0f;
            if (PopulateIntersectedWalls(ray))
            {
                Floor.Raycast(ray, out dist);
                pivotPoint = ray.GetPoint(dist);
            }
            else
                pivotPoint = Vector3.zero;
        }

        static private bool IsPointInBounds(Vector3 pos)
        {
            bool pointInBounds = true;
            foreach(Plane wall in Walls)
            {
                if (!wall.GetSide(pos))
                    pointInBounds = false;
            }
            return pointInBounds;
        }
    }

}