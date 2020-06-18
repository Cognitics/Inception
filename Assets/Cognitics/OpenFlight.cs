using System.Collections.Generic;

namespace Cognitics.OpenFlight
{
    public enum Opcode : ushort
    {
        Invalid = 0,
        Header = 1,
        Group = 2,
        Object = 4,
        Face = 5,
        PushLevel = 10,
        PopLevel = 11,
        DegreeOfFreedom = 14,
        PushSubFace = 19,
        PopSubFace = 20,
        PushExtension = 21,
        PopExtension = 22,
        Continuation = 23,
        Comment = 31,
        ColorPalette = 32,
        LongId = 33,
        Matrix = 49,
        Vector = 50,
        MultiTexture = 52,
        UvList = 53,
        BinarySeparatingPlane = 55,
        Replicate = 60,
        InstanceReference = 61,
        InstanceDefinition = 62,
        ExternalReference = 63,
        TexturePalette = 64,
        VertexPalette = 67,
        VertexWithColor = 68,
        VertexWithColorNormal = 69,
        VertexWithColorNormalUV = 70,
        VertexWithColorUV = 71,
        VertexList = 72,
        LevelOfDetail = 73,
        BoundingBox = 74,
        RotateAboutEdge = 76,
        Translate = 78,
        Scale = 79,
        RotateAboutPoint = 80,
        RotateScaleToPoint = 81,
        Put = 82,
        EyePointTrackPlanePalette = 83,
        Mesh = 84,
        LocalVertexPool = 85,
        MeshPrimitive = 86,
        RoadSegment = 87,
        RoadZone = 88,
        MorphVertexList = 89,
        LinkagePalette = 90,
        Sound = 91,
        RoadPath = 92,
        SoundPalette = 93,
        GeneralMatrix = 94,
        Text = 95,
        Switch = 96,
        LineStylePalette = 97,
        ClipRegion = 98,
        Extension = 100,
        LightSource = 101,
        LightSourcePalette = 102,
        BoundingSphere = 105,
        BoundingCylinder = 106,
        BoundingConvexHull = 107,
        BoundingVolumeCenter = 108,
        BoundingVolumeOrientation = 109,
        LightPoint = 111,
        TextureMappingPalette = 112,
        MaterialPalette = 113,
        NameTable = 114,
        Cat = 115,
        CatData = 116,
        BoundingHistogram = 119,
        PushAttribute = 122,
        PopAttribute = 123,
        Curve = 126,
        RoadConstruction = 127,
        LightPointAppearancePalette = 128,
        LightPointAnimationPalette = 129,
        IndexedLightPoint = 130,
        LightPointSystem = 131,
        IndexedString = 132,
        ShaderPalette = 133,
        ExtendedMaterialHeader = 135,
        ExtendedMaterialAmbient = 136,
        ExtendedMaterialDiffuse = 137,
        ExtendedMaterialSpecular = 138,
        ExtendedMaterialEmissive = 139,
        ExtendedMaterialAlpha = 140,
        ExtendedMaterialLightMap = 141,
        ExtendedMaterialNormalMap = 142,
        ExtendedMaterialBumpMap = 143,
        ExtendedMaterialShadowMap = 145,
        ExtendedMaterialReflectionMap = 147,
        Point = 147,
    }

    public struct Record
    {
        public int Position { get; }
        public Opcode Opcode { get; }
        public ushort Length { get; }

        internal Record(int position, Opcode opcode, ushort length)
        {
            Position = position;
            Opcode = opcode;
            Length = length;
        }

        internal Record(BinaryParser parser)
        {
            Position = parser.Position;
            Opcode = (Opcode)parser.UInt16BE();
            Length = parser.UInt16BE();
            parser.Position = Position + Length;
        }

        public override string ToString() => string.Format("[{0}] {1}:{2} ({3})", Position, (int)Opcode, Opcode.ToString(), Length);
    }

    public class Node
    {
        public Record Record;
        public List<Record> Records = new List<Record>();
        public List<Node> Children = new List<Node>();

