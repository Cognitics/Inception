using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;


namespace Cognitics.CDB
{
    public static class Shapefile
    {
        public static List<Feature> ReadFeatures(string filename)
        {
            if (!File.Exists(filename))
            {
                Console.WriteLine("The file " + filename + " does not exist.");
                return new List<Feature>();
            }
            var result = new List<Feature>();
            ShapefileDataReader reader = TryShapefileDataReader(filename);
            if (reader == null)
                return new List<Feature>();

            try
            {
                while (reader.Read())
                {
                    var feature = new Feature();
                    var attr = new AttributesTable();
                    var geometry = (Geometry)reader.Geometry;
                    for (int i = 0; i < reader.DbaseHeader.NumFields; ++i)
                        attr.Add(reader.DbaseHeader.Fields[i].Name, reader.GetValue(i + 1));
                    feature.Geometry = geometry;
                    feature.Attributes = attr;
                    result.Add(feature);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in feature read: " + ex.ToString() + ", filename: " + filename);
            }
            reader.Close();
            reader.Dispose();

            return result;
        }

        public static Dictionary<string, AttributesTable> ReadClassAttributes(string filename)
        {
            if (!File.Exists(filename))
            {
                Console.WriteLine("The file " + filename + " does not exist.");
                return new Dictionary<string, AttributesTable>();
            }
            var result = new Dictionary<string, AttributesTable>();
            DbaseFileReader reader = TryDbaseFileReader(filename);
            if (reader == null)
                return new Dictionary<string, AttributesTable>();

            var header = reader.GetHeader();
            int classNameIndex = 0;
            for (int i = 0; i < header.NumFields; ++i)
            {
                if (header.Fields[i].Name == "CNAM")
                    classNameIndex = i;
            }
            foreach (ArrayList entry in reader)
            {
                string className = (string)entry[classNameIndex];
                result[className] = new AttributesTable();
                for (int i = 0; i < header.NumFields; ++i)
                    result[className].Add(header.Fields[i].Name, entry[i]);
            }
            return result;
        }

        public static Dictionary<int, AttributesTable> ReadExtendedAttributes(string filename)
        {
            if (!File.Exists(filename))
            {
                Console.WriteLine("The file " + filename + " does not exist.");
                return new Dictionary<int, AttributesTable>();
            }
            var result = new Dictionary<int, AttributesTable>();

            DbaseFileReader reader = TryDbaseFileReader(filename);
            if (reader == null)
                return new Dictionary<int, AttributesTable>();

            var header = reader.GetHeader();
            int linkIndex = 0;
            for (int i = 0; i < header.NumFields; ++i)
            {
                if (header.Fields[i].Name == "LNK")
                    linkIndex = i;
            }
            foreach (ArrayList entry in reader)
            {
                int link = (int)entry[linkIndex];
                Console.WriteLine("LNK=" + link.ToString());
                result[link] = new AttributesTable();
                for (int i = 0; i < header.NumFields; ++i)
                    result[link].Add(header.Fields[i].Name, entry[i]);
            }
            return result;
        }


        private static ShapefileDataReader TryShapefileDataReader(string filename)
        {
            var factory = new GeometryFactory();
            ShapefileDataReader reader;

            try
            {
                reader = new ShapefileDataReader(filename, factory);
            }
            catch(Exception e)
            {
                Console.WriteLine("The file " + filename + " did not load correctly: " + e.Message);
                return null;
            }
            reader.Reset();

            return reader;
        }

        private static DbaseFileReader TryDbaseFileReader(string filename)
        {
            DbaseFileReader reader;
            try
            {
                reader = new DbaseFileReader(filename);
            }
            catch (Exception e)
            {
                Console.WriteLine("The file " + filename + " did not load correctly: " + e.Message);
                return null;
            }

            return reader;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // the rest is debug and can be deleted when this class is fully tested

        public static void DumpFeatures(List<Feature> features)
        {
            features.ForEach(f => DumpFeature(f));
        }

        public static void DumpFeature(Feature feature)
        {
            Console.WriteLine("FEATURE: " + feature.Geometry.ToString());
            DumpAttributes("ATTR", (AttributesTable)feature.Attributes);
        }

        public static void DumpAttributes(string title, AttributesTable attributes)
        {
            string str = "[" + title + "] ";
            var names = attributes.GetNames();
            foreach (var name in names)
                str += name + "=" + attributes[name] + " ; ";
            Console.WriteLine(str);
        }



        static void DumpShapeHeader(ShapefileHeader header)
        {
            var bounds = header.Bounds;
            Console.WriteLine(string.Format("ShapeType: {0}", header.ShapeType));
            Console.WriteLine(string.Format("Bounds: ({0},{1}) ({2},{3})", bounds.MinX, bounds.MinY, bounds.MaxX, bounds.MaxY));
        }
        static void DumpDbaseHeader(DbaseFileHeader header)
        {
            Console.WriteLine(string.Format("DBF: Fields.Length = {0} ; NumRecords = {1}", header.Fields.Length, header.NumRecords));
            for (int i = 0; i < header.NumFields; ++i)
            {
                var field = header.Fields[i];
                Console.WriteLine(string.Format("  [{0}] {1}", field.DbaseType, field.Name));
            }
        }


    }
}
