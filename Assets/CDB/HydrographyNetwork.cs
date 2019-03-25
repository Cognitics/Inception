
namespace Cognitics.CDB
{
    public class HydrographyNetwork : TiledDataset
    {

        public override int Code => 204;
        public override string Name => "HydrographyNetwork";

        public readonly HydrographyNetworkConnections Connections;
        public readonly HydrographyNetworkHydrography Hydrography;

        internal HydrographyNetwork(Database db) : base(db)
        {
            Connections = new HydrographyNetworkConnections(this);
            Hydrography = new HydrographyNetworkHydrography(this);
        }

        public class HydrographyNetworkConnections : VectorComponent
        {
            public override int Selector1 => 1;
            public override string Name => Dataset.Name + " Connections";
            internal HydrographyNetworkConnections(Dataset dataset) : base(dataset) { }
        }

        public class HydrographyNetworkHydrography : VectorComponent
        {
            public override int Selector1 => 2;
            public override string Name => Dataset.Name + " Hydrography";
            internal HydrographyNetworkHydrography(Dataset dataset) : base(dataset) { }
        }

    }

}
