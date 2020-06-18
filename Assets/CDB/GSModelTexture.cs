
namespace Cognitics.CDB
{
    public class GSModelTexture : TiledDataset
    {
        public override int Code => 301;
        public override string Name => "GSModelTexture";

        public readonly GSModelTextureArchive TextureArchive;

        internal GSModelTexture(Database db) : base(db)
        {
            TextureArchive = new GSModelTextureArchive(this);
        }

    }

    public class GSModelTextureArchive : Component
    {
        public override int Selector1 => 1;
        public override int Selector2 => 1;
        public override string Name => "Texture Archive";
        public override string Extension => ".zip";

        internal GSModelTextureArchive(Dataset dataset) : base(dataset) { }

    }

}
