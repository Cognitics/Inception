
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Cognitics.CoordinateSystems;

namespace Cognitics.UnityCDB
{
    public class QuadTree : MonoBehaviour
    {
        // switch delegate should call Divide() or Consolidate() based on LOD switching criteria
        [HideInInspector] public QuadTreeDelegate SwitchDelegate = null;
        [HideInInspector] public QuadTreeDelegate LoadDelegate = null;
        [HideInInspector] public QuadTreeDelegate LoadedDelegate = null;
        [HideInInspector] public QuadTreeDelegate UnloadDelegate = null;
        [HideInInspector] public QuadTreeDelegate UpdateDelegate = null;

        public List<QuadTreeNode> Children = new List<QuadTreeNode>();

        public Database Database = null;

        public GeographicBounds GeographicBounds = GeographicBounds.EmptyValue;

        public void Initialize(Database database, GeographicBounds bounds)
        {
            Database = database;
            GeographicBounds = bounds;
        }

        public QuadTreeNode AddChild(GeographicBounds bounds)
        {
            var node = new QuadTreeNode(this, 0, bounds);
            Children.Add(node);
            return node;
        }

        private void Update()
        {
            Children.ForEach(child => child.Update());
        }


    }


    public delegate void QuadTreeDelegate(QuadTreeNode node);

    public class QuadTreeNode
    {
        public QuadTree Root;
        public GeographicBounds GeographicBounds = GeographicBounds.EmptyValue;

        public int Depth = 0;

        public bool InRange = false;
        public bool IsActive = false;
        public bool IsLoading = false;
        public bool IsLoaded = false;

        public bool IsDistanceTesting = false;
        public bool IsDistanceTested = false;
        public float Distance = float.MaxValue;
        private DateTime lastDistanceTest = DateTime.MinValue;

        public List<QuadTreeNode> Children = new List<QuadTreeNode>();

        public QuadTreeNode(QuadTree root, int depth, GeographicBounds bounds)
        {
            Root = root;
            Depth = depth;
            GeographicBounds = bounds;
        }

        public void AddChild(GeographicBounds bounds) => Children.Add(new QuadTreeNode(Root, Depth + 1, bounds));

        public List<QuadTreeNode> ActiveTiles()
        {
            var result = new List<QuadTreeNode>();
            if (IsActive)
                result.Add(this);
            Children.ForEach(child => result.AddRange(child.ActiveTiles()));
            return result;
        }

        public void Divide()
        {
            if (!IsActive)
                return;
            if (HasChildren())
                return;
            AddChild(new GeographicBounds(GeographicBounds.MinimumCoordinates, GeographicBounds.Center));
            AddChild(new GeographicBounds(
                new GeographicCoordinates(GeographicBounds.MinimumCoordinates.Latitude, GeographicBounds.Center.Longitude),
                new GeographicCoordinates(GeographicBounds.Center.Latitude, GeographicBounds.MaximumCoordinates.Longitude)
                ));
            AddChild(new GeographicBounds(
                new GeographicCoordinates(GeographicBounds.Center.Latitude, GeographicBounds.MinimumCoordinates.Longitude),
                new GeographicCoordinates(GeographicBounds.MaximumCoordinates.Latitude, GeographicBounds.Center.Longitude)
                ));
            AddChild(new GeographicBounds(GeographicBounds.Center, GeographicBounds.MaximumCoordinates));
            Debug.LogFormat("[TREE] DIVIDE: DEPTH: {0} DISTANCE:{1}  {2}", Depth, Distance, GeographicBounds.String);
        }

        public void Consolidate()
        {
            if (HasGrandChildren())
                return;
            if (!HasChildren())
                return;
            Children.ForEach(child => Root.UnloadDelegate(child));
            Children.Clear();
            Debug.LogFormat("[TREE] CONSOLIDATE: DEPTH: {0} DISTANCE:{1}  {2}", Depth, Distance, GeographicBounds.String);
        }

        public bool HasChildren() => Children.Count > 0;

        public bool HasGrandChildren()
        {
            foreach (var child in Children)
            {
                if (child.HasChildren())
                    return true;
            }
            return false;
        }

        public bool ChildrenActive()
        {
            foreach (var child in Children)
            {
                if (!child.IsActive)
                    return false;
            }
            return HasChildren();
        }

        public bool ChildrenLoaded()
        {
            foreach (var child in Children)
            {
                if (!child.IsLoaded)
                    return false;
            }
            return HasChildren();
        }

        public bool Update()
        {
            Root.UpdateDelegate(this);

            // bottom-up traversal avoids cascading HasGrandChildren() tests
            bool hasGrandChildren = false;
            foreach (var child in Children)
            {
                if (child.Update())
                    hasGrandChildren = true;
            }

            // nothing to do if grandchildren exist
            if (hasGrandChildren)
                return true;

            // nothing to do if loading task is active
            if (IsLoading)
                return HasChildren();

            // process distances
            if (!hasGrandChildren && IsDistanceTested)
                Root.SwitchDelegate(this);

            if (InRange)
            {
                // start load if not loaded and children state is unknown
                if (!IsLoaded && !ChildrenLoaded())
                {
                    IsLoading = true;
                    Root.LoadDelegate(this);
                    return HasChildren();
                }

                // activate if not active and children are not active
                if (!IsActive && !ChildrenActive())
                {
                    Root.LoadedDelegate(this);
                    IsActive = true;
                    return HasChildren();
                }

                // if active and children are ready, deactivate and activate the children
                if (IsActive && ChildrenLoaded())
                {
                    Children.ForEach(child => child.IsActive = true);
                    IsActive = false;
                    Root.UnloadDelegate(this);
                    return HasChildren();
                }
            }
            else
            {
                Root.UnloadDelegate(this);
            }

            // distance testing
            if (IsDistanceTesting)
                return HasChildren();

            if ((DateTime.Now - lastDistanceTest).TotalSeconds < 4)
                return HasChildren();
            lastDistanceTest = DateTime.Now;
            IsDistanceTesting = true;
            IsDistanceTested = false;
            Task.Run(() => TaskDistanceTest());

            return HasChildren();
        }

        private void TaskDistanceTest()
        {
            try
            {
                Distance = Root.Database.DistanceForBounds(GeographicBounds);
                //Console.WriteLine(string.Format("DISTANCE {0} = {1}", GeographicBounds.String, Distance));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            IsDistanceTested = true;
            IsDistanceTesting = false;
        }


    }

}

