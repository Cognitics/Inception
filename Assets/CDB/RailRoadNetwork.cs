
namespace Cognitics.CDB
{
    public class RailRoadNetwork : TiledDataset
    {

        public override int Code => 202;
        public override string Name => "RailRoadNetwork";

        public readonly RailRoadNetworkConnections Connections;
        public readonly RailRoadNetworkRailRoads RailRoads;

        internal RailRoadNetwork(Database db) : base(db)
        {
            Connections = new RailRoadNetworkConnections(this);
            RailRoads = new RailRoadNetworkRailRoads(this);
        }

        public class RailRoadNetworkConnections : VectorComponent
        {
            public override int Selector1 => 1;
            public override string Name => Dataset.Name + " Connections";
            internal RailRoadNetworkConnections(Dataset dataset) : base(dataset) { }
        }

        public class RailRoadNetworkRailRoads : VectorComponent
        {
            public override int Selector1 => 2;
            public override string Name => Dataset.Name + " RailRoads";
            internal RailRoadNetworkRailRoads(Dataset dataset) : base(dataset) { }
        }


    }

}
