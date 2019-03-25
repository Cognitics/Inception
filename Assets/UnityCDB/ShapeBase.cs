using System.IO;
using UnityEngine;

namespace Cognitics.UnityCDB
{
    // Base class for shapes containing features
    public abstract class ShapeBase : MonoBehaviour
    {
        public VolumetricFeature featureTemplate = null;

        [HideInInspector] public Database Database;
        [HideInInspector] public GameObject UserObject = null;
        [HideInInspector] public string Path = null;
        [HideInInspector] public string Filename = null;

        #region MonoBehaviour

        protected void Start()
        {
            // Read all of the features in the shapefile, then create them
            string filename = string.Format("{0}/{1}", Path, Filename);
            if (File.Exists(filename))
            {
                var features = CDB.Shapefile.ReadFeatures(filename);
                foreach (var feature in features)
                {
                    var volumetricFeature = Instantiate(featureTemplate, featureTemplate.transform.parent, false);
                    volumetricFeature.UserObject = UserObject;
                    volumetricFeature.Database = Database;
                    volumetricFeature.Feature = feature;
                    volumetricFeature.gameObject.SetActive(true);
                }
            }
            else
            {
                Debug.LogErrorFormat("shapefile not found: {0}", filename);
            }
        }

        #endregion
    }
}
