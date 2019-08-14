using System;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using BitMiracle.LibTiff.Classic;

namespace Cognitics.Unity
{
    public class AsyncTiffReader : AsyncFileReader
    {
        public int Width = -1;
        public int Height = -1;
        public int Depth = -1;
        public TextureFormat TextureFormat;
        public NativeArray<byte> Data;

        public AsyncTiffReader(string filename) : base(filename) { }

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
            // TODO: this should be another job
            try
            {
                var tiff = Tiff.ClientOpen("AsyncTiffReader", "r", new System.IO.MemoryStream(Bytes.ToArray()), new TiffStream());
                FieldValue[] value = tiff.GetField(TiffTag.IMAGEWIDTH);
                Width = value[0].ToInt();
                value = tiff.GetField(TiffTag.IMAGELENGTH);
                Height = value[0].ToInt();
                TextureFormat = TextureFormat.RFloat;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            Data = new NativeArray<byte>(Width * Height * sizeof(float), Allocator.Persistent);
            ParseJob = new ParseJob_ { BytesNA = Bytes, DataNA = Data };
            return ParseJob.Schedule();
        }

        private struct ParseJob_ : IJob
        {
            [ReadOnly] public NativeArray<byte> BytesNA;
            public NativeArray<byte> DataNA;
            public void Execute()
            {
                // TODO: investigate IJobParallelFor with this
                try
                {
                    var tiff = Tiff.ClientOpen("AsyncTiffReader", "r", new System.IO.MemoryStream(BytesNA.ToArray()), new TiffStream());

                    FieldValue[] value = tiff.GetField(TiffTag.IMAGEWIDTH);
                    int width = value[0].ToInt();
                    value = tiff.GetField(TiffTag.IMAGELENGTH);
                    int height = value[0].ToInt();
                    value = tiff.GetField(TiffTag.BITSPERSAMPLE);
                    int depth = value[0].ToInt() / 8;
                    value = tiff.GetField(TiffTag.SAMPLEFORMAT);
                    var sampleFormat = (SampleFormat)value[0].ToInt();
                    int stride = tiff.ScanlineSize();

                    // TODO: continue using scanlines, but use BinaryParser (add native functions) rather than bitconverter
                    var data = new float[width * height];
                    byte[] buffer = new byte[stride];

                    if ((depth == 4) && (sampleFormat == SampleFormat.IEEEFP))
                    {
                        for (int row = 0; row < height; ++row)
                        {
                            if (!tiff.ReadScanline(buffer, row))
                                break;
                            // TODO: BlockCopy isn't working correctly?
                            //Buffer.BlockCopy(buffer, 0, data, row * width, buffer.Length);
                            for (int col = 0; col < width; ++col)
                                data[(row * width) + col] = BitConverter.ToSingle(buffer, col * 4);
                        }
                    }

                    if ((depth == 4) && (sampleFormat == SampleFormat.INT))
                    {
                        for (int row = 0; row < height; ++row)
                        {
                            if (!tiff.ReadScanline(buffer, row))
                                break;
                            for (int col = 0; col < width; ++col)
                                data[(row * width) + col] = BitConverter.ToInt32(buffer, col * 4);
                        }
                    }

                    if ((depth == 2) && (sampleFormat == SampleFormat.INT))
                    {
                        for (int row = 0; row < height; ++row)
                        {
                            if (!tiff.ReadScanline(buffer, row))
                                break;
                            for (int col = 0; col < width; ++col)
                                data[(row * width) + col] = BitConverter.ToInt16(buffer, col * 2);
                        }
                    }

                    if (depth == 1)
                    {
                        for (int row = 0; row < height; ++row)
                        {
                            if (tiff.ReadScanline(buffer, row))
                                break;
                            buffer.CopyTo(data, row * width);
                        }
                    }

                    var tmp = new byte[data.Length * sizeof(float)];
                    Buffer.BlockCopy(data, 0, tmp, 0, data.Length);
                    DataNA.CopyFrom(tmp);

                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }
    }

}

