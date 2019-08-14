#if false
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Net;
using System.Net.Http;
using System.Xml;
using UnityEngine;
using BruTile;//
using BruTile.Predefined;//
using BruTile.Web;
using BruTile.Wms;//
using BruTile.Wmsc;
using GlobalSphericalMercator = BruTile.Predefined.GlobalSphericalMercator;
using HttpTileSource = BruTile.Web.HttpTileSource;
using KnownTileSources = BruTile.Predefined.KnownTileSources;
using KnownTileSource = BruTile.Predefined.KnownTileSource;

public class BruTileTester : MonoBehaviour//, BruTile.ITileProvider
{
    // TEMP: This is just a dummy data structure to show the received images in the inspector, for demonstration purposes
    [Serializable] public class Container
    {
        [HideInInspector] public string name = null;
        public Texture2D[] textures = null;
    }

    //public string[] urls = new string[]
    //{
    //    // Cognitics
    //    "http://tgs.cognitics.net:80/geoserver/wms",

    //    // WORLD OSM WMS
    //    // www.osm-wms.de
    //    // maps.heigit.org/osm-wms/service?
    //    // SERVICE=WMS&VERSION=1.3.0&REQUEST=GetCapabilities
    //    // SERVICE=WMS&VERSION=1.1.1&REQUEST=GetMap&LAYERS=osm_auto:all&STYLES=
    //    //            &BBOX=44.5,12,45.5,13&SRS=EPSG:4326&FORMAT=image/png&WIDTH=256&HEIGHT=256
    //    //"http://maps.heigit.org/osm-wms/service",
    //};

    public Container[] containers = null;
    public bool VerboseLogging = false;

    #region MonoBehaviour

    void Start()
    {
        WmsCapabilities wmsCapabilities = null;
        ITileSource tileSource = null;
        var containersList = new List<Container>();
        Service.ParseName = false;

        // capabilities documentation
        // http://www.opengis.net/wms?service=wms&version=1.3&request=GetCapabilities
        // http://schemas.opengis.net/wms/1.3.0/capabilities_1_3_0.xsd

        // TEMP: url and desired layer should be rolled into a struct
        const string url = "http://tgs.cognitics.net:80/geoserver/wms";
        const string desiredLayer = "Base:CDB Elevation_Terrain_Primary"; // this comes down as an option from capabilities query, so a UI would be preferable but we're just hardcoding elevation for now
        {
            string baseUrl = url + "?SERVICE=WMS";
            string capUrl = baseUrl + "&REQUEST=GetCapabilities";
            if (GetCapabilities(capUrl, out wmsCapabilities))
            {
                var version = wmsCapabilities.Version;
                //string firstLayer = wmsCapabilities.Capability.Layer.ChildLayers[0].Name;
                var formats = wmsCapabilities.Capability.Request.GetMap.Format;
                PrintFormats(ref formats, true);
                string compatibleFormat = null;
                foreach (string format in formats)
                {
                    if (format.ToLower().EndsWith("png") || format.ToLower().EndsWith("jpg") || format.ToLower().EndsWith("jpeg"))
                    {
                        compatibleFormat = format;
                        break;
                    }
                }

                if (GetMap(baseUrl, version.VersionString, desiredLayer, 0, 18, compatibleFormat, out tileSource))
                {
                    string levelId = "0"; // desired zoom level
                    containersList.Add(new Container() { name = tileSource.Name, textures = Process(tileSource, levelId) });
                }
                else
                {
                    Debug.LogError("GetMap failed.");
                }
            }
            else
            {
                Debug.LogError("GetCapabilities failed.");
            }
        }
/*
        // resource.sgu.se/service/wms/130/brunnar?
        // SERVICE=WMS&VERSION=1.3&REQUEST=GetCapabilities&TILED=true
        {
            //var res = new List<ITileSource>(WmscTileSource.CreateFromWmscCapabilties(uri));
            const string url = "http://resource.sgu.se/service/wms/130/brunnar?service=wms&version=1.3&request=GetCapabilities&TILED=true";
            GetCapabilities(url, out wmsCapabilities);
        }

        // another WMS
        {
            const string url = "http://geodata.nationaalgeoregister.nl/ahn25m/wms?service=wms";
            var schema = new WkstNederlandSchema {Format = "image/jpeg"};
            schema.Validate();
            var request = new WmscRequest(new Uri(url), schema, 
                                          layers: new[] { "ahn25m" }.ToList(), 
                                          styles: new string[0].ToList());
            var provider = new HttpTileProvider(request);
            string levelId = "0";
            tileSource = new TileSource(provider, schema);
            containersList.Add(new Container() { name = tileSource.Name, textures = Process(tileSource, request, levelId) });
        }

        // OpenStreetMap
        tileSource = new HttpTileSource(new GlobalSphericalMercator(0, 18),
            "http://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png",
            new[] { "a", "b", "c" }, "OSM", name: "OpenStreetMap Source");
        containersList.Add(new Container() { name = tileSource.Name, textures = Process(tileSource) });

        // Google Map
        tileSource = CreateGoogleTileSource("http://mt{s}.google.com/vt/lyrs=m@130&hl=en&x={x}&y={y}&z={z}", "Google Map Source");
        containersList.Add(new Container() { name = tileSource.Name, textures = Process(tileSource) });

        // Google Terrain
        tileSource = CreateGoogleTileSource("http://mt{s}.google.com/vt/lyrs=t@125,r@130&hl=en&x={x}&y={y}&z={z}", "Google Terrain Source");
        containersList.Add(new Container() { name = tileSource.Name, textures = Process(tileSource) });

        // Here Maps
        tileSource = new HttpTileSource(new GlobalSphericalMercator(0, 18),
            "https://{s}.base.maps.cit.api.here.com/maptile/2.1/maptile/newest/normal.day/{z}/{x}/{y}/256/png8?app_id=xWVIueSv6JL0aJ5xqTxb&app_code=djPZyynKsbTjIUDOBcHZ2g",
            new[] { "1", "2", "3", "4" }, name: "Here Maps Source");
        containersList.Add(new Container() { name = tileSource.Name, textures = Process(tileSource) });

        // BruTile known sources
        foreach (var knownTileSource in Enum.GetValues(typeof(KnownTileSource)).Cast<KnownTileSource>())
        {
            tileSource = KnownTileSources.Create(knownTileSource);
            containersList.Add(new Container() { name = knownTileSource.ToString(), textures = Process(tileSource) });
        }
*/
        containers = containersList.ToArray();
    }

