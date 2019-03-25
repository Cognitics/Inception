
namespace Cognitics.CDB
{
    public class GTFeature : TiledDataset
    {
        public override int Code => 101;
        public override string Name => "GTFeature";

        public readonly GTFeatureManMade ManMade;
        public readonly GTFeatureTrees Trees;
        public readonly GTFeatureMovingModels MovingModels;

        internal GTFeature(Database db) : base(db)
        {
            ManMade = new GTFeatureManMade(this);
            Trees = new GTFeatureTrees(this);
            MovingModels = new GTFeatureMovingModels(this);
        }

        public class GTFeatureManMade : VectorComponent
        {
            public override int Selector1 => 1;
            public override string Name => Dataset.Name + " Man-made";
            internal GTFeatureManMade(Dataset dataset) : base(dataset) { }
        }

        public class GTFeatureTrees : VectorComponent
        {
            public override int Selector1 => 2;
            public override string Name => Dataset.Name + " Trees";
            internal GTFeatureTrees(Dataset dataset) : base(dataset) { }
        }

        public class GTFeatureMovingModels : VectorComponent
        {
            public override int Selector1 => 3;
            public override string Name => Dataset.Name + " Moving Models";
            internal GTFeatureMovingModels(Dataset dataset) : base(dataset) { }
        }

    }

}
