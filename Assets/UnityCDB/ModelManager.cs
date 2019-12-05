using System.Collections.Generic;
using UnityEngine;
using Cognitics.CoordinateSystems;
using Cognitics.UnityCDB;

public class LODData
{
    public Mesh mesh = null;
    public float switchInDistanceSq = 0f;
    public float switchOutDistanceSq = 0f;
    public static float multiplier = 1222.310099f; // TODO: check on this.. it (presumably) assumes 30 degree FOV with 1920 width (2*1920/PI=1222.3)

    // TODO/NOTE: If field-of-view and/or resolution changes are permitted during an app run, these values should be recalculated for best presentation
    public void CalculateFromSignificantSize(float significantSize)
    {
        float fov = 30f; // TODO: read our fov
        float screenWidth = 1920; // TODO: read our width
        // Apply unwinded multiplier to get the switch-out distance
        float switchOutDistance = significantSize * multiplier / 10f * 30f / fov * screenWidth / 1920;
        switchOutDistanceSq = switchOutDistance * switchOutDistance;
        // TODO: Switch-in distance should match switch-out distance of prior LOD
        switchInDistanceSq = 0f;
    }
}

public class ModelManager
{
    public Database Database = null;
    public GameObject UserObject = null;
    public List<Model> Models = new List<Model>();
    public int ValidCount = 0;

    private bool _enableLods = false; // debugging
    private bool _lastEnableLods = false;

    // stuff models in an octree to reduce the subset for distance testing
    private Octree _octree = null;
    private Camera _intersectionCamera = null;
    private Plane[] planes = new Plane[6]; // the planes for the camera in question
    private List<Model> results = new List<Model>();

    public void Init(CartesianBounds cartesianBounds)
    {
        UserObject = GameObject.Find("UserObject");

        BuildOctree(cartesianBounds);

        _intersectionCamera = UserObject.GetComponentInChildren<Camera>();
        _lastEnableLods = _enableLods;
    }

    // Add to list for processing (for RunLoad execution, etc).
    // NOTE: This does not add it to the octree because we need computed bounds in each Model's MeshRenderer before we can do that.
    public void Add(Model model)
    {
        Models.Add(model);
    }

    protected void AddToOctree(Model model)
    {
        _octree.Add(model);
    }

    public void Remove(Model model)
    {
        Models.Remove(model);
        _octree.Remove(model);
    }

    public void BuildOctree(CartesianBounds cartesianBounds)
    {
        float centerX = (float)(cartesianBounds.MinimumCoordinates.X + cartesianBounds.MaximumCoordinates.X) / 2f;
        float centerZ = (float)(cartesianBounds.MinimumCoordinates.Y + cartesianBounds.MaximumCoordinates.Y) / 2f;
        float sizeX = (float)(cartesianBounds.MaximumCoordinates.X - cartesianBounds.MinimumCoordinates.X);
        float sizeZ = (float)(cartesianBounds.MaximumCoordinates.Y - cartesianBounds.MinimumCoordinates.Y);
        // NOTE: This is offset in y in a naive way, just so that two nodes on top of one another 
        // don't have their adjoining edge at y=0 which many model bounds would intersect. So it improves the insertion quality for our purposes
        var boundary = new Bounds(new Vector3(centerX, 100f, centerZ), new Vector3(sizeX, 1000f, sizeZ));

        _octree = new Octree(boundary, 1);
        _octree.Build(Models);
    }

    public float Distance(Model model)
    {
        float dist = Mathf.Sqrt(DistanceSq(model));
        return dist;
    }

    public float DistanceClosestPoint(Model model)
    {
        float dist = Mathf.Sqrt(DistanceSqClosestPoint(model));
        return dist;
    }

    // Square of distance to the model's position
    public float DistanceSq(Model model)
    {
        float distSq = Vector3.SqrMagnitude(model.transform.position - UserObject.transform.position);
        return distSq;
    }

    // Square of distance to the closest point on the model's bounds
    public float DistanceSqClosestPoint(Model model)
    {
        Vector3 pt = model.GetComponent<MeshRenderer>().bounds.ClosestPoint(UserObject.transform.position);
        float distSq = Vector3.SqrMagnitude(pt - UserObject.transform.position);
        return distSq;
    }

    public void HighlightForTag(string tag)
    {
        foreach (var model in Models)
            model.HighlightForTag(tag);
    }

}