        internal Node(BinaryParser parser)
        {
            Record = new Record(parser);
            bool pushed = false;
            while (parser.Position < parser.Bytes.Length)
            {
                var opcode = (Opcode)parser.UInt16BE();
                parser.Position -= 2;
                switch (opcode)
                {
                    case Opcode.PushLevel:
                        new Record(parser);
                        pushed = true;
                        break;
                    case Opcode.PopLevel:
                        if(pushed)
                            new Record(parser);
                        return;
                    case Opcode.Header:
                    case Opcode.Group:
                    case Opcode.Object:
                    case Opcode.Face:
                    case Opcode.Mesh:
                    case Opcode.Point:
                    case Opcode.LightPoint:
                    case Opcode.LightPointSystem:
                    case Opcode.DegreeOfFreedom:
                    case Opcode.VertexList:
                    case Opcode.MorphVertexList:
                    case Opcode.BinarySeparatingPlane:
                    case Opcode.ExternalReference:
                    case Opcode.LevelOfDetail:
                    case Opcode.Sound:
                    case Opcode.LightSource:
                    case Opcode.RoadSegment:
                    case Opcode.RoadConstruction:
                    case Opcode.RoadPath:
                    case Opcode.ClipRegion:
                    case Opcode.Text:
                    case Opcode.Switch:
                    case Opcode.Cat:
                    case Opcode.Extension:
                    case Opcode.Curve:
                        if (!pushed)
                            return;
                        Children.Add(new Node(parser));
                        break;
                    default:
                        Records.Add(new Record(parser));
                        break;
                }
            }
        }

        public void Dump(System.IO.StreamWriter writer, int depth = 0)
        {
            var indent = new string(' ', depth * 2);
            writer.WriteLine("{0}{1}", indent, Record.ToString());
            foreach (var record in Records)
                writer.WriteLine("{0}  {1}", indent, record.ToString());
            foreach (var child in Children)
                child.Dump(writer, depth + 1);
        }

    }

    public class Group
    {
        public string ID;
        // TODO
    }

    public class Object
    {
        public string ID;
        // TODO
    }

    public class Face
    {
        public string ID;
        public int IRColorCode;
        public short RelativePriority;
        public byte DrawType;
        public byte TextureWhite;
        public ushort ColorNameIndex;
        public ushort AlternateColorNameIndex;
        public byte Template = 0;
        public short DetailTexturePatternIndex = -1;
        public short TexturePatternIndex = -1;
        public short MaterialIndex = -1;
        public short SurfaceMaterialCode;
        public short FeatureID;
        public int IRMaterialCode;
        public ushort Transparency;
        public byte LODGenerationControl;
        public byte LineStyleIndex;
        public uint Flags = 0;
        public byte LightMode = 0;
        public uint PackedColorPrimaryABGR;
        public uint PackedColorAlternateABGR;
        public short TextureMappingIndex = -1;
        public int PrimaryColorIndex = -1;
        public int AlternateColorIndex = -1;
        public short ShaderIndex = -1;
    }

    public class Mesh
    {
        public string ID;
        public int IRColorCode;
        public short RelativePriority;
        public byte DrawType;
        public byte TextureWhite;
        public ushort ColorNameIndex;
        public ushort AlternateColorNameIndex;
        public byte Template = 0;
        public short DetailTexturePatternIndex = -1;
        public short TexturePatternIndex = -1;
        public short MaterialIndex = -1;
        public short SurfaceMaterialCode;
        public short FeatureID;
        public int IRMaterialCode;
        public ushort Transparency;
        public byte LODGenerationControl;
        public byte LineStyleIndex;
        public uint Flags = 0;
        public byte LightMode = 0;
        public uint PackedColorPrimaryABGR;
        public uint PackedColorAlternateABGR;
        public short TextureMappingIndex = -1;
        public int PrimaryColorIndex = -1;
        public int AlternateColorIndex = -1;
        public short ShaderIndex = -1;
    }

    public class VertexList
    {
        public int[] Offsets;
    }

    public class LevelOfDetail
    {
        public string ID;
        public double SwitchInDistance;
        public double SwitchOutDistance;
        // TODO
    }

    public struct RGBA32 { public byte R, G, B, A; }
    public struct RGB { public float R, G, B; }
    public struct Coordinate { public double X, Y, Z; }
    public struct Normal { public float I, J, K; }
    public struct UV { public float U, V; }

    public class ColorPalette
    {
        public RGBA32[] Colors = new RGBA32[1024];
        public Dictionary<int, string> Names = new Dictionary<int, string>();
    }

