
namespace Cognitics.CDB
{
    public class GSFeature : TiledDataset
    {
        public override int Code => 100;
        public override string Name => "GSFeature";

        public readonly GSFeatureManMade ManMade;
        public readonly GSFeatureNatural Natural;
        public readonly GSFeatureTrees Trees;
        public readonly GSFeatureAirports Airports;
        public readonly GSFeatureEnvironmental Environmental;

        internal GSFeature(Database db) : base(db)
        {
            ManMade = new GSFeatureManMade(this);
            Natural = new GSFeatureNatural(this);
            Trees = new GSFeatureTrees(this);
            Airports = new GSFeatureAirports(this);
            Environmental = new GSFeatureEnvironmental(this);
        }

    }

    public class GSFeatureManMade : VectorComponent
    {
        public override int Selector1 => 1;
        public override string Name => Dataset.Name + " Man-made";
        internal GSFeatureManMade(Dataset dataset) : base(dataset) { }
    }

    public class GSFeatureNatural : VectorComponent
    {
        public override int Selector1 => 1;
        public override string Name => Dataset.Name + " Natural";
        internal GSFeatureNatural(Dataset dataset) : base(dataset) { }
    }

    public class GSFeatureTrees : VectorComponent
    {
        public override int Selector1 => 1;
        public override string Name => Dataset.Name + " Trees";
        internal GSFeatureTrees(Dataset dataset) : base(dataset) { }
    }

    public class GSFeatureAirports : VectorComponent
    {
        public override int Selector1 => 1;
        public override string Name => Dataset.Name + " Airport";
        internal GSFeatureAirports(Dataset dataset) : base(dataset) { }
    }

    public class GSFeatureEnvironmental : VectorComponent
    {
        public override int Selector1 => 1;
        public override string Name => Dataset.Name + " Environmental";
        internal GSFeatureEnvironmental(Dataset dataset) : base(dataset) { }
    }


}
