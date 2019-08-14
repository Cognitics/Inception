

namespace Cognitics.UnityCDB
{
    public class WriteToExternalStorage
    {
        public static string GetAndroidExternalFilesDir()
        {
            using (UnityEngine.AndroidJavaClass unityPlayer = new UnityEngine.AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (UnityEngine.AndroidJavaObject context = unityPlayer.GetStatic<UnityEngine.AndroidJavaObject>("currentActivity"))
                {
                    // Get all available external file directories (emulated and sdCards)
                    UnityEngine.AndroidJavaObject[] externalFilesDirectories = context.Call<UnityEngine.AndroidJavaObject[]>("getExternalFilesDirs", (object)null);
                    UnityEngine.AndroidJavaObject emulated = null;
                    UnityEngine.AndroidJavaObject sdCard = null;

                    for (int i = 0; i < externalFilesDirectories.Length; i++)
                    {
                        UnityEngine.AndroidJavaObject directory = externalFilesDirectories[i];
                        using (UnityEngine.AndroidJavaClass environment = new UnityEngine.AndroidJavaClass("android.os.Environment"))
                        {
                            // Check which one is the emulated and which the sdCard.
                            bool isRemovable = environment.CallStatic<bool>("isExternalStorageRemovable", directory);
                            bool isEmulated = environment.CallStatic<bool>("isExternalStorageEmulated", directory);
                            if (isEmulated)
                                emulated = directory;
                            else if (isRemovable && isEmulated == false)
                                sdCard = directory;
                        }
                    }
                    // Return the sdCard if available
                    if (sdCard != null)
                        return sdCard.Call<string>("getAbsolutePath");
                    else
                        return emulated.Call<string>("getAbsolutePath");
                }
            }
        }
    }
}