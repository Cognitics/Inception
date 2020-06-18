using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
#endif

public class BuildDate : MonoBehaviour

#if UNITY_EDITOR
#pragma warning disable 618
, IPreprocessBuild
#pragma warning restore 618
#endif

{
    public GameObject BuildVersionText;
    public TextAsset BuildDateTextAsset;

    private void Start()
    {
        if (BuildDateTextAsset != null)
            BuildVersionText.GetComponent<Text>().text += BuildDateTextAsset.text;
        else
            BuildVersionText.GetComponent<Text>().text += "N/A";

    }

    public string s_BuildDate
    {
        get
        {
            return BuildDateTextAsset.text;
        }
    }

#if UNITY_EDITOR
    public int callbackOrder { get { return 0; } }

    public void OnPreprocessBuild(BuildTarget target, string path)
    {
        Debug.Log("MyCustomBuildProcessor.OnPreprocessBuild for target " + target + " at path " + path);

        string builddate = System.DateTime.Now.ToString("yyyy/MM/dd_hh:mm");
        
        Debug.Log("builddate:" + builddate);

        string outfile = "Assets/BuildInfo.txt";

        Debug.Log("path = '" + outfile + "'");

        System.IO.File.WriteAllText(outfile, builddate + "\n");

        AssetDatabase.Refresh();
    }
#endif
}