using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Jobs;

namespace Cognitics.Unity
{
    // GCJob acts as a delegate for a Unity job, bypassing the requirement that a job contains only blittable data.
    // Create a derived class and override Execute().
    // Call Schedule() to schedule the job.
    // Call IsCompleted() to check if complete.
    // Call Complete() to finalize the job.
    public abstract class GCJob
    {
        public GCJob() => UnityJob = new UnityJob_() { GCHandle = GCHandle.Alloc(this) };
        public void Schedule() => JobHandle = UnityJob.Schedule();
        public bool IsCompleted => JobHandle.IsCompleted;
        public void Complete() => JobHandle.Complete();

        public abstract void Execute();

        public JobHandle JobHandle { get; private set; }

        ~GCJob() => UnityJob.GCHandle.Free();

        private UnityJob_ UnityJob;
        private struct UnityJob_ : IJob
        {
            public GCHandle GCHandle;   // blittable
            public void Execute()
            {
                try
                {
                    // delegate to the wrapper
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
