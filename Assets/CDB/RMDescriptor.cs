
namespace Cognitics.CDB
{
    public class RMDescriptor : TiledDataset
    {

        public override int Code => 6;
        public override string Name => "RMDescriptor";

        internal RMDescriptor(Database db) : base(db)
        {
        }

    }

}
