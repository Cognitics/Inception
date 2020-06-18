
namespace Cognitics.CDB
{
    public class GSModelDescriptor : TiledDataset
    {

        public override int Code => 303;
        public override string Name => "GSModelDescriptor";

        internal GSModelDescriptor(Database db) : base(db)
        {
        }

    }

}
