
namespace Cognitics.CDB
{
    public class GTModelGeometry : GTModelDataset
    {

        public override int Code => 500;
        public override string Name => "GTModelGeometry";

        public string Subdirectory(string facc)
        {
            return string.Format("GTModel/{0:000}_{1}/{2}", Code, Name, Database.Metadata.FeatureDataDictionary.Subdirectory(facc));
        }


        public readonly GTModelGeometryEntryFile GeometryEntryFile;

        internal GTModelGeometry(Database db) : base(db)
        {
            GeometryEntryFile = new GTModelGeometryEntryFile(this);
        }

    }

    public class GTModelGeometryEntryFile : Component
    {
        public override int Selector1 => 1;
        public override int Selector2 => 1;
        public override string Name => "Geometry Entry File";
        public override string Extension => ".flt";

        public string Filename(string facc, int fsc, string modl)
        {
            return string.Format("D{0:000}_S{1:000}_T{2:000}_{3}_{4:000}_{5}", Dataset.Code, Selector1, Selector2, facc, fsc, modl);
        }

        internal GTModelGeometryEntryFile(Dataset dataset) : base(dataset) { }



    }

}
