using System.IO;

namespace Cognitics.GeoPackage
{
    public class BinaryGeometry
    {
        public byte Version;
        public int SpatialReferenceSystemID;
        public Point[] Envelope = null;

        static public Geometry Read(Stream stream)
        {
            var binaryGeometry = new BinaryGeometry();
            var reader = new EndianBinaryReader(stream);
            if (!binaryGeometry.ReadHeader(reader))
                return null;
            return binaryGeometry.ReadGeometry(reader);
        }


        #region implementation

        private bool ReadHeader(EndianBinaryReader reader)
        {
            var gp = reader.ReadBytes(2);
            if (gp[0] != 0x47)  // G
                return false;
            if (gp[1] != 0x50)  // P
                return false;
            Version = reader.ReadByte();
            var flags = reader.ReadByte();
            int flags_binaryType = (flags & (1 << 5)) >> 5;
            int flags_emptyGeometry = (flags & (1 << 4)) >> 4;
            int flags_envelopeContents = (flags & 14) >> 1;
            int flags_byteOrder = flags & 1;
            bool isLittleEndian = flags_byteOrder != 0;
            SpatialReferenceSystemID = reader.ReadInt32Endian(isLittleEndian);
            if (flags_envelopeContents > 0)
            {
                Envelope = new Point[2] { new Point(), new Point() } ;
                Envelope[0].X = reader.ReadDoubleEndian(isLittleEndian);
                Envelope[1].X = reader.ReadDoubleEndian(isLittleEndian);
                Envelope[0].Y = reader.ReadDoubleEndian(isLittleEndian);
                Envelope[1].Y = reader.ReadDoubleEndian(isLittleEndian);
            }
            if (flags_envelopeContents == 2)
            {
                Envelope[0].Z = reader.ReadDoubleEndian(isLittleEndian);
                Envelope[1].Z = reader.ReadDoubleEndian(isLittleEndian);
            }
            if (flags_envelopeContents > 2)
            {
                Envelope[0].M = reader.ReadDoubleEndian(isLittleEndian);
                Envelope[1].M = reader.ReadDoubleEndian(isLittleEndian);
            }
            return true;
        }

        private Point ReadPoint(EndianBinaryReader reader, bool isLittleEndian, bool hasZ, bool hasM)
        {
            var geometry = new Point();
            geometry.X = reader.ReadDoubleEndian(isLittleEndian);
            geometry.Y = reader.ReadDoubleEndian(isLittleEndian);
            if(hasZ)
                geometry.Z = reader.ReadDoubleEndian(isLittleEndian);
            if(hasM)
                geometry.M = reader.ReadDoubleEndian(isLittleEndian);
            return geometry;
        }

        private LineString ReadLineString(EndianBinaryReader reader, bool isLittleEndian, bool hasZ, bool hasM)
        {
            uint count = reader.ReadUInt32Endian(isLittleEndian);
            var geometry = new LineString();
            for (int i = 0; i < count; ++i)
                geometry.Points.Add(ReadPoint(reader, isLittleEndian, hasZ, hasM));
            return geometry;
        }

        private Polygon ReadPolygon(EndianBinaryReader reader, bool isLittleEndian, bool hasZ, bool hasM)
        {
            uint count = reader.ReadUInt32Endian(isLittleEndian);
            var geometry = new Polygon();
            for (int i = 0; i < count; ++i)
                geometry.Rings.Add(ReadLineString(reader, isLittleEndian, hasZ, hasM));
            return geometry;
        }

        private MultiPoint ReadMultiPoint(EndianBinaryReader reader, bool isLittleEndian, bool hasZ, bool hasM)
        {
            uint count = reader.ReadUInt32Endian(isLittleEndian);
            var geometry = new MultiPoint();
            for (int i = 0; i < count; ++i)
            {
                isLittleEndian = reader.ReadByte() == 1;
                reader.ReadUInt32Endian(isLittleEndian);
                geometry.Geometries.Add(ReadPoint(reader, isLittleEndian, hasZ, hasM));
            }
            return geometry;
        }

        private MultiLineString ReadMultiLineString(EndianBinaryReader reader, bool isLittleEndian, bool hasZ, bool hasM)
        {
            uint count = reader.ReadUInt32Endian(isLittleEndian);
            var geometry = new MultiLineString();
            for (int i = 0; i < count; ++i)
            {
                isLittleEndian = reader.ReadByte() == 1;
                reader.ReadUInt32Endian(isLittleEndian);
                geometry.Geometries.Add(ReadLineString(reader, isLittleEndian, hasZ, hasM));
            }
            return geometry;
        }

