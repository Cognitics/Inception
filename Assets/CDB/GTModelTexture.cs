
namespace Cognitics.CDB
{
    public class GTModelTexture : GTModelDataset
    {
        public override int Code => 501;
        public override string Name => "GTModelTexture";

        public readonly GTModelYearRoundTexture YearRoundTexture;

        internal GTModelTexture(Database db) : base(db)
        {
            YearRoundTexture = new GTModelYearRoundTexture(this);
        }

    }

    public class GTModelYearRoundTexture : Component
    {
        public override int Selector1 => 1;
        public override int Selector2 => 1;
        public override string Name => "Year-Round Texture";
        public override string Extension => ".rgb";
        internal GTModelYearRoundTexture(Dataset dataset) : base(dataset) { }

        public float[] Read(string facc, int fsc, string modl)
        {
            //string filename = System.IO.Path.Combine(Dataset.Database.Path, ((GTModelTexture)Dataset).Subdirectory(facc), Filename(facc, fsc, modl)) + Extension;
            //return SiliconGraphicsImage.Read(filename);
            return null;
        }


    }



}
