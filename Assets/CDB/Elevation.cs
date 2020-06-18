
namespace Cognitics.CDB
{
    public class Elevation : TiledDataset
    {
        public override int Code => 1;
        public override string Name => "Elevation";

        public readonly PrimaryTerrainElevation PrimaryTerrainElevation;

        internal Elevation(Database database) : base(database)
        {
            PrimaryTerrainElevation = new PrimaryTerrainElevation(this);
        }

    }
}
