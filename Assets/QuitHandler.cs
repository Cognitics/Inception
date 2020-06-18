
using UnityEngine;

public class QuitHandler : MonoBehaviour
{
    private YesNoDialog yesNoDialog = null;

    private void Start()
    {
        yesNoDialog = YesNoDialog.Instance();
        yesNoDialog.ClosePanel();
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            yesNoDialog.Choice("Are you sure you wish to quit?", ConfirmQuit, CancelQuit);
            return;
        }
    }

    private void CancelQuit()
    {
    }

    private void ConfirmQuit()
    {
#if UNITY_ANDROID
            // Get the unity player activity
            AndroidJavaObject activity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
            // call activity's boolean moveTaskToBack(boolean nonRoot) function
            // documentation: http://developer.android.com/reference/android/app/Activity.html#moveTaskToBack(boolean)
            activity.Call<bool>("moveTaskToBack", true);   //To suspend
#else
        Application.Quit();
#endif
    }

}
