
namespace Cognitics.Unity
{
    public class FileReadJob : GCJob
    {
        public string Filename;
        public byte[] FileBytes = null;

        public override void Execute()
        {
            string dirname = System.IO.Path.GetDirectoryName(Filename);
            if (System.IO.Path.GetExtension(dirname) == ".zip")
            {
                string entryname = System.IO.Path.GetFileName(Filename);
                if (!lzip.entryExists(dirname, entryname))
                    return;
                lzip.entry2Buffer(dirname, entryname, ref FileBytes);
                return;
            }
            FileBytes = System.IO.File.ReadAllBytes(Filename);
        }
    }

}