    public class MaterialPalette
    {
        public int Index;
        public string Name;
        public RGB Ambient;
        public RGB Diffuse;
        public RGB Specular;
        public RGB Emissive;
        public float Shininess = 0f;
        public float Alpha = 0f;
    }

    public class TexturePalette
    {
        public int Index;
        public string Filename;
        public int X = 0;
        public int Y = 0;
    }

    public class VertexPaletteEntry
    {
        public int ColorNameIndex = 0;
        public bool IsStartHardEdge = false;
        public bool IsNormalFrozen = false;
        public bool IsNoColor = false;
        public bool IsPackedColor = false;
        public bool HasNormal = false;
        public bool HasUV = false;
        public Coordinate Coordinate = new Coordinate();
        public Normal Normal = new Normal();
        public UV UV = new UV();
        public RGBA32 PackedColor = new RGBA32();
        public uint VertexColorIndex = 0;
    }

    public class LocalVertexPool
    {
        public bool HasPosition;
        public bool HasColorIndex;
        public bool HasRGBAColor;
        public bool HasNormal;
        public bool HasBaseUV;
        public bool HasUV1;
        public bool HasUV2;
        public bool HasUV3;
        public bool HasUV4;
        public bool HasUV5;
        public bool HasUV6;
        public bool HasUV7;
        public Coordinate[] Coordinates;
        public RGBA32[] Colors;
        public Normal[] Normals;
        public UV[] UVBase;
        public UV[] UV1;
        public UV[] UV2;
        public UV[] UV3;
        public UV[] UV4;
        public UV[] UV5;
        public UV[] UV6;
        public UV[] UV7;
    }

    public class MeshPrimitive
    {
        public int Type;
        public int[] Indices;
    }

    public class Parser
    {
        public string Filename { get; }
        private BinaryParser BinaryParser { get; }

        public Parser(string filename) : this(System.IO.File.ReadAllBytes(filename))
        {
            Filename = filename;
        }

        public Parser(byte[] bytes)
        {
            BinaryParser = new BinaryParser(bytes);
        }

        public List<Record> Records()
        {
            var result = new List<Record>();
            while (BinaryParser.Position < BinaryParser.Bytes.Length)
                result.Add(new Record(BinaryParser));
            return result;
        }

        public Node Node()
        {
            return new Node(BinaryParser);
        }

        public void DumpNodes(System.IO.StreamWriter writer)
        {
            BinaryParser.Position = 0;
            Node().Dump(writer);
            writer.Flush();
        }
        public void DumpNodes(string filename) => DumpNodes(new System.IO.StreamWriter(filename));

        public void DumpRecords(System.IO.StreamWriter writer)
        {
            BinaryParser.Position = 0;
            var records = Records();
            foreach (var record in records)
                writer.WriteLine(record.ToString());
            writer.Flush();
        }
        public void DumpRecords(string filename) => DumpRecords(new System.IO.StreamWriter(filename));

        public Group ParseGroup(Record record)
        {
            BinaryParser.Position = record.Position + 4;
            var result = new Group();
            result.ID = BinaryParser.String(8);
            return result;
        }

        public Object ParseObject(Record record)
        {
            BinaryParser.Position = record.Position + 4;
            var result = new Object();
            result.ID = BinaryParser.String(8);
            return result;
        }

