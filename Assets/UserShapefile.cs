using System;
using System.Collections.Generic;
using System.IO;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite.IO.KML;
using System.Text;

public class UserShapefile
{
    public static void WriteFeaturesToShapefile(string filename, List<Feature> features)
    {
        if (File.Exists(filename + ".shp"))
            File.Delete(filename + ".shp");

        if (File.Exists(filename + ".dbf"))
            File.Delete(filename + ".dbf");

        if (File.Exists(filename + ".shx"))
            File.Delete(filename + ".shx");

        if (features.Count == 0)
            return;

        var outGeomFactory = GeometryFactory.Default;
        var writer = new ShapefileDataWriter(filename, outGeomFactory);
        var outDbaseHeader = ShapefileDataWriter.GetHeader(features[0], features.Count);
        writer.Header = outDbaseHeader;
        writer.Write(features);
    }

    public static void WriteFeaturesToKML(string path, List<Feature> features)
    {
        string filename = path + "KMLFeatures" + ".kml";
        string header = "<?xml version=\"1.0\" encoding=\"UTF-8\" ?>\n";
        string documentOpen = "<Document>\n";
        string xmlns = "<kml xmlns=\"http://www.opengis.net/kml/2.2\">\n";
        string placemarkOpen = "<Placemark>\n";
        string placemarkClose = "</Placemark>\n";
        string documentClose = "</Document>\n";
        string kmlClose = "</kml>\n";

        KMLWriter writer = new KMLWriter();
        StringBuilder sb = new StringBuilder();
        string[] title = new string[features.Count];
        string[] description = new string[features.Count];

        for(int i = 0; i < features.Count; ++i)
        {
            sb.Append(placemarkOpen);
            sb.Append("<name>" + features[i].Attributes["Title"].ToString() + "</name>\n");
            sb.Append("<description>" + features[i].Attributes["Description"].ToString() + "</description>\n");
            writer.Write(features[i].Geometry, sb);
            sb.Append(placemarkClose);
        }

        if (File.Exists(filename))
            File.Delete(filename);

        string fileContents = header + xmlns + documentOpen + sb.ToString() + documentClose + kmlClose;

        using (StreamWriter streamWriter = File.CreateText(filename))
        {
            streamWriter.Write(fileContents);
            streamWriter.Close();
        }
    }

    //public static List<Feature> ReadFeatures(string filename)
    //{
    //    return Cognitics.CDB.Shapefile.ReadFeatures(filename);
    //}
}

