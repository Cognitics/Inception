
namespace Cognitics.CDB
{
    public class GSModelGeometry : TiledDataset
    {

        public override int Code => 300;
        public override string Name => "GSModelGeometry";

        public readonly GSModelGeometryArchive GeometryArchive;


        internal GSModelGeometry(Database db) : base(db)
        {
            GeometryArchive = new GSModelGeometryArchive(this);
        }

    }

    public class GSModelGeometryArchive : Component
    {
        public override int Selector1 => 1;
        public override int Selector2 => 1;
        public override string Name => "Geometry Archive";
        public override string Extension => ".zip";

        public string Filename(Tile tile, string facc, int fsc, string modl)
        {
            return string.Format("{0}_{1}_{2:000}_{3}", tile.Filename(this), facc, fsc, modl);
        }

        internal GSModelGeometryArchive(Dataset dataset) : base(dataset) { }

    }

}
