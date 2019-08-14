
using UnityEngine;

public class Arrows : MonoBehaviour
{
    public GameObject UserObject;
    private bool isUpButtonPressed = false;
    private bool isDownButtonPressed = false;

    void Start()
    {

    }


    void Update ()
    {
        if (isUpButtonPressed)
            UserObject.transform.position += UserObject.GetComponent<User>().SpeedFactor() * Time.deltaTime * Vector3.up;

        if (isDownButtonPressed)
            UserObject.transform.position += UserObject.GetComponent<User>().SpeedFactor() * Time.deltaTime * -Vector3.up;
    }

    public void UpButtonPressed()
    {
        isUpButtonPressed = true;
    }
    public void UpButtonReleased()
    {
        isUpButtonPressed = false;
    }

    public void DownButtonPressed()
    {
        isDownButtonPressed = true;
    }
    public void DownButtonReleased()
    {
        isDownButtonPressed = false;
    }
}
