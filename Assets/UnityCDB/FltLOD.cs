using UnityEngine;

// NOTE: This could easily be merged into Model.cs so we keep one script per object.
public class FltLOD : MonoBehaviour
{
    public GameObject UserObject = null;
    public Vector3 Center;
    public float SwitchInDistance = 0f;
    public float SwitchOutDistance = 0f;
    public bool SkipUpdate = false;

    private bool _enable = true;

    #region MonoBehaviour

    protected void Update()
    {
        if (SkipUpdate || UserObject == null)
            return;

        // TODO: when SwitchInDistance = 0, SwitchOutDistance = 0, leverage LevelOfDetail record's 'significantSize' field to calculate good values
        bool enable = _enable;
        float distSq = Vector3.SqrMagnitude(transform.TransformPoint(Center) - UserObject.transform.position);
        enable = (distSq >= SwitchOutDistance * SwitchOutDistance) && (distSq < SwitchInDistance * SwitchInDistance);
        if (enable != _enable)
        {
            _enable = enable;
            for (int i = 0; i < transform.childCount; i++)
                transform.GetChild(i).gameObject.SetActive(enable);
        }
    }

    #endregion
}
