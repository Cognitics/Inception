//#define USE_AS_QUADTREE // an experiment
using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Cognitics.UnityCDB
{
    // TODO: this could be templated so that the data type is generic
    public class Octree
    {
        // E/W => +x/-x, N/S => +z/-z, UPPER/LOWER => +y/-y
        public enum NodePosition
        {
            UPPER_NW = 0,
            UPPER_NE = 1,
            UPPER_SW = 2,
            UPPER_SE = 3,
#if !USE_AS_QUADTREE
            LOWER_NW = 4,
            LOWER_NE = 5,
            LOWER_SW = 6,
            LOWER_SE = 7,
#endif
        }

        Bounds boundary;
        const int defaultCapacity = 10;
        int capacity = defaultCapacity; // model count of a node before it's divided
        List<Model> models = new List<Model>(); // list of our specific data type (no templating at this time)
        Octree[] children = null; // if this is non-null, the node is divided and has 8 (non-null) children
        static Color normalDrawColor = new Color(134f/255f, 95f/255f, 197f/255f, 1f);
        static Color selectionDrawColor = Color.yellow;
        static int debugCountInserted = 0;
        static int debugCountInsertedOverCapacity = 0;

        public Octree(Bounds boundary, int capacity = defaultCapacity)
        {
            this.boundary = boundary;
            this.capacity = capacity;
        }

        public void Draw()
        {
            boundary.Draw(normalDrawColor, 1);
#if UNITY_EDITOR
            foreach (var model in models)
            {
                if (model.gameObject == Selection.activeGameObject)
                {
                    boundary.Draw(selectionDrawColor, 0, -0.1f);
                    model.GetComponent<MeshRenderer>().bounds.Draw(Color.white);
                    break;
                }
            }
#endif

            if (children != null)
            {
                for (int i = 0; i < children.Length; ++i)
                    children[i].Draw();
            }
        }

        public bool Add(Model model)
        {
            var meshRenderer = model.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                return false;

#if !USE_AS_QUADTREE
            bool contains = boundary.Contains(model.GetComponent<MeshRenderer>().bounds);
#else
            bool contains = boundary.Contains(model.GetComponent<MeshRenderer>().bounds, true);
#endif
            if (contains && models.Count < capacity)
            {
                models.Add(model);
                ++debugCountInserted;
                return true;
            }
            else
            {
                // If the model is even partially outside of the range, don't add it to the octree
                if (!contains)
                    return false;

                if (children == null)
                    Divide();

                for (int i = 0; i < children.Length; ++i)
                {
                    if (children[i].Add(model))
                        return true;
                }

                // We failed to stash the model because the bounds straddled child nodes, so add it here despite exceeding desired capacity
                models.Add(model);
                ++debugCountInserted;
                ++debugCountInsertedOverCapacity;
                return true;
            }

            //return false;
        }

        public bool Remove(Model model)
        {
            // TODO: may want to perform a consolidate when relevant, but at this point we're only removing models when we tear down
            if (models.Remove(model))
            {
                return true;
            }
            else
            {
                if (children != null)
                {
                    for (int i = 0; i < children.Length; ++i)
                    {
                        if (children[i].Remove(model))
                            return true;
                    }
                }
            }

            return false;
        }

        // Build/rebuild from a list of models
        public void Build(List<Model> models)
        {
            Clear();
            if (models != null)
            {
                foreach (var model in models)
                    Add(model);
            }
        }

        protected void Clear()
        {
            models.Clear();
            if (children != null)
            {
                for (int i = 0; i < children.Length; ++i)
                {
                    children[i].Clear();
                    children[i] = null;
                }
                children = null;
            }
        }

        // If we reach capacity for any node, we subdivide it
        protected void Divide()
        {
            // Cache off some values for readability
            float x = boundary.center.x;
            float y = boundary.center.y;
            float z = boundary.center.z;
            float hw = boundary.extents.x;
            float hh = boundary.extents.y;
            float hl = boundary.extents.z;

            // Set the bounds on each child
#if !USE_AS_QUADTREE
            children = new Octree[8];
#else
            children = new Octree[4];
#endif
            Bounds bounds = new Bounds(Vector3.zero, new Vector3(hw, hh, hl));
            bounds.center = new Vector3(x - hw / 2f, y + hh / 2f, z + hl / 2f);
            children[(int)NodePosition.UPPER_NW] = new Octree(bounds, capacity);
            bounds.center = new Vector3(x + hw / 2f, y + hh / 2f, z + hl / 2f);
            children[(int)NodePosition.UPPER_NE] = new Octree(bounds, capacity);
            bounds.center = new Vector3(x - hw / 2f, y + hh / 2f, z - hl / 2f);
            children[(int)NodePosition.UPPER_SW] = new Octree(bounds, capacity);
            bounds.center = new Vector3(x + hw / 2f, y + hh / 2f, z - hl / 2f);
            children[(int)NodePosition.UPPER_SE] = new Octree(bounds, capacity);
#if !USE_AS_QUADTREE
            bounds.center = new Vector3(x - hw / 2f, y - hh / 2f, z + hl / 2f);
            children[(int)NodePosition.LOWER_NW] = new Octree(bounds, capacity);
            bounds.center = new Vector3(x + hw / 2f, y - hh / 2f, z + hl / 2f);
            children[(int)NodePosition.LOWER_NE] = new Octree(bounds, capacity);
            bounds.center = new Vector3(x - hw / 2f, y - hh / 2f, z - hl / 2f);
            children[(int)NodePosition.LOWER_SW] = new Octree(bounds, capacity);
            bounds.center = new Vector3(x + hw / 2f, y - hh / 2f, z - hl / 2f);
            children[(int)NodePosition.LOWER_SE] = new Octree(bounds, capacity);
#endif
        }

        protected void Consolidate()
        {
            // TODO
            throw new Exception("[UnityCDB.Octree] Consolidate method not yet implemented");
        }

        public bool Query(Bounds bounds, ref List<Model> result)
        {
            if (result == null)
                result = new List<Model>();

            if (!boundary.Intersects(bounds))
            {
                return false;
            }
            else
            {
                foreach (var model in models)
                {
                    if (!model.Loaded)
                        continue;

                    if (bounds.Contains(model.transform.position))
                        result.Add(model);
                }

                if (children != null)
                {
                    for (int i = 0; i < children.Length; ++i)
                        children[i].Query(bounds, ref result);
                }
            }

            return result.Count != 0;
        }

        public bool Query(Plane[] frustum, ref List<Model> result)
        {
            if (result == null)
                result = new List<Model>();

            if (!GeometryUtility.TestPlanesAABB(frustum, boundary))
            {
                return false;
            }
            else
            {
                foreach (var model in models)
                {
                    if (!model.Loaded)
                        continue;

                    var meshRenderer = model.gameObject.GetComponent<MeshRenderer>();
                    if (meshRenderer != null && GeometryUtility.TestPlanesAABB(frustum, meshRenderer.bounds))
                        result.Add(model);
                }

                if (children != null)
                {
                    for (int i = 0; i < children.Length; ++i)
                        children[i].Query(frustum, ref result);
                }
            }

            return result.Count != 0;
        }
    }
}
