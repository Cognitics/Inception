#if UNITY_EDITOR && !UNITY_ANDROID && !UNITY_IOS
using UnityEngine;
using UnityEditor;

namespace Cognitics.UnityCDB
{
    public class FindMissingReferences
    {
        [MenuItem("Project/Find Missing References on Selected Objects")]
        public static void Execute()
        {
            bool foundMissing = false;
            bool foundEmpty = false;

            foreach (var gameObject in Selection.gameObjects)
            {
                var components = gameObject.GetComponents<Component>();
                foreach (var component in components)
                {
                    if (component == null)
                    {
                        foundMissing = true;
                        Debug.LogError(string.Format("Missing component reference found in: {0}", gameObject), gameObject);
                        continue;
                    }

                    SerializedObject serializedObject = new SerializedObject(component);
                    var serializedProperty = serializedObject.GetIterator();
                    while (serializedProperty.NextVisible(true))
                    {
                        if (serializedProperty.propertyType == SerializedPropertyType.ObjectReference && serializedProperty.objectReferenceValue == null)
                        {
                            if (serializedProperty.objectReferenceInstanceIDValue != 0)
                            {
                                foundMissing = true;
                                Debug.LogError(string.Format("Missing reference found in: {0}, property: {1}", gameObject, serializedProperty.name), gameObject);
                            }
                            else
                            {
                                foundEmpty = true;
                                Debug.Log(string.Format("Empty reference found in: {0}, property: {1}", gameObject, serializedProperty.name), gameObject);
                            }
                        }
                    }
                }
            }

            if (!foundMissing)
                Debug.Log("no missing references found.");
            if (!foundEmpty)
                Debug.Log("no empty references found.");
        }
    }
}
#endif
