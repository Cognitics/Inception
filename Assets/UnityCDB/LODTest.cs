using UnityEngine;
using Cognitics.UnityCDB;

// NOTE: This script uses Unity's LODGroup. See FltLOD.cs for a different approach.
public class LODTest : MonoBehaviour
{
    LODGroup lodGroup = null;

    void Start()
    {
        // LOD-related statics, for reference if nothing else
        //LODGroup.crossFadeAnimationDuration = 0.1f;
        //QualitySettings.lodBias = 1;
        //QualitySettings.maximumLODLevel = 0;

        lodGroup = gameObject.GetOrAddComponent<LODGroup>();
        lodGroup.fadeMode = LODFadeMode.CrossFade;
        lodGroup.animateCrossFading = true;

        // NOTE: QualitySettings.lodBias factors into the final calculations
        LOD[] lods = new LOD[3];
        lods[0].screenRelativeTransitionHeight = 0.6f;
        lods[1].screenRelativeTransitionHeight = 0.3f;
        lods[2].screenRelativeTransitionHeight = 0.1f;

        var renderers = gameObject.GetComponentsInChildren<Renderer>();
        lods[0].renderers = new Renderer[1] { renderers[0] };
        lods[1].renderers = new Renderer[1] { renderers[1] };
        lods[2].renderers = new Renderer[1] { renderers[2] };
        lodGroup.SetLODs(lods);
    }
}