        private MultiPolygon ReadMultiPolygon(EndianBinaryReader reader, bool isLittleEndian, bool hasZ, bool hasM)
        {
            uint count = reader.ReadUInt32Endian(isLittleEndian);
            var geometry = new MultiPolygon();
            for (int i = 0; i < count; ++i)
            {
                isLittleEndian = reader.ReadByte() == 1;
                reader.ReadUInt32Endian(isLittleEndian);
                geometry.Geometries.Add(ReadPolygon(reader, isLittleEndian, hasZ, hasM));
            }
            return geometry;
        }

        private GeometryCollection ReadGeometryCollection(EndianBinaryReader reader, bool isLittleEndian, bool hasZ, bool hasM)
        {
            uint count = reader.ReadUInt32Endian(isLittleEndian);
            var geometry = new GeometryCollection();
            for (int i = 0; i < count; ++i)
                geometry.Geometries.Add(ReadGeometry(reader));
            return geometry;
        }

        private Geometry ReadGeometry(EndianBinaryReader reader)
        {
            bool isLittleEndian = reader.ReadByte() == 1;
            var wkbType = (WKBGeometryType)reader.ReadUInt32Endian(isLittleEndian);
            switch (wkbType)
            {
                case WKBGeometryType.wkbPoint:
                    return ReadPoint(reader, isLittleEndian, false, false);
                case WKBGeometryType.wkbPointZ:
                    return ReadPoint(reader, isLittleEndian, true, false);
                case WKBGeometryType.wkbPointM:
                    return ReadPoint(reader, isLittleEndian, false, true);
                case WKBGeometryType.wkbPointZM:
                    return ReadPoint(reader, isLittleEndian, true, true);
                case WKBGeometryType.wkbLineString:
                    return ReadLineString(reader, isLittleEndian, false, false);
                case WKBGeometryType.wkbLineStringZ:
                    return ReadLineString(reader, isLittleEndian, true, false);
                case WKBGeometryType.wkbLineStringM:
                    return ReadLineString(reader, isLittleEndian, false, true);
                case WKBGeometryType.wkbLineStringZM:
                    return ReadLineString(reader, isLittleEndian, true, true);
                case WKBGeometryType.wkbPolygon:
                    return ReadPolygon(reader, isLittleEndian, false, false);
                case WKBGeometryType.wkbPolygonZ:
                    return ReadPolygon(reader, isLittleEndian, true, false);
                case WKBGeometryType.wkbPolygonM:
                    return ReadPolygon(reader, isLittleEndian, false, true);
                case WKBGeometryType.wkbPolygonZM:
                    return ReadPolygon(reader, isLittleEndian, true, true);
                case WKBGeometryType.wkbMultiPoint:
                    return ReadMultiPoint(reader, isLittleEndian, false, false);
                case WKBGeometryType.wkbMultiPointZ:
                    return ReadMultiPoint(reader, isLittleEndian, true, false);
                case WKBGeometryType.wkbMultiPointM:
                    return ReadMultiPoint(reader, isLittleEndian, false, true);
                case WKBGeometryType.wkbMultiPointZM:
                    return ReadMultiPoint(reader, isLittleEndian, true, true);
                case WKBGeometryType.wkbMultiLineString:
                    return ReadMultiLineString(reader, isLittleEndian, false, false);
                case WKBGeometryType.wkbMultiLineStringZ:
                    return ReadMultiLineString(reader, isLittleEndian, true, false);
                case WKBGeometryType.wkbMultiLineStringM:
                    return ReadMultiLineString(reader, isLittleEndian, false, true);
                case WKBGeometryType.wkbMultiLineStringZM:
                    return ReadMultiLineString(reader, isLittleEndian, true, true);
                case WKBGeometryType.wkbMultiPolygon:
                    return ReadMultiPolygon(reader, isLittleEndian, false, false);
                case WKBGeometryType.wkbMultiPolygonZ:
                    return ReadMultiPolygon(reader, isLittleEndian, true, false);
                case WKBGeometryType.wkbMultiPolygonM:
                    return ReadMultiPolygon(reader, isLittleEndian, false, true);
                case WKBGeometryType.wkbMultiPolygonZM:
                    return ReadMultiPolygon(reader, isLittleEndian, true, true);
                case WKBGeometryType.wkbGeometryCollection:
                    return ReadGeometryCollection(reader, isLittleEndian, false, false);
                case WKBGeometryType.wkbGeometryCollectionZ:
                    return ReadGeometryCollection(reader, isLittleEndian, true, false);
                case WKBGeometryType.wkbGeometryCollectionM:
                    return ReadGeometryCollection(reader, isLittleEndian, false, true);
                case WKBGeometryType.wkbGeometryCollectionZM:
                    return ReadGeometryCollection(reader, isLittleEndian, true, true);
            }
            return null;
        }

        #endregion

    }


}
