
namespace Cognitics.CDB
{
    public class VectorCompositeMaterial : TiledDataset
    {
        public override int Code => 200;
        public override string Name => "VectorCompositeMaterial";

        internal VectorCompositeMaterial(Database db) : base(db)
        {
            // TODO: this is xml referenced by the CMIX attribute of vector datasets
        }

    }

}
