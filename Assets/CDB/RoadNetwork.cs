
namespace Cognitics.CDB
{
    public class RoadNetwork : TiledDataset
    {
        public override int Code => 201;
        public override string Name => "RoadNetwork";

        public readonly RoadNetworkConnections Connections;
        public readonly RoadNetworkRoads Roads;
        public readonly RoadNetworkAirports Airports;

        internal RoadNetwork(Database db) : base(db)
        {
            Connections = new RoadNetworkConnections(this);
            Roads = new RoadNetworkRoads(this);
            Airports = new RoadNetworkAirports(this);
        }

    }

    public class RoadNetworkConnections : VectorComponent
    {
        public override int Selector1 => 1;
        public override string Name => Dataset.Name + " Connections";
        internal RoadNetworkConnections(Dataset dataset) : base(dataset) { }
    }

    public class RoadNetworkRoads : VectorComponent
    {
        public override int Selector1 => 2;
        public override string Name => Dataset.Name + " Roads";
        internal RoadNetworkRoads(Dataset dataset) : base(dataset) { }
    }

    public class RoadNetworkAirports : VectorComponent
    {
        public override int Selector1 => 3;
        public override string Name => Dataset.Name + " Airports";
        internal RoadNetworkAirports(Dataset dataset) : base(dataset) { }
    }



}
