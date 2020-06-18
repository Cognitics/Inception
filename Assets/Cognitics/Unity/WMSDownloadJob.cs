
using System.IO;
using System.Net;
using UnityEngine;

namespace Cognitics.Unity
{
    public class WMSDownloadJob : GCJob
    {
        public string OnlineImageryServer;
        public string OnlineImageryLayer;
        public int Width;
        public int Height;
        public double South;
        public double North;
        public double West;
        public double East;
        public string Filename;

        public override void Execute()
        {
            string uri = string.Format("{0}?SERVICE=WMS&VERSION=1.1.1&REQUEST=GetMap&SRS=EPSG:4326&Format=image/tiff&LAYERS={1}{2}{3}",
                OnlineImageryServer,
                WebUtility.UrlEncode(OnlineImageryLayer),
                string.Format("&WIDTH={0}&HEIGHT={1}", Width, Height),
                string.Format("&BBOX={0},{1},{2},{3}", West, South, East, North));
            var web = (HttpWebRequest)WebRequest.Create(uri);
            var response = (HttpWebResponse)web.GetResponse();
            if (response.ContentType == "image/tiff")
            {
                Debug.Log("WMS: " + uri);
                string filepath = Path.GetDirectoryName(Filename);
                if (!Directory.Exists(filepath))
                    Directory.CreateDirectory(filepath);
                using (var s = response.GetResponseStream())
                {
                    using (var m = new MemoryStream())
                    {
                        s.CopyTo(m);
                        if (m.Length < 1024 * 1024 * 3 * 2)
                            File.WriteAllBytes(Filename, m.ToArray());
                    }
                }
            }
            else
            {
                Debug.Log("FAILED: " + uri);
                using (var reader = new StreamReader(response.GetResponseStream()))
                    Debug.Log(reader.ReadToEnd());
            }
        }
    }

}