        public Face ParseFace(Record record)
        {
            BinaryParser.Position = record.Position + 4;
            var result = new Face();
            result.ID = BinaryParser.String(8);
            result.IRColorCode = BinaryParser.Int32BE();
            result.RelativePriority = BinaryParser.Int16BE();
            result.DrawType = BinaryParser.Byte();
            result.TextureWhite = BinaryParser.Byte();
            result.ColorNameIndex = BinaryParser.UInt16BE();
            result.AlternateColorNameIndex = BinaryParser.UInt16BE();
            BinaryParser.Position += 1;
            result.Template = BinaryParser.Byte();
            result.DetailTexturePatternIndex = BinaryParser.Int16BE();
            result.TexturePatternIndex = BinaryParser.Int16BE();
            result.MaterialIndex = BinaryParser.Int16BE();
            result.SurfaceMaterialCode = BinaryParser.Int16BE();
            result.FeatureID = BinaryParser.Int16BE();
            result.IRMaterialCode = BinaryParser.Int32BE();
            result.Transparency = BinaryParser.UInt16BE();
            result.LODGenerationControl = BinaryParser.Byte();
            result.LineStyleIndex = BinaryParser.Byte();
            result.Flags = BinaryParser.UInt32BE();
            result.LightMode = BinaryParser.Byte();
            BinaryParser.Position += 7;
            result.PackedColorPrimaryABGR = BinaryParser.UInt32BE();
            result.PackedColorAlternateABGR = BinaryParser.UInt32BE();
            result.TextureMappingIndex = BinaryParser.Int16BE();
            BinaryParser.Position += 2;
            result.PrimaryColorIndex = BinaryParser.Int32BE();
            result.AlternateColorIndex = BinaryParser.Int32BE();
            BinaryParser.Position += 2;
            result.ShaderIndex = BinaryParser.Int16BE();
            return result;
        }

        public Mesh ParseMesh(Record record)
        {
            BinaryParser.Position = record.Position + 4;
            var result = new Mesh();
            result.ID = BinaryParser.String(8);
            BinaryParser.Position += 4;
            result.IRColorCode = BinaryParser.Int32BE();
            result.RelativePriority = BinaryParser.Int16BE();
            result.DrawType = BinaryParser.Byte();
            result.TextureWhite = BinaryParser.Byte();
            result.ColorNameIndex = BinaryParser.UInt16BE();
            result.AlternateColorNameIndex = BinaryParser.UInt16BE();
            BinaryParser.Position += 1;
            result.Template = BinaryParser.Byte();
            result.DetailTexturePatternIndex = BinaryParser.Int16BE();
            result.TexturePatternIndex = BinaryParser.Int16BE();
            result.MaterialIndex = BinaryParser.Int16BE();
            result.SurfaceMaterialCode = BinaryParser.Int16BE();
            result.FeatureID = BinaryParser.Int16BE();
            result.IRMaterialCode = BinaryParser.Int32BE();
            result.Transparency = BinaryParser.UInt16BE();
            result.LODGenerationControl = BinaryParser.Byte();
            result.LineStyleIndex = BinaryParser.Byte();
            result.Flags = BinaryParser.UInt32BE();
            result.LightMode = BinaryParser.Byte();
            BinaryParser.Position += 7;
            result.PackedColorPrimaryABGR = BinaryParser.UInt32BE();
            result.PackedColorAlternateABGR = BinaryParser.UInt32BE();
            result.TextureMappingIndex = BinaryParser.Int16BE();
            BinaryParser.Position += 2;
            result.PrimaryColorIndex = BinaryParser.Int32BE();
            result.AlternateColorIndex = BinaryParser.Int32BE();
            BinaryParser.Position += 2;
            result.ShaderIndex = BinaryParser.Int16BE();
            return result;
        }

