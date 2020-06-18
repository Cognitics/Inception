
namespace Cognitics.CDB
{
    public class HumanGeography : TiledDataset
    {
        public override int Code => 947;    // TODO
        public override string Name => "HumanGeography";

        public readonly HumanGeographyReligiousInstitutions ReligiousInstitutions;

        internal HumanGeography(Database db) : base(db)
        {
            ReligiousInstitutions = new HumanGeographyReligiousInstitutions(this);
        }

    }

    public class HumanGeographyReligiousInstitutions : VectorComponent
    {
        public override int Selector1 => 1;
        public override string Name => Dataset.Name + " Religious Institutions";
        internal HumanGeographyReligiousInstitutions(Dataset dataset) : base(dataset) { }
    }


}
