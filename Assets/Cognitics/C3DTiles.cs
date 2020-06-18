using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// https://github.com/CesiumGS/3d-tiles/tree/master/specification/schema

namespace Cognitics.C3DTiles
{
    // https://github.com/CesiumGS/3d-tiles/blob/master/specification/schema/tileset.schema.json
    [Serializable]
    public class TileSet
    {
        public Asset asset;
        public JObject properties;
        public double geometricError;
        public Tile root;
        public string[] extensionsUsed;
        public string[] extensionsRequired;
        public JObject extensions;
        public JObject extras;
        public static TileSet Parse(string json) => JsonConvert.DeserializeObject<TileSet>(json);
        public static TileSet Fetch(string url) => Parse(Web.UTF8StringFromWebRequest(url));
    }

    // https://github.com/CesiumGS/3d-tiles/blob/master/specification/schema/asset.schema.json
    [Serializable]
    public class Asset
    {
        public string version;
        public string tilesetVersion;
        public JObject extensions;
        public JObject extras;
    }

    // https://github.com/CesiumGS/3d-tiles/blob/master/specification/schema/tile.schema.json
    [Serializable]
    public class Tile
    {
        public BoundingVolume boundingVolume;
        public BoundingVolume viewerRequestVolume;
        public double geometricError;
        public string refine;
        public double[] transform;
        public TileContent content;
        public Tile[] children;
        public JObject extensions;
        public JObject extras;
    }

    // https://github.com/CesiumGS/3d-tiles/blob/master/specification/schema/boundingVolume.schema.json
    [Serializable]
    public class BoundingVolume
    {
        public double[] box;
        public double[] region;
        public double[] sphere;
        public JObject extensions;
        public JObject extras;
    }

    // https://github.com/CesiumGS/3d-tiles/blob/master/specification/schema/tile.content.schema.json
    [Serializable]
    public class TileContent
    {
        public BoundingVolume boundingVolume;
        public string uri;
        public string url;
        public JObject extensions;
        public JObject extras;
    }

    // this is for OGC 3DT
    [Serializable]
    public class FeatureTable
    {
        public int BATCH_LENGTH;
        public double[] RTC_CENTER;
        public static FeatureTable Parse(string json) => JsonConvert.DeserializeObject<FeatureTable>(json);
        public static FeatureTable Fetch(string url) => Parse(Web.UTF8StringFromWebRequest(url));
    }


}
