using System;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

namespace Cognitics.Unity
{
    public class AsyncJP2Reader : AsyncFileReader
    {
        public int Width = -1;
        public int Height = -1;
        public int Depth = -1;
        public TextureFormat TextureFormat;
        public NativeArray<byte> Data;

        public AsyncJP2Reader(string filename) : base(filename) { }

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
            // TODO: how do we get the size?

            /*
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
                */

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
                    // TODO: how do we get the size?

                    CSJ2K.Util.PortableImage img = CSJ2K.J2kImage.FromBytes(BytesNA.ToArray());

                    int[] ib = img.GetComponent(0);
                    int[] ig = img.GetComponent(1);
                    int[] ir = img.GetComponent(2);

                    int dim = (int)Math.Sqrt(ib.Length);

                    var result = new byte[dim * dim * 3];

                    for (int y = 0; y < dim; ++y)
                    {
                        for (int x = 0; x < dim; ++x)
                        {
                            int i = (y * dim) + x;
                            result[(i * 3) + 0] = (byte)ir[i];
                            result[(i * 3) + 1] = (byte)ig[i];
                            result[(i * 3) + 2] = (byte)ib[i];
                        }
                    }




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