        public LocalVertexPool ParseLocalVertexPool(Record record)
        {
            BinaryParser.Position = record.Position + 4;
            var result = new LocalVertexPool();
            var count = BinaryParser.UInt32BE();
            var flags = BinaryParser.UInt32BE();
            result.HasPosition =   (flags & (1 << 31)) != 0;
            result.HasColorIndex = (flags & (1 << 30)) != 0;
            result.HasRGBAColor =  (flags & (1 << 29)) != 0;
            result.HasNormal =     (flags & (1 << 28)) != 0;
            result.HasBaseUV =     (flags & (1 << 27)) != 0;
            result.HasUV1 =        (flags & (1 << 26)) != 0;
            result.HasUV2 =        (flags & (1 << 25)) != 0;
            result.HasUV3 =        (flags & (1 << 24)) != 0;
            result.HasUV4 =        (flags & (1 << 23)) != 0;
            result.HasUV5 =        (flags & (1 << 22)) != 0;
            result.HasUV6 =        (flags & (1 << 21)) != 0;
            result.HasUV7 =        (flags & (1 << 20)) != 0;
            if (result.HasPosition)
                result.Coordinates = new Coordinate[count];
            if (result.HasColorIndex || result.HasRGBAColor)
                result.Colors = new RGBA32[count];
            if (result.HasNormal)
                result.Normals = new Normal[count];
            if (result.HasBaseUV)
                result.UVBase = new UV[count];
            if (result.HasUV1)
                result.UV1 = new UV[count];
            if (result.HasUV2)
                result.UV2 = new UV[count];
            if (result.HasUV3)
                result.UV3 = new UV[count];
            if (result.HasUV4)
                result.UV4 = new UV[count];
            if (result.HasUV5)
                result.UV5 = new UV[count];
            if (result.HasUV6)
                result.UV6 = new UV[count];
            if (result.HasUV7)
                result.UV7 = new UV[count];
            for (int i = 0; i < count; ++i)
            {
                if (result.HasPosition)
                {
                    result.Coordinates[i].X = BinaryParser.DoubleBE();
                    result.Coordinates[i].Y = BinaryParser.DoubleBE();
                    result.Coordinates[i].Z = BinaryParser.DoubleBE();
                }
                if (result.HasColorIndex || result.HasRGBAColor)
                {
                    result.Colors[i].A = BinaryParser.Byte();
                    result.Colors[i].B = BinaryParser.Byte();
                    result.Colors[i].G = BinaryParser.Byte();
                    result.Colors[i].R = BinaryParser.Byte();
                }
                if (result.HasNormal)
                {
                    result.Normals[i].I = BinaryParser.SingleBE();
                    result.Normals[i].J = BinaryParser.SingleBE();
                    result.Normals[i].K = BinaryParser.SingleBE();
                }
                if (result.HasBaseUV)
                {
                    result.UVBase[i].U = BinaryParser.SingleBE();
                    result.UVBase[i].V = BinaryParser.SingleBE();
                }
                if (result.HasUV1)
                {
                    result.UV1[i].U = BinaryParser.SingleBE();
                    result.UV1[i].V = BinaryParser.SingleBE();
                }
                if (result.HasUV2)
                {
                    result.UV2[i].U = BinaryParser.SingleBE();
                    result.UV2[i].V = BinaryParser.SingleBE();
                }
                if (result.HasUV3)
                {
                    result.UV3[i].U = BinaryParser.SingleBE();
                    result.UV3[i].V = BinaryParser.SingleBE();
                }
                if (result.HasUV4)
                {
                    result.UV4[i].U = BinaryParser.SingleBE();
                    result.UV4[i].V = BinaryParser.SingleBE();
                }
                if (result.HasUV5)
                {
                    result.UV5[i].U = BinaryParser.SingleBE();
                    result.UV5[i].V = BinaryParser.SingleBE();
                }
                if (result.HasUV6)
                {
                    result.UV6[i].U = BinaryParser.SingleBE();
                    result.UV6[i].V = BinaryParser.SingleBE();
                }
                if (result.HasUV7)
                {
                    result.UV7[i].U = BinaryParser.SingleBE();
                    result.UV7[i].V = BinaryParser.SingleBE();
                }
            }
            return result;
        }

        public MeshPrimitive ParseMeshPrimitive(Record record)
        {
            BinaryParser.Position = record.Position + 4;
            var result = new MeshPrimitive();
            result.Type = BinaryParser.Int16BE();
            var bytes_per_index = BinaryParser.UInt16BE();
            var count = BinaryParser.UInt32BE();
            result.Indices = new int[count];
            if (bytes_per_index == 1)
            {
                for (int i = 0; i < count; ++i)
                    result.Indices[i] = BinaryParser.Byte();
            }
            if (bytes_per_index == 2)
            {
                for (int i = 0; i < count; ++i)
                    result.Indices[i] = BinaryParser.UInt16BE();
            }
            if (bytes_per_index == 4)
            {
                for (int i = 0; i < count; ++i)
                    result.Indices[i] = BinaryParser.Int32BE();
            }
            return result;
        }

        public VertexList ParseVertexList(Record record)
        {
            BinaryParser.Position = record.Position + 4;
            var result = new VertexList();
            int count = (record.Length - 4) / 4;
            result.Offsets = new int[count];
            for (int i = 0; i < count; ++i)
                result.Offsets[i] = BinaryParser.Int32BE();
            return result;
        }

        public LevelOfDetail ParseLevelOfDetail(Record record)
        {
            BinaryParser.Position = record.Position + 4;
            var result = new LevelOfDetail();
            result.ID = BinaryParser.String(8);
            BinaryParser.Position += 4;
            result.SwitchInDistance = BinaryParser.DoubleBE();
            result.SwitchOutDistance = BinaryParser.DoubleBE();
            return result;
        }

