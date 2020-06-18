
namespace Cognitics.CDB
{
    public class PowerLineNetwork : TiledDataset
    {
        public override int Code => 203;
        public override string Name => "PowerLineNetwork";

        public readonly PowerLineNetworkConnections Connections;
        public readonly PowerLineNetworkPowerLines PowerLines;

        internal PowerLineNetwork(Database db) : base(db)
        {
            Connections = new PowerLineNetworkConnections(this);
            PowerLines = new PowerLineNetworkPowerLines(this);
        }

        public class PowerLineNetworkConnections : VectorComponent
        {
            public override int Selector1 => 1;
            public override string Name => Dataset.Name + " Connections";
            internal PowerLineNetworkConnections(Dataset dataset) : base(dataset) { }
        }

        public class PowerLineNetworkPowerLines : VectorComponent
        {
            public override int Selector1 => 2;
            public override string Name => Dataset.Name + " PowerLines";
            internal PowerLineNetworkPowerLines(Dataset dataset) : base(dataset) { }
        }
    }

}
