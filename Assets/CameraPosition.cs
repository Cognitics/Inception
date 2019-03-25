
using UnityEngine;
using Cognitics.CoordinateSystems;

public class CameraPosition : MonoBehaviour
{
    public GameObject UserObject = null;
    public ScaledFlatEarthProjection Projection;

    [HideInInspector] public Vector3 position = new Vector3(0.0f, 0.0f, 0.0f);

    public float updateRateSeconds = 4.0F;

    int frameCount = 0;
    float dt = 0.0F;
    float fps = 0.0F;

    CartesianCoordinates cartesianCoordinates = new CartesianCoordinates();

    UnityEngine.UI.Text uiText;
    string format = "{0}{1:##0.0000}  {2}{3:###0.0000}   {4:###0.00}   {5:#0.0} FPS";


    void Awake()
    {
        uiText = GetComponentInChildren<UnityEngine.UI.Text>();
    }

    void Update()
    {
        frameCount++;
        dt += Time.unscaledDeltaTime;
        if (dt > 1.0 / updateRateSeconds)
        {
            fps = frameCount / dt;
            frameCount = 0;
            dt -= 1.0F / updateRateSeconds;
        }

        cartesianCoordinates.X = UserObject.transform.position.x;
        cartesianCoordinates.Y = UserObject.transform.position.z;
        var geographicCoordinates = cartesianCoordinates.TransformedWith(Projection);
        position.x = (float)geographicCoordinates.Longitude;
        position.y = UserObject.transform.position.y / (float)Projection.Scale;
        position.z = (float)geographicCoordinates.Latitude;

        char latChar = (position.z < 0) ? 'S' : 'N';
        char lonChar = (position.x < 0) ? 'W' : 'E';
        float lat = (position.z < 0) ? -position.z : position.z;
        float lon = (position.x < 0) ? -position.x : position.x;
        uiText.text = string.Format(format, latChar, lat, lonChar, lon, position.y, fps);

	}
}
