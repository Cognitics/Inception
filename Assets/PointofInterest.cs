
using UnityEngine;

public class PointofInterest : MonoBehaviour
{
    
    public void Seattle()
    { 
        Camera.main.transform.parent.position = new Vector3(-1138.788f, 77.921f, 653.2443f);
        Camera.main.transform.parent.rotation = Quaternion.Euler(new Vector3(14.692f, 40.301f, 0f));
    }
    public void Portland()
    {
        Camera.main.transform.parent.position = new Vector3(-1446.17f, 97.73677f, -2752.33f);
        Camera.main.transform.parent.rotation = Quaternion.Euler(new Vector3(21.872f, 11.46f, 0f));
    }
    public void MtStHelens()
    {
        Camera.main.transform.parent.position = new Vector3(-748.1101f, 35.3013f, -1167.421f);
        Camera.main.transform.parent.rotation = Quaternion.Euler(new Vector3(5.714f, -159.471f, 0f));
    }
    public void MtHood()
    {
        Camera.main.transform.parent.position = new Vector3(4.763175f, 61.95778f, -2935.065f);
        Camera.main.transform.parent.rotation = Quaternion.Euler(new Vector3(9.131001f, -60.997f, 0f));
    }
}
