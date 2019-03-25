
#if UNITY_EDITOR && !UNITY_ANDROID && !UNITY_IOS
using System;
using UnityEngine;
using UnityEditor;

namespace Cognitics.UnityCDB
{
    public class ObjectDump
    {
        private const int numCategories = 5;
        private static UnityEngine.Object[][] Objects = new UnityEngine.Object[numCategories][];
        private static Type[] Types = new Type[numCategories]
        {
            //typeof(UnityEngine.Object),
            typeof(Texture),
            typeof(Mesh),
            typeof(Material),
            typeof(GameObject),
            typeof(Component),
        };

        [MenuItem("Project/Object Dump")]
        public static void Execute()
        {
            string str = "";
            str += "SUMMARY:\n--------\n";
            for (int i = 0; i < Objects.Length; i++)
            {
                Objects[i] = Resources.FindObjectsOfTypeAll(Types[i]);
                str += string.Format("{0}: {1} objects\n", Types[i].ToString(), Objects[i].Length);
            }
            str += "\n";
            str += "DETAIL:\n--------\n";
            for (int i = 0; i < Objects.Length; i++)
            {
                str += string.Format("[{0}]\n", Types[i].ToString());
                for (int j = 0; j < Objects[i].Length; j++)
                    str += string.Format("{0}\n", Objects[i][j].name);
                str += "\n";
            }
            Debug.Log(str);
        }
    }
}
#endif
