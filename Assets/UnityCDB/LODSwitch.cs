
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitics.UnityCDB
{
    public class LODSwitch
    {
        public Dictionary<int, float> EntryDistanceByLOD = new Dictionary<int, float>();
        public Dictionary<int, float> ExitDistanceByLOD = new Dictionary<int, float>();

        public LODSwitch(Database database)
        {
            Database = database;
        }

        public void QuadTreeSwitchUpdate(QuadTreeNode node)       // QuadTreeDelegate
        {
            float distance = node.Distance;
            node.InRange = distance < EntryDistanceForLOD(node.Depth);
            if (distance < EntryDistanceForLOD(node.Depth + 1))
                node.Divide();
            if (distance > ExitDistanceForLOD(node.Depth + 1))
                node.Consolidate();
        }

        public float EntryDistanceForLOD(int lod) => EntryDistanceByLOD.ContainsKey(lod) ? EntryDistanceByLOD[lod] : float.MinValue;
        public float ExitDistanceForLOD(int lod) => ExitDistanceByLOD.ContainsKey(lod) ? ExitDistanceByLOD[lod] : float.MaxValue;

        public void InitializeDefault(float scale, int maxLOD)
        {
            const float factor = 10.0f;
            const float metersPerGeocell = 111120.0f;
            float distance = metersPerGeocell * factor * scale;
            for (int lod = 0; lod < maxLOD; ++lod)
            {
                EntryDistanceByLOD[lod] = distance;
                ExitDistanceByLOD[lod] = distance * 2;
                distance /= 2;
            }

        }


        private Database Database = null;


    }

}
