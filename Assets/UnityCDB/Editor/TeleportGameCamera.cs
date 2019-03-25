#if UNITY_EDITOR && !UNITY_ANDROID && !UNITY_IOS
using UnityEngine;
using UnityEditor;

namespace Cognitics.UnityCDB
{
    public class TeleportGameCamera
    {
        [MenuItem("Project/Teleport Game Camera")]
        public static void Execute()
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                var source = sceneView.camera;
                var target = Camera.main.transform.parent; // NB: we are using the user object!
                if (target == null)
                    target = Camera.main.transform; // fallback
                if (source != null && target != null)
                {
                    target.transform.position = source.transform.position;
                    target.transform.rotation = source.transform.rotation;
                }
            }
        }
    }
}
#endif
