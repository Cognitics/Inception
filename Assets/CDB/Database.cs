
using System.Collections.Generic;
using Cognitics.CoordinateSystems;

namespace Cognitics.CDB
{
    public class Database
    {
        public readonly string Path;

        public readonly Tiles Tiles;

        public readonly Elevation Elevation;
        public readonly MinMaxElevation MinMaxElevation;
        public readonly MaxCulture MaxCulture;
        public readonly Imagery Imagery;
        public readonly RMTexture RMTexture;
        public readonly RMDescriptor RMDescriptor;
        public readonly GSFeature GSFeature;
        public readonly GTFeature GTFeature;
        public readonly GeoPolitical GeoPolitical;
        public readonly VectorCompositeMaterial VectorCompositeMaterial;
        public readonly RoadNetwork RoadNetwork;
        public readonly RailRoadNetwork RailRoadNetwork;
        public readonly PowerLineNetwork PowerLineNetwork;
        public readonly HydrographyNetwork HydrographyNetwork;
        public readonly GSModelGeometry GSModelGeometry;
        public readonly GSModelTexture GSModelTexture;
        public readonly GSModelSignature GSModelSignature;
        public readonly GSModelDescriptor GSModelDescriptor;
        public readonly GTModelGeometry GTModelGeometry;
        public readonly GTModelTexture GTModelTexture;
        public readonly GTModelSignature GTModelSignature;
        public readonly GTModelDescriptor GTModelDescriptor;
        public readonly Metadata Metadata;

        public Database(string path)
        {
            Path = path;
            Metadata = new Metadata(this);
            Tiles = new Tiles(this);
            Elevation = new Elevation(this);
            MinMaxElevation = new MinMaxElevation(this);
            MaxCulture = new MaxCulture(this);
            Imagery = new Imagery(this);
            RMTexture = new RMTexture(this);
            RMDescriptor = new RMDescriptor(this);
            GSFeature = new GSFeature(this);
            GTFeature = new GTFeature(this);
            GeoPolitical = new GeoPolitical(this);
            VectorCompositeMaterial = new VectorCompositeMaterial(this);
            RoadNetwork = new RoadNetwork(this);
            RailRoadNetwork = new RailRoadNetwork(this);
            PowerLineNetwork = new PowerLineNetwork(this);
            HydrographyNetwork = new HydrographyNetwork(this);
            GSModelGeometry = new GSModelGeometry(this);
            GSModelTexture = new GSModelTexture(this);
            GSModelSignature = new GSModelSignature(this);
            GSModelDescriptor = new GSModelDescriptor(this);
            GTModelGeometry = new GTModelGeometry(this);
            GTModelTexture = new GTModelTexture(this);
            GTModelSignature = new GTModelSignature(this);
            GTModelDescriptor = new GTModelDescriptor(this);
        }

        public string Name => System.IO.Path.GetFileName(Path.TrimEnd(System.IO.Path.DirectorySeparatorChar));
        public bool Exists => System.IO.Directory.Exists(Path);

        public List<GeographicCoordinates> ExistingGeocells() => Tiles.ExistingGeocells();
        public GeographicBounds ExistingBounds() => Tiles.ExistingBounds();



    }

}
