
using UnityEngine;
using UnityEngine.UI;

public class CanvasPlatformScaling : MonoBehaviour
{
    void Start()
    {
#if UNITY_ANDROID
        GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        GetComponent<CanvasScaler>().referenceResolution.Set(1920, 1080);
#endif
#if UNITY_UNITY_STANDALONE_WIN
        GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
#endif
    }
}
