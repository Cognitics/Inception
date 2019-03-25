
namespace Cognitics.CDB
{
    public class GTModelDescriptor : GTModelDataset
    {

        public override int Code => 503;
        public override string Name => "GTModelDescriptor";

        internal GTModelDescriptor(Database db) : base(db)
        {
        }

    }

}
