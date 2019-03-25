using System;
using System.IO;
using System.Text;

namespace Cognitics
{
    public class EndianBinaryReader : BinaryReader
    {
        public EndianBinaryReader(Stream input) : base(input) { }
        public EndianBinaryReader(Stream input, Encoding encoding) : base(input, encoding) { }
        public EndianBinaryReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen) { }


        public ushort ReadUInt16Endian(bool littleEndian) => littleEndian ? ReadUInt16Little() : ReadUInt16Big();
        public short ReadInt16Endian(bool littleEndian) => littleEndian ? ReadInt16Little() : ReadInt16Big();
        public uint ReadUInt32Endian(bool littleEndian) => littleEndian ? ReadUInt32Little() : ReadUInt32Big();
        public int ReadInt32Endian(bool littleEndian) => littleEndian ? ReadInt32Little() : ReadInt32Big();
        public ulong ReadUInt64Endian(bool littleEndian) => littleEndian ? ReadUInt64Little() : ReadUInt64Big();
        public long ReadInt64Endian(bool littleEndian) => littleEndian ? ReadInt64Little() : ReadInt64Big();
        public float ReadSingleEndian(bool littleEndian) => littleEndian ? ReadSingleLittle() : ReadSingleBig();
        public double ReadDoubleEndian(bool littleEndian) => littleEndian ? ReadDoubleLittle() : ReadDoubleBig();

        public ushort ReadUInt16Big() => BitConverter.IsLittleEndian ? ReadUInt16Swapped() : ReadUInt16();
        public ushort ReadUInt16Little() => BitConverter.IsLittleEndian ? ReadUInt16() : ReadUInt16Swapped();
        public short ReadInt16Big() => BitConverter.IsLittleEndian ? ReadInt16Swapped() : ReadInt16();
        public short ReadInt16Little() => BitConverter.IsLittleEndian ? ReadInt16() : ReadInt16Swapped();
        public uint ReadUInt32Big() => BitConverter.IsLittleEndian ? ReadUInt32Swapped() : ReadUInt32();
        public uint ReadUInt32Little() => BitConverter.IsLittleEndian ? ReadUInt32() : ReadUInt32Swapped();
        public int ReadInt32Big() => BitConverter.IsLittleEndian ? ReadInt32Swapped() : ReadInt32();
        public int ReadInt32Little() => BitConverter.IsLittleEndian ? ReadInt32() : ReadInt32Swapped();
        public ulong ReadUInt64Big() => BitConverter.IsLittleEndian ? ReadUInt64Swapped() : ReadUInt64();
        public ulong ReadUInt64Little() => BitConverter.IsLittleEndian ? ReadUInt64() : ReadUInt64Swapped();
        public long ReadInt64Big() => BitConverter.IsLittleEndian ? ReadInt64Swapped() : ReadInt64();
        public long ReadInt64Little() => BitConverter.IsLittleEndian ? ReadInt64() : ReadInt64Swapped();
        public float ReadSingleBig() => BitConverter.IsLittleEndian ? ReadSingleSwapped() : ReadSingle();
        public float ReadSingleLittle() => BitConverter.IsLittleEndian ? ReadSingle() : ReadSingleSwapped();
        public double ReadDoubleBig() => BitConverter.IsLittleEndian ? ReadDoubleSwapped() : ReadDouble();
        public double ReadDoubleLittle() => BitConverter.IsLittleEndian ? ReadDouble() : ReadDoubleSwapped();

        private ushort ReadUInt16Swapped()
        {
            var data = ReadBytes(2);
            Array.Reverse(data);
            return BitConverter.ToUInt16(data, 0);
        }

        private short ReadInt16Swapped()
        {
            var data = ReadBytes(2);
            Array.Reverse(data);
            return BitConverter.ToInt16(data, 0);
        }

        private uint ReadUInt32Swapped()
        {
            var data = ReadBytes(4);
            Array.Reverse(data);
            return BitConverter.ToUInt32(data, 0);
        }

        private int ReadInt32Swapped()
        {
            var data = ReadBytes(4);
            Array.Reverse(data);
            return BitConverter.ToInt32(data, 0);
        }

        private ulong ReadUInt64Swapped()
        {
            var data = ReadBytes(8);
            Array.Reverse(data);
            return BitConverter.ToUInt64(data, 0);
        }

        private long ReadInt64Swapped()
        {
            var data = ReadBytes(8);
            Array.Reverse(data);
            return BitConverter.ToInt64(data, 0);
        }

        private float ReadSingleSwapped()
        {
            var data = ReadBytes(4);
            Array.Reverse(data);
            return BitConverter.ToSingle(data, 0);
        }

        private double ReadDoubleSwapped()
        {
            var data = ReadBytes(8);
            Array.Reverse(data);
            return BitConverter.ToDouble(data, 0);
        }


    }
}
