
namespace Cognitics.CDB
{
    public abstract class Dataset
    {
        public static bool operator ==(Dataset a, Dataset b) => (a.Database == b.Database) && (a.Code == b.Code);
        public static bool operator !=(Dataset a, Dataset b) => !(a == b);

        public override int GetHashCode() => Code;
        public override bool Equals(object obj) => (obj is Dataset) && (this == (Dataset)obj);

        public readonly Database Database;
        protected Dataset(Database Database) { this.Database = Database; }

        public virtual int Code { get; }
        public virtual string Name { get; }
    }

}