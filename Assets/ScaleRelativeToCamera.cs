/// ScaleRelativeToCamera.cs
/// Hayden Scott-Baron (Dock) - http://starfruitgames.com
/// 19 Oct 2012
/// 
/// Scales object relative to camera. 
/// Useful for GUI and items that appear in the world space. 

using UnityEngine;
using System.Collections;

public class ScaleRelativeToCamera : MonoBehaviour
{
    public Camera Camera;
    public float objectScale = 1f;
    private float initialScale = 0.01f;
    private LineRenderer lr;
    private const int LINE_SCALE = 5;
    Vector3 objectSize;
    Vector3 faceCamera;

    // set the initial scale, and setup reference camera
    void Start()
    {
        // record initial scale, use this as a basis
        objectSize = new Vector3(initialScale, initialScale, initialScale);
        Camera = Camera.main;
        lr = gameObject.GetComponent<LineRenderer>();
    }

    // scale object relative to distance from camera plane
    void Update()
    {
        faceCamera = Camera.transform.position - transform.position;

        faceCamera.x = faceCamera.z = 0.0f;
        transform.LookAt(Camera.transform.position - faceCamera);
        transform.Rotate(0, 180, 0);
        Plane plane = new Plane(Camera.transform.forward, Camera.transform.position);
        float dist = plane.GetDistanceToPoint(transform.position);
        transform.localScale = objectSize * dist * objectScale;
        if (lr != null)
        {
            lr.startWidth = objectSize.x * dist * objectScale * LINE_SCALE;
            lr.endWidth = objectSize.x * dist * objectScale * LINE_SCALE;
        }
    }
}