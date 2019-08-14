using System;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

namespace Cognitics.Unity
{
    public class AsyncOpenFlightReader : AsyncFileReader
    {
        public int Width = -1;
        public int Height = -1;
        public int Depth = -1;
        public TextureFormat TextureFormat;
        public NativeArray<byte> Data;

        public AsyncOpenFlightReader(string filename) : base(filename) { }

        public override bool Run()
        {
            if (Ready)
                return true;
            if (!base.Run())
                return false;
            if (!Data.IsCreated)
                ParseJobHandle = StartParseJob();
            if (!ParseJobHandle.IsCompleted)
                return false;
            ParseJobHandle.Complete();
            base.Dispose();
            Ready = true;
            return true;
        }

        public override void Dispose() => Data.Dispose();

        private bool Ready = false;
        private ParseJob_ ParseJob;
        private JobHandle ParseJobHandle;

        private JobHandle StartParseJob()
        {
            // TODO

            //Data = new NativeArray<byte>(Width * Height * Depth, Allocator.Persistent);
            ParseJob = new ParseJob_ { BytesNA = Bytes };
            return ParseJob.Schedule();
        }

        private struct ParseJob_ : IJob
        {
            [ReadOnly] public NativeArray<byte> BytesNA;
            //public NativeArray<byte> DataNA;
            public void Execute()
            {
                try
                {
                    // TODO 
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }


        // TODO: parse -> construct
        // the parse step will collect reference information
        // the construct step will use a job for each submesh to construct the data


    }

}

