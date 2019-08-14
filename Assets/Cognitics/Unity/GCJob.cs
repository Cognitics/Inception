using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Jobs;

namespace Cognitics.Unity
{
    public abstract class GCJob
    {
        public JobHandle JobHandle;
        public GCJob() => UnityJob = new UnityJob_() { GCHandle = GCHandle.Alloc(this) };
        ~GCJob() => UnityJob.GCHandle.Free();
        public void Schedule() => JobHandle = UnityJob.Schedule();
        public bool IsCompleted => JobHandle.IsCompleted;
        public void Complete() => JobHandle.Complete();
        public abstract void Execute();

        private UnityJob_ UnityJob;
        private struct UnityJob_ : IJob
        {
            public GCHandle GCHandle;
            public void Execute()
            {
                try
                {
                    ((GCJob)GCHandle.Target).Execute();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

    }

}