    #endregion

    #region Queries

    private bool GetCapabilities(string url, out WmsCapabilities wmsCapabilities)
    {
        wmsCapabilities = null;
        try
        {
            Uri uri = null;
            try
            {
                uri = new Uri(url);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
            if (uri != null)
                wmsCapabilities = new WmsCapabilities(uri);
        }
        catch (System.Exception e)
        {
            // ArgumentException, ArgumentNullException, WmsParsingException
            Debug.LogException(e);
        }
        if (wmsCapabilities != null)
        {
            var cap = wmsCapabilities.Capability;
            var service = wmsCapabilities.Service;
            string updateSequence = wmsCapabilities.UpdateSequence;
            var serviceExceptionReport = wmsCapabilities.ServiceExceptionReport;
            WmsVersion version = wmsCapabilities.Version;

            Debug.LogFormat("Service: [Title: {0}], [Abstract: {1}], [LayerLimit: {2}]", string.IsNullOrEmpty(service.Title) ? "<none>" : service.Title, string.IsNullOrEmpty(service.Abstract) ? "<none>" : service.Abstract, service.LayerLimit != null ? service.LayerLimit.ToString() : "<none>");

            if (serviceExceptionReport != null)
            {
                Debug.Assert(serviceExceptionReport.ServiceExceptions.Count != 0);
                Debug.LogErrorFormat("serviceExceptionReport contains {0} exceptions", serviceExceptionReport.ServiceExceptions.Count);
                foreach (var serviceException in serviceExceptionReport.ServiceExceptions)
                {
                    Debug.LogErrorFormat("serviceException: code {0} locator {1} value {2}", serviceException.Code, serviceException.Locator, serviceException.Value);
                }
            }

            WalkLayers(cap.Layer, PrintLayer);
            var ec = cap.ExtendedCapabilities;
            PrintExtendedCaps(ref ec);
        }
        return wmsCapabilities != null;
    }

    private bool GetMap(string url, string version, string layer, int minLevel, int maxLevel, string imageType, out ITileSource tileSource)
    {
        if (string.IsNullOrEmpty(imageType))
        {
            tileSource = null;
            return false;
        }

        var schema = new GlobalSphericalMercator(imageType, YAxis.OSM, minLevel, maxLevel);
        schema.Validate();
        var request = new WmscRequest(new Uri(url), schema, 
                                        layers: new[] { layer }.ToList(), 
                                        styles: new[] { "" }.ToList(), 
                                        version: version);
        var provider = new HttpTileProvider(request);
        tileSource = new TileSource(provider, schema);
        return tileSource != null;
    }

    // TODO
    private bool GetFeatureInfo(string url)//, out FeatureInfo featureInfo)
    {
        return false;
    }

    #endregion // Queries

    #region Misc

    private Texture2D[] Process(ITileSource source, 
                                //IRequest request = null, 
                                string levelId = null)
    {
        int levelId_int = -1;
        if (!string.IsNullOrEmpty(levelId))
        {
            if (!int.TryParse(levelId, out levelId_int))
                Debug.LogErrorFormat("Could not parse levelId {0}", levelId);
        }
        IEnumerable<TileInfo> tileInfos = null;
        var extent = new BruTile.Extent(-20037508, -20037508, 20037508, 20037508);
        if (levelId_int != -1)
        {
            tileInfos = source.Schema.GetTileInfos(extent, levelId);
        }
        else
        {
            const int screenWidthInPixels = 400; // The width of the map on screen in pixels
            var unitsPerPixel = extent.Width / screenWidthInPixels;
            tileInfos = source.Schema.GetTileInfos(extent, unitsPerPixel);
        }

        var tiles = new Dictionary<BruTile.TileInfo, byte[]>();
        var tileUris = new Dictionary<BruTile.TileInfo, Uri>();
        int count = 0;
        foreach (var tileInfo in tileInfos)
        {
            try
            {
                tiles[tileInfo] = source.GetTile(tileInfo);
                //if (request != null)
                //    tileUris[tileInfo] = request.GetUri(tileInfo);
            }
            catch (HttpRequestException e)
            {
                Debug.LogException(e);
            }
            ++count;
        }
        var textureTest = new Texture2D[count];
        int i = 0;
        foreach (var kvp in tiles)
        {
            var tileInfo = kvp.Key;
            var bytes = kvp.Value;
            if (bytes == null)
                continue;

            int width = source.Schema.GetTileWidth(tileInfo.Index.Level);
            int height = source.Schema.GetTileHeight(tileInfo.Index.Level);
            if (source.Schema.Format.ToLower().EndsWith("png"))
                textureTest[i] = Load_PNG_or_JPG(width, height, bytes);
            else if (source.Schema.Format.ToLower().EndsWith("jpg") || source.Schema.Format.ToLower().EndsWith("jpeg"))
                textureTest[i] = Load_PNG_or_JPG(width, height, bytes);
            ++i;
        }

        if (VerboseLogging)
            PrintTiles(ref tiles);

        return textureTest;
    }

    private Texture2D Load_PNG_or_JPG(int Width, int Height, byte[] bytes)
    {
        Texture2D tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        tex.LoadImage(bytes);

        return tex;
    }

    private void PrintTiles(ref Dictionary<BruTile.TileInfo, byte[]> tiles)
    {
        foreach (var tile in tiles)
        {
            Debug.LogFormat("Column: {0}, Row: {1}, level: {2}, bytes: {3}", 
                tile.Key.Index.Col, tile.Key.Index.Row, tile.Key.Index.Level, tile.Value.Length);
        }
    }

    private void PrintExtendedCaps(ref Dictionary<XName, XNode> ec)
    {
        if (ec.Count == 0)
        {
            Debug.Log("No extended caps.");
        }
        else
        {
            string str = "Extended caps:";
            foreach (var kvp in ec)
            {
                var node = kvp.Value;
                while (node != null)
                {
                    var doc = XDocument.Parse(node.ToString());
                    foreach (var x in doc.Root.Elements())
                    {
                        str += string.Format("\n{0}: {1}", x.Name, x.Value);
                        if (x.HasElements)
                        {
                            foreach (var elem in x.Elements())
                                str += string.Format("\n\t[ELEM]{0}: {1}", elem.Name, elem.Value);
                        }
                        if (x.HasAttributes)
                        {
                            foreach (var attr in x.Attributes())
                                str += string.Format("\n\t[ATTR]{0}: {1}", attr.Name, attr.Value);
                        }
                    }
                    node = node.NextNode;
                }
            }
            Debug.Log(str);
        }
    }

    private static BruTile.ITileSource CreateGoogleTileSource(string urlFormatter, string name)
    {
        return new HttpTileSource(new GlobalSphericalMercator(), urlFormatter, new[] {"0", "1", "2", "3"}, name: name, 
            tileFetcher: FetchGoogleTile); 
    }

    private static byte[] FetchGoogleTile(Uri arg)
    {
        var httpClient = new System.Net.Http.HttpClient();

        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "http://maps.google.com/");
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", @"Mozilla / 5.0(Windows; U; Windows NT 6.0; en - US; rv: 1.9.1.7) Gecko / 20091221 Firefox / 3.5.7");
           
        return httpClient.GetByteArrayAsync(arg).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public Bounds ToBounds(BoundingBox bbox)
    {
        var bounds = new Bounds();
        Vector3 min = new Vector3((float)bbox.MinX, -1f, (float)bbox.MinY);
        Vector3 max = new Vector3((float)bbox.MaxX, +1f, (float)bbox.MaxY);
        bounds.center = (min + max) / 2;
        bounds.Encapsulate(min);
        bounds.Encapsulate(max);
        return bounds;
    }

    public Bounds ToBounds(List<BoundingBox> bboxList, string crs)
    {
        var bounds = new Bounds();
        foreach (var bbox in bboxList)
        {
            if (bbox.CRS == crs)
            {
                bounds = ToBounds(bbox);

                // per the spec, there's no more than one bounding box for each supported CRS 
                // (meaning if there were additional, behavior is undefined)
                break;
            }
        }
        return bounds;
    }

    //private List<string> GetFormats(Request request, string typeStr = null)
    //{
    //    List<string> formats = new List<string>();
    //    foreach (string format in request.GetCapabilities.Format)
    //    {
    //        if (string.IsNullOrEmpty(typeStr) || format.StartsWith(typeStr))
    //            formats.Add(format);
    //    }
    //    return formats;
    //}

    private void PrintFormats(ref List<string> formats, bool imagesOnly = false)
    {
        //var formats = GetFormats(request, imagesOnly ? "images/" : null);
        if (formats.Count == 0)
        {
            Debug.Log("No formats.");
        }
        else
        {
            string str = "Formats:";
            foreach (string format in formats)
                str += string.Format("\nformat: {0}", format);
            Debug.Log(str);
        }
    }

    #region Layers

    public delegate void WalkDelegate(Layer layer);
    private int layerDepth = 0;

    private void PrintLayer(Layer layer)
    {
        string str = "";
        //for (int i = 0; i < layerDepth; ++i)
        //    str += "\t";
        str += string.Format("Layer: [Name: {0}], [Title: {1}], [Abstract: {2}]", string.IsNullOrEmpty(layer.Name) ? "<none>" : layer.Name, string.IsNullOrEmpty(layer.Title) ? "<none>" : layer.Title, string.IsNullOrEmpty(layer.Abstract) ? "<none>" : layer.Abstract);

        if (layer.CRS.Count != 0)
        {
            str += "\nCRS support:";
            layer.CRS.ForEach((x) => {str += "\n" + x;} );
        }
        else
        {
            str += "\nNo keywords.";
        }

        if (layer.KeywordList.Keyword.Count != 0)
        {
            str += "\nKeywords:";
            foreach (var keyword in layer.KeywordList.Keyword)
                str += string.Format("\n\t[Vocabulary: {0}], [Value: {1}]", string.IsNullOrEmpty(keyword.Vocabulary) ? "<none>" : keyword.Vocabulary, string.IsNullOrEmpty(keyword.Value) ? "<none>" : keyword.Value);
        }
        else
        {
            str += "\nNo keywords.";
        }

        if (layer.BoundingBox.Count != 0)
        {
            str += "\nBounding boxes:";
            foreach (var bbox in layer.BoundingBox)
            {
                //Bounds bounds = ToBounds(bbox);
                str += string.Format("\nCRS: {0}, MinX: {1}, MinY: {2}, MaxX: {3}, MaxY: {4}, ResX: {5}, ResY: {6}", bbox.CRS, bbox.MinX, bbox.MinY, bbox.MaxX, bbox.MaxY, bbox.ResX.HasValue ? bbox.ResX.ToString() : "<none>", bbox.ResY.HasValue ? bbox.ResY.ToString() : "<none>");
            }
        }
        else
        {
            str += "\nNo bounding boxes.";
        }

        Debug.Log(str);
    }

    private void WalkLayers(Layer layer, WalkDelegate walkDelegate)
    {
        walkDelegate(layer);
        ++layerDepth;
        foreach (var child in layer.ChildLayers)
            WalkLayers(child, walkDelegate);
        --layerDepth;
    }

    #endregion // Layers

    #endregion // Misc
}

            /*var doc = XDocument.Parse(node.ToString());
            foreach (var x in doc.Root.Elements())
                string serverTitle = rootNode.SelectSingleNode("Service").SelectSingleNode("Title").InnerText;
            string serverAbstract = null;
            if (rootNode.SelectSingleNode("Service").SelectSingleNode("Abstract") != null)
            {
                serverAbstract = rootNode.SelectSingleNode("Service").SelectSingleNode("Abstract").InnerText;
            };*/

#endif
