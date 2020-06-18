
using System.Net;
using System.Text;

namespace Cognitics
{
    public static class Web
    {
        public static byte[] BytesFromWebRequest(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Accept = "application/*";
            using (WebResponse response = request.GetResponse())
            {
                using (System.IO.Stream stream = response.GetResponseStream())
                {
                    using (var content = new System.IO.MemoryStream())
                    {
                        stream.CopyTo(content);
                        return content.ToArray();
                    }
                }
            }
        }

        public static string UTF8StringFromWebRequest(string url) => Encoding.UTF8.GetString(BytesFromWebRequest(url));

    }

}