        public ColorPalette ParseColorPalette(Record record)
        {
            BinaryParser.Position = record.Position + 4;
            var result = new ColorPalette();
            BinaryParser.Position += 128;
            for (int i = 0; i < 1024; ++i)
            {
                result.Colors[i].A = BinaryParser.Byte();
                result.Colors[i].B = BinaryParser.Byte();
                result.Colors[i].G = BinaryParser.Byte();
                result.Colors[i].R = BinaryParser.Byte();
            }
            if (record.Length <= 4228)
                return result;
            int namesCount = BinaryParser.Int32BE();
            for (int i = 0; i < namesCount; ++i)
            {
                ushort len = BinaryParser.UInt16BE();
                BinaryParser.Position += 2;
                ushort index = BinaryParser.UInt16BE();
                BinaryParser.Position += 2;
                result.Names[index] = BinaryParser.String(len - 8);
            }
            return result;
        }

        public MaterialPalette ParseMaterialPalette(Record record)
        {
            BinaryParser.Position = record.Position + 4;
            var result = new MaterialPalette();
            result.Index = BinaryParser.Int32BE();
            result.Name = BinaryParser.String(12);
            BinaryParser.Position += 4;   // flags
            result.Ambient.R = BinaryParser.SingleBE();
            result.Ambient.G = BinaryParser.SingleBE();
            result.Ambient.B = BinaryParser.SingleBE();
            result.Diffuse.R = BinaryParser.SingleBE();
            result.Diffuse.G = BinaryParser.SingleBE();
            result.Diffuse.B = BinaryParser.SingleBE();
            result.Specular.R = BinaryParser.SingleBE();
            result.Specular.G = BinaryParser.SingleBE();
            result.Specular.B = BinaryParser.SingleBE();
            result.Emissive.R = BinaryParser.SingleBE();
            result.Emissive.G = BinaryParser.SingleBE();
            result.Emissive.B = BinaryParser.SingleBE();
            result.Shininess = BinaryParser.SingleBE();
            result.Alpha = BinaryParser.SingleBE();
            return result;
        }

        public TexturePalette ParseTexturePalette(Record record)
        {
            BinaryParser.Position = record.Position + 4;
            var result = new TexturePalette();
            result.Filename = BinaryParser.String(200);
            result.Index = BinaryParser.Int32BE();
            result.X = BinaryParser.Int32BE();
            result.Y = BinaryParser.Int32BE();
            if (Filename.Length > 0)
                result.Filename = System.IO.Path.GetDirectoryName(Filename) + "/" + result.Filename;
            return result;
        }

        public Dictionary<int, VertexPaletteEntry> ParseVertexPalette(Record record)
        {
            var palette_position = record.Position;
            BinaryParser.Position = record.Position + record.Length;
            var result = new Dictionary<int, VertexPaletteEntry>();
            while (BinaryParser.Position < BinaryParser.Bytes.Length)
            {
                var entry_record = new Record(BinaryParser);
                var entry_position = entry_record.Position - palette_position;
                switch (entry_record.Opcode)
                {
                    case Opcode.VertexWithColor:
                        result.Add(entry_position, ParseVertexWithColorRecord(entry_record));
                        break;
                    case Opcode.VertexWithColorNormal:
                        result.Add(entry_position, ParseVertexWithColorNormalRecord(entry_record));
                        break;
                    case Opcode.VertexWithColorNormalUV:
                        result.Add(entry_position, ParseVertexWithColorNormalUVRecord(entry_record));
                        break;
                    case Opcode.VertexWithColorUV:
                        result.Add(entry_position, ParseVertexWithColorUVRecord(entry_record));
                        break;
                    default:
                        return result;
                }
                BinaryParser.Position = entry_record.Position + entry_record.Length;
            }
            return result;
        }

