
namespace Cognitics.CDB
{
    public class GeoPolitical : TiledDataset
    {
        public override int Code => 102;
        public override string Name => "GeoPolitical";

        public readonly GeoPoliticalBoundary Boundary;
        public readonly GeoPoliticalLocation Location;
        public readonly GeoPoliticalConstraint Constraint;

        internal GeoPolitical(Database db) : base(db)
        {
            Boundary = new GeoPoliticalBoundary(this);
            Location = new GeoPoliticalLocation(this);
            Constraint = new GeoPoliticalConstraint(this);
        }

    }

    public class GeoPoliticalBoundary : VectorComponent
    {
        public override int Selector1 => 1;
        public override string Name => Dataset.Name + " Boundary";
        internal GeoPoliticalBoundary(Dataset dataset) : base(dataset) { }
    }

    public class GeoPoliticalLocation : VectorComponent
    {
        public override int Selector1 => 2;
        public override string Name => Dataset.Name + " Location";
        internal GeoPoliticalLocation(Dataset dataset) : base(dataset) { }
    }

    public class GeoPoliticalConstraint : VectorComponent
    {
        public override int Selector1 => 3;
        public override string Name => Dataset.Name + " Constraint";
        internal GeoPoliticalConstraint(Dataset dataset) : base(dataset) { }
    }

}
