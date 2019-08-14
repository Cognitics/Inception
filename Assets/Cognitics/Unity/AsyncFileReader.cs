using System.Text;
using Unity.Jobs;
using Unity.Collections;

namespace Cognitics.Unity
{
    public class AsyncFileReader
    {
        public string Filename;
        public NativeArray<byte> Bytes;

        public AsyncFileReader(string filename) => Filename = filename;

        public virtual bool Run()
        {
            if (Loaded)
                return true;
            if (!FilenameNA.IsCreated)
                LengthJobHandle = StartLengthJob();
            if (!LengthJobHandle.IsCompleted)
                return false;
            if (!Bytes.IsCreated)
            {
                LengthJobHandle.Complete();
                ReadJobHandle = StartReadJob();
            }
            if (!ReadJobHandle.IsCompleted)
                return false;
            ReadJobHandle.Complete();
            FilenameNA.Dispose();
            LengthNA.Dispose();
            Loaded = true;
            return true;
        }

        public virtual void Dispose() => Bytes.Dispose();

        private bool Loaded = false;
        private NativeArray<byte> FilenameNA;
        private NativeArray<long> LengthNA;
        private LengthJob_ LengthJob;
        private JobHandle LengthJobHandle;
        private ReadJob_ ReadJob;
        private JobHandle ReadJobHandle;

        private JobHandle StartLengthJob()
        {
            FilenameNA = new NativeArray<byte>(Filename.Length, Allocator.Persistent);
            FilenameNA.CopyFrom(Encoding.ASCII.GetBytes(Filename));
            LengthNA = new NativeArray<long>(1, Allocator.Persistent);
            LengthJob = new LengthJob_ { FilenameNA = FilenameNA, LengthNA = LengthNA };
            return LengthJob.Schedule();
        }

        private JobHandle StartReadJob()
        {
            long length = LengthNA[0];
            length = (length < 0) ? 0 : length;
            Bytes = new NativeArray<byte>((int)length, Allocator.Persistent);
            ReadJob = new ReadJob_ { FilenameNA = FilenameNA, BytesNA = Bytes };
            return ReadJob.Schedule();
        }

        private struct LengthJob_ : IJob
        {
            [ReadOnly] public NativeArray<byte> FilenameNA;
            public NativeArray<long> LengthNA;
            public void Execute()
            {
                try
                {
                    var info = new System.IO.FileInfo(Encoding.ASCII.GetString(FilenameNA.ToArray()));
                    LengthNA[0] = info.Length;  // throws on failure
                }
                catch
                {
                    LengthNA[0] = -1;
                }
            }
        }

        private struct ReadJob_ : IJob
        {
            [ReadOnly] public NativeArray<byte> FilenameNA;
            public NativeArray<byte> BytesNA;
            public void Execute()
            {
                if (BytesNA.Length > 0)
                    BytesNA.CopyFrom(System.IO.File.ReadAllBytes(Encoding.ASCII.GetString(FilenameNA.ToArray())));
            }
        }

    }

}