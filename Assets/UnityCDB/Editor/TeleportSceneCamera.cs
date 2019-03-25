#if UNITY_EDITOR && !UNITY_ANDROID && !UNITY_IOS
using UnityEngine;
using UnityEditor;

namespace Cognitics.UnityCDB
{
    public class TeleportSceneCamera
    {
        [MenuItem("Project/Teleport Scene Camera")]
        public static void Execute()
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                var source = Camera.main;
                var target = sceneView.camera;
                if (source != null && target != null)
                {
                    target.transform.position = source.transform.position;
                    target.transform.rotation = source.transform.rotation;
                    sceneView.AlignViewToObject(target.transform);
                }
            }
        }
    }
}
#endif
