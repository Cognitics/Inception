
#if UNITY_EDITOR && !UNITY_ANDROID && !UNITY_IOS
using UnityEngine;
using UnityEditor;

namespace Cognitics.UnityCDB
{
    public class GenerateAirspaceData
    {
        [MenuItem("Project/Generate Airspace Data")]
        public static AirspaceData Create()
        {
            var asset = ScriptableObject.CreateInstance<AirspaceData>();
            AssetDatabase.CreateAsset(asset, "Assets/AirspaceData.asset");
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
#endif
