
using System.Runtime.InteropServices;

namespace Cognitics
{
    public class BinaryParser
    {
        public byte[] Bytes = null;
        public int Position = 0;

        public BinaryParser(byte[] data)
        {
            Bytes = data;
            impl = System.BitConverter.IsLittleEndian ? (Impl) new LittleEndianImpl() : new BigEndianImpl();
        }

        public byte Byte() => Advance(1, Bytes[Position]);

        public char Char() => Advance(1, (char)Bytes[Position]);

        public short Int16() => impl.Int16(this);
        public short Int16BE() => Advance(2, (short)((Bytes[Position + 0] << 8) + Bytes[Position + 1]));
        public short Int16LE() => Advance(2, (short)((Bytes[Position + 1] << 8) + Bytes[Position + 0]));

        public ushort UInt16() => impl.UInt16(this);
        public ushort UInt16BE() => (ushort)Int16BE();
        public ushort UInt16LE() => (ushort)Int16LE();

        public int Int32() => impl.Int32(this);
        public int Int32BE() => Advance(4, 
            (Bytes[Position + 0] << 24) +
            (Bytes[Position + 1] << 16) +
            (Bytes[Position + 2] << 8) +
            Bytes[Position + 3]);
        public int Int32LE() => Advance(4, 
            (Bytes[Position + 3] << 24) +
            (Bytes[Position + 2] << 16) +
            (Bytes[Position + 1] << 8) +
            Bytes[Position + 0]);

        public uint UInt32() => impl.UInt32(this);
        public uint UInt32BE() => (uint)Int32BE();
        public uint UInt32LE() => (uint)Int32LE();

        public long Int64() => impl.Int64(this);
        public long Int64BE() => Advance(8, 
            ((long)Bytes[Position + 0] << 56) +
            ((long)Bytes[Position + 1] << 48) +
            ((long)Bytes[Position + 2] << 40) +
            ((long)Bytes[Position + 3] << 32) +
            ((long)Bytes[Position + 4] << 24) +
            ((long)Bytes[Position + 5] << 16) + 
            ((long)Bytes[Position + 6] << 8) +
            (long)Bytes[Position + 7]);
        public long Int64LE() => Advance(8, 
            ((long)Bytes[Position + 7] << 56) +
            ((long)Bytes[Position + 6] << 48) +
            ((long)Bytes[Position + 5] << 40) +
            ((long)Bytes[Position + 4] << 32) +
            ((long)Bytes[Position + 3] << 24) +
            ((long)Bytes[Position + 2] << 16) + 
            ((long)Bytes[Position + 1] << 8) +
            (long)Bytes[Position + 0]);

        public ulong UInt64() => impl.UInt64(this);
        public ulong UInt64BE() => (ulong)Int64BE();
        public ulong UInt64LE() => (ulong)Int64LE();

        public float Single() => impl.Single(this);
        public float SingleBE() => Float(UInt32BE());
        public float SingleLE() => Float(UInt32LE());

        public double Double() => impl.Double(this);
        public double DoubleBE() => Double(UInt64BE());
        public double DoubleLE() => Double(UInt64LE());

        public bool Boolean() => Byte() != 0;

        public string String(int length)
        {
            int len = 0;
            for (len = 0; (len < length) && (Bytes[Position + len] != 0); ++len) ;
            return Advance(length, System.Text.Encoding.UTF8.GetString(Bytes, Position, len));
        }

        private T Advance<T>(int num, T result) { Position += num; return result; }

        #region Float

        [StructLayout(LayoutKind.Explicit)]
        private struct FloatUInt32
        {
            [FieldOffset(0)] public float Float;
            [FieldOffset(0)] public uint UInt32;
        }

        private float Float(uint value)
        {
            var tmp = default(FloatUInt32);
            tmp.UInt32 = value;
            return tmp.Float;
        }

        #endregion
        #region Double

        [StructLayout(LayoutKind.Explicit)]
        private struct DoubleUInt64
        {
            [FieldOffset(0)] public double Double;
            [FieldOffset(0)] public ulong UInt64;
        }

        private double Double(ulong value)
        {
            var tmp = default(DoubleUInt64);
            tmp.UInt64 = value;
            return tmp.Double;
        }

        #endregion
        #region Impl

        private Impl impl;

        private abstract class Impl
        {
            internal abstract ushort UInt16(BinaryParser bp);
            internal abstract short Int16(BinaryParser bp);
            internal abstract uint UInt32(BinaryParser bp);
            internal abstract int Int32(BinaryParser bp);
            internal abstract ulong UInt64(BinaryParser bp);
            internal abstract long Int64(BinaryParser bp);
            internal abstract float Single(BinaryParser bp);
            internal abstract double Double(BinaryParser bp);
        }

        private class BigEndianImpl : Impl
        {
            internal override ushort UInt16(BinaryParser bp) => bp.UInt16BE();
            internal override short Int16(BinaryParser bp) => bp.Int16BE();
            internal override uint UInt32(BinaryParser bp) => bp.UInt32BE();
            internal override int Int32(BinaryParser bp) => bp.Int32BE();
            internal override ulong UInt64(BinaryParser bp) => bp.UInt64BE();
            internal override long Int64(BinaryParser bp) => bp.Int64BE();
            internal override float Single(BinaryParser bp) => bp.SingleBE();
            internal override double Double(BinaryParser bp) => bp.DoubleBE();
        }

        private class LittleEndianImpl : Impl
        {
            internal override ushort UInt16(BinaryParser bp) => bp.UInt16LE();
            internal override short Int16(BinaryParser bp) => bp.Int16LE();
            internal override uint UInt32(BinaryParser bp) => bp.UInt32LE();
            internal override int Int32(BinaryParser bp) => bp.Int32LE();
            internal override ulong UInt64(BinaryParser bp) => bp.UInt64LE();
            internal override long Int64(BinaryParser bp) => bp.Int64LE();
            internal override float Single(BinaryParser bp) => bp.SingleLE();
            internal override double Double(BinaryParser bp) => bp.DoubleLE();
        }

        #endregion

    }

}
