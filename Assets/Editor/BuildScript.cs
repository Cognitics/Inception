using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Assets.Editor
{
    public class BuildScript
    {
        static void Build()
        {
            string[] scenes = { "Assets/Scenes/SampleScene.unity" };
            BuildPipeline.BuildPlayer(scenes, "D:/CDBProductivitySuite/Build/Inception/Inception/Build/Inception.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        }
    }
}
