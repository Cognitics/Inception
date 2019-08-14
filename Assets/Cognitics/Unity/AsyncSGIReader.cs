using System;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

namespace Cognitics.Unity
{
    public class AsyncSGIReader : AsyncFileReader
    {
        public int Width = -1;
        public int Height = -1;
        public int Depth = -1;
        public TextureFormat TextureFormat;
        public NativeArray<byte> Data;

        public AsyncSGIReader(string filename) : base(filename) { }

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
            // TODO: this should be another job (maybe)
            var parser = new BinaryParser(Bytes.ToArray());
            parser.Position = 4;
            Depth = parser.UInt16BE();
            Width = parser.UInt16BE();
            Height = parser.UInt16BE();

            if (Depth < 3)
                Depth = 1;
            if (Depth >= 3)
                Depth = parser.UInt16BE();

            TextureFormat = TextureFormat.RGBA32;
            if (Depth == 3)
                TextureFormat = TextureFormat.RGB24;
            if (Depth == 1)
                TextureFormat = TextureFormat.Alpha8;

            Data = new NativeArray<byte>(Width * Height * Depth, Allocator.Persistent);
            ParseJob = new ParseJob_ { BytesNA = Bytes, DataNA = Data };
            return ParseJob.Schedule();
        }

        private struct ParseJob_ : IJob
        {
            [ReadOnly] public NativeArray<byte> BytesNA;
            public NativeArray<byte> DataNA;
            public void Execute()
            {
                try
                {
                    var sgi = new CDB.SiliconGraphicsImage();
                    var data = sgi.ReadRGB8("", BytesNA.ToArray(), out int w, out int h, out int c);
                    DataNA.CopyFrom(data);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }

}

