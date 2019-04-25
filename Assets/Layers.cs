
using UnityEngine;
using UnityEngine.UI;
using Cognitics.UnityCDB;

public class Layers : MonoBehaviour
{
    [HideInInspector] public Database Database = null;
    public UnityEngine.UI.Toggle template = null;

    // TODO: we will want to pull these in dynamically based on an enum or name lookup
    private readonly string[] layerStrings = new string[3]
    {
        "Structures",
        "Structures (Geo-specific)",
        "Trees",
    };

    protected void Start()
    {
        if (Database == null)
            return;

        if (template != null)
        {
            int index = 0;
            foreach (var str in layerStrings)
            {
                var gameObj = GameObject.Instantiate(template.gameObject, template.transform.parent, false);
                gameObj.name = str;
                var text = gameObj.GetComponentInChildren<Text>();
                text.text = str;
                var toggle = gameObj.GetComponentInChildren<UnityEngine.UI.Toggle>();
                switch (index)
                {
                    case 0: toggle.isOn = Database.ManMadeData != null; break;
                    case 1: toggle.isOn = Database.ManMadeDataSpecific != null; break;
                    case 2: toggle.isOn = Database.TreeData != null; break;
                }
                gameObj.SetActive(true);
                index++;
            }
        }
    }

    public void LayerToggle(UnityEngine.UI.Toggle toggle)
    {
        if (Database == null)
            return;

        int index = toggle.gameObject.transform.GetSiblingIndex() - 1; // subtract 1 for the (deactivated) template that should always be the first sibling
        if (toggle.isOn)
        {
            // load or show features
            switch (index)
            {
                case 0: Database.InitializeGTFeatureLayer(ref Database.ManMadeData, Database.DB.GTFeature.ManMade, 0); break;
                case 1: Database.InitializeGSFeatureLayer(ref Database.ManMadeDataSpecific, Database.DB.GSFeature.ManMade, 0); break;
                case 2: Database.InitializeGTFeatureLayer(ref Database.TreeData, Database.DB.GTFeature.Trees, 0); break;
                default:
                    throw new System.ArgumentOutOfRangeException("index", "invalid feature data index" + index);
            }
        }
        else
        {
            // unload or hide features
            switch (index)
            {
                case 0: Database.DestroyGTFeatureLayer(ref Database.ManMadeData); break;
                case 1: Database.DestroyGSFeatureLayer(ref Database.ManMadeDataSpecific); break;
                case 2: Database.DestroyGTFeatureLayer(ref Database.TreeData); break;
                default:
                    throw new System.ArgumentOutOfRangeException("index", "invalid feature data index" + index);
            }
        }
    }
}
