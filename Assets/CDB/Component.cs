
namespace Cognitics.CDB
{
    public abstract class Component
    {
        public static bool operator ==(Component a, Component b)
        {
            return (a.Dataset == b.Dataset) && (a.Selector1 == b.Selector1) && (a.Selector2 == b.Selector2);
        }
        public static bool operator !=(Component a, Component b) => !(a == b);

        public override int GetHashCode() => System.Tuple.Create(Dataset, Selector1, Selector2).GetHashCode();
        public override bool Equals(object obj) => (obj is Component) && (this == (Component)obj);

        public readonly Dataset Dataset;

        protected Component(Dataset dataset) { Dataset = dataset; }

        public virtual int Selector1 { get; }
        public virtual int Selector2 { get; }
        public virtual string Name { get; }
        public virtual string Extension { get; }

        public virtual string Filename(Tile tile)
        {
            return System.IO.Path.Combine(Dataset.Database.Path, tile.Path((TiledDataset)Dataset), tile.Filename(this) + Extension);
        }
        public virtual bool Exists(Tile tile) => System.IO.File.Exists(Filename(tile));

    }

}
