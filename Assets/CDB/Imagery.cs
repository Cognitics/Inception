
namespace Cognitics.CDB
{
    public class Imagery : TiledDataset
    {
        public override int Code => 4;
        public override string Name => "Imagery";

        public readonly YearlyVstiRepresentation YearlyVstiRepresentation;

        internal Imagery(Database database) : base(database)
        {
            YearlyVstiRepresentation = new YearlyVstiRepresentation(this);
        }

    }
}
