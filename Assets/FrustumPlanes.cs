using UnityEngine;

public class FrustumPlanes : MonoBehaviour
{
    private Camera _camera;
    private Plane[] planes = new Plane[6]; // the planes for the camera in question

    void Start()
    {
        // Default to main camera
        SetCamera(Camera.main);
    }

    void Update()
    {
        //0 = left, 1 = right, 2 = down, 3 = up, 4 = near, 5 = far
        GeometryUtility.CalculateFrustumPlanes(_camera, planes);
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Debug.Log(planes[0].ToString());
        }
    }

    public void SetCamera(Camera camera)
    {
        _camera = camera;
        GeometryUtility.CalculateFrustumPlanes(_camera, planes);
    }

    public Plane GetPlane(string direction)
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
            case "near":
                return planes[4];
            case "far":
                return planes[5];
        }
        return new Plane();
    }

    public Plane[] GetPlaneArray()
    {
        return planes;
    }
}
