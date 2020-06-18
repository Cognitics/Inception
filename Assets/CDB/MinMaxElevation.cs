
namespace Cognitics.CDB
{
    public class MinMaxElevation : TiledDataset
    {

        public override int Code => 2;
        public override string Name => "MinMaxElevation";

        internal MinMaxElevation(Database db) : base(db)
        {
        }

    }

}