        public VertexPaletteEntry ParseVertexWithColorRecord(Record record)
        {
            BinaryParser.Position = record.Position + 4;
            var entry = new VertexPaletteEntry();
            entry.ColorNameIndex = BinaryParser.UInt16BE();
            ushort flags = BinaryParser.UInt16BE();
            // TODO: handle flags
            entry.Coordinate.X = BinaryParser.DoubleBE();
            entry.Coordinate.Y = BinaryParser.DoubleBE();
            entry.Coordinate.Z = BinaryParser.DoubleBE();
            entry.PackedColor.A = BinaryParser.Byte();
            entry.PackedColor.B = BinaryParser.Byte();
            entry.PackedColor.G = BinaryParser.Byte();
            entry.PackedColor.R = BinaryParser.Byte();
            entry.ColorNameIndex = BinaryParser.UInt16BE();
            entry.VertexColorIndex = BinaryParser.UInt32BE();
            return entry;
        }

        public VertexPaletteEntry ParseVertexWithColorNormalRecord(Record record)
        {
            BinaryParser.Position = record.Position + 4;
            var entry = new VertexPaletteEntry();
            entry.ColorNameIndex = BinaryParser.UInt16BE();
            ushort flags = BinaryParser.UInt16BE();
            // TODO: handle flags
            entry.Coordinate.X = BinaryParser.DoubleBE();
            entry.Coordinate.Y = BinaryParser.DoubleBE();
            entry.Coordinate.Z = BinaryParser.DoubleBE();
            entry.HasNormal = true;
            entry.Normal.I = BinaryParser.SingleBE();
            entry.Normal.J = BinaryParser.SingleBE();
            entry.Normal.K = BinaryParser.SingleBE();
            entry.PackedColor.A = BinaryParser.Byte();
            entry.PackedColor.B = BinaryParser.Byte();
            entry.PackedColor.G = BinaryParser.Byte();
            entry.PackedColor.R = BinaryParser.Byte();
            entry.ColorNameIndex = BinaryParser.UInt16BE();
            entry.VertexColorIndex = BinaryParser.UInt32BE();
            return entry;
        }

        public VertexPaletteEntry ParseVertexWithColorNormalUVRecord(Record record)
        {
            BinaryParser.Position = record.Position + 4;
            var entry = new VertexPaletteEntry();
            entry.ColorNameIndex = BinaryParser.UInt16BE();
            ushort flags = BinaryParser.UInt16BE();
            // TODO: handle flags
            entry.Coordinate.X = BinaryParser.DoubleBE();
            entry.Coordinate.Y = BinaryParser.DoubleBE();
            entry.Coordinate.Z = BinaryParser.DoubleBE();
            entry.HasNormal = true;
            entry.Normal.I = BinaryParser.SingleBE();
            entry.Normal.J = BinaryParser.SingleBE();
            entry.Normal.K = BinaryParser.SingleBE();
            entry.HasUV = true;
            entry.UV.U = BinaryParser.SingleBE();
            entry.UV.V = BinaryParser.SingleBE();
            entry.PackedColor.A = BinaryParser.Byte();
            entry.PackedColor.B = BinaryParser.Byte();
            entry.PackedColor.G = BinaryParser.Byte();
            entry.PackedColor.R = BinaryParser.Byte();
            entry.ColorNameIndex = BinaryParser.UInt16BE();
            entry.VertexColorIndex = BinaryParser.UInt32BE();
            return entry;
        }

        public VertexPaletteEntry ParseVertexWithColorUVRecord(Record record)
        {
            BinaryParser.Position = record.Position + 4;
            var entry = new VertexPaletteEntry();
            entry.ColorNameIndex = BinaryParser.UInt16BE();
            ushort flags = BinaryParser.UInt16BE();
            // TODO: handle flags
            entry.Coordinate.X = BinaryParser.DoubleBE();
            entry.Coordinate.Y = BinaryParser.DoubleBE();
            entry.Coordinate.Z = BinaryParser.DoubleBE();
            entry.HasUV = true;
            entry.UV.U = BinaryParser.SingleBE();
            entry.UV.V = BinaryParser.SingleBE();
            entry.PackedColor.A = BinaryParser.Byte();
            entry.PackedColor.B = BinaryParser.Byte();
            entry.PackedColor.G = BinaryParser.Byte();
            entry.PackedColor.R = BinaryParser.Byte();
            entry.ColorNameIndex = BinaryParser.UInt16BE();
            entry.VertexColorIndex = BinaryParser.UInt32BE();
            return entry;
        }





    }

}
