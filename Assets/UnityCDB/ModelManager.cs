using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LODData
{
    public Mesh mesh = null;
    public float switchInDistanceSq = 0f;
    public float switchOutDistanceSq = 0f;

    public void CalculateFromSignificantSize(float significantSize)
    {
        //switchInDistanceSq = 
        //switchOutDistanceSq = 
    }
}

public class ModelEntry
{
    public MeshEntry meshEntry;
}

public class ModelManager
{
    public GameObject UserObject = null;
    public Dictionary<Model, ModelEntry> Models = new Dictionary<Model, ModelEntry>();

    // TODO: stuff models in a quadtree or octree that we will use for faster distance calculations

    // TODO: yield as desired so that we don't take on too much per frame
    public void Run()
    {
        if (UserObject == null)
            UserObject = GameObject.Find("UserObject");

        // TODO: consider keeping a separate data structure for models still in the load phase
        foreach (var modelEntry in Models)
        {
            if (!modelEntry.Key.Loaded)
                modelEntry.Key.RunLoad();

            // TEMP: this activates LOD switching, but it's too slow w/o the octree in place
            //if (modelEntry.Key.Loaded && modelEntry.Value.meshEntry != null && modelEntry.Value.meshEntry.Lods != null)
            //{
            //    // Select LOD mesh based on distance
            //    foreach (var lod in modelEntry.Value.meshEntry.Lods)
            //    {
            //        float SwitchInDistanceSq = lod.switchInDistanceSq;
            //        float SwitchOutDistanceSq = lod.switchOutDistanceSq;
            //        float distSq = DistanceSq(modelEntry.Key);
            //        if (distSq >= SwitchInDistanceSq && distSq < SwitchOutDistanceSq)
            //        {
            //            var meshFilter = modelEntry.Key.GetComponent<MeshFilter>();
            //            if (meshFilter.mesh.triangles.Length != lod.mesh.triangles.Length) // TODO: fix faked equivalence test
            //                meshFilter.mesh = lod.mesh;
            //            break;
            //        }
            //    }
            //}
        }
    }

    public float Distance(Model model)
    {
        float dist = Mathf.Sqrt(DistanceSq(model));
        return dist;
    }

    // Square of distance to the center of the model
    // NOTE/TODO: This isn't a great choice for larger models but it's cheap; we could provide a method that gives distance to nearest point on bounding box, but it may be overkill
    public float DistanceSq(Model model)
    {
        float distSq = Vector3.SqrMagnitude(model.transform.position - UserObject.transform.position);
        return distSq;
    }
}
