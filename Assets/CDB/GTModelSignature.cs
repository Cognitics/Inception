
namespace Cognitics.CDB
{
    public class GTModelSignature : GTModelDataset
    {

        public override int Code => 502;
        public override string Name => "GTModelSignature";

        internal GTModelSignature(Database db) : base(db)
        {
        }

    }

}
