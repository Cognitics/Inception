
namespace Cognitics.CDB
{
    public abstract class TiledDataset : Dataset
    {
        public string Filename => string.Format("D{0:000}", Code);
        public string Subdirectory => string.Format("{0:000}_{1}", Code, Name);
        protected TiledDataset(Database database) : base(database) { }
    }

}