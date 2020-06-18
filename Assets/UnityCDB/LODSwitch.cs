
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cognitics.UnityCDB
{
    public class LODSwitch
    {
        public float MaxDistance = 1000.0f;
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
            {
                if (Database.SystemMemoryLimitExceeded)
                    return;
                node.Divide();
            }
            if (distance > ExitDistanceForLOD(node.Depth + 1))
                node.Consolidate();
        }

        public float EntryDistanceForLOD(int lod) => EntryDistanceByLOD.ContainsKey(lod) ? EntryDistanceByLOD[lod] : float.MinValue;
        public float ExitDistanceForLOD(int lod) => ExitDistanceByLOD.ContainsKey(lod) ? ExitDistanceByLOD[lod] : float.MinValue;

        public void InitializeDefault(float scale, int maxLOD)
        {
            const float meters_per_geocell = 111120.0f;
            float geocell_size = meters_per_geocell * scale;
            float dist_in = geocell_size * 0.5f;
            float dist_out = geocell_size * 1.0f;
            for (int lod = 0; lod >= -10; --lod)
            {
                EntryDistanceByLOD[lod] = dist_in;
                ExitDistanceByLOD[lod] = dist_out;
                dist_in *= 2;
                dist_out *= 2;
            }
            dist_in = geocell_size * 0.5f;
            dist_out = geocell_size * 2.0f;
            for (int lod = 0; lod < maxLOD; ++lod)
            {
                EntryDistanceByLOD[lod] = dist_in;
                ExitDistanceByLOD[lod] = dist_out;
                dist_in *= 0.5f;
                dist_out *= 0.5f;
            }
        }

        private Database Database = null;


    }

}
