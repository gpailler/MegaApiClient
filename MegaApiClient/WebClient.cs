using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;

namespace CG.Web.MegaApiClient
{
    public class WebClient : IWebClient
    {
        private static readonly string UserAgent;
        private const uint BufferSize = 8192;

        static WebClient()
        {
            AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
            UserAgent = string.Format("{0} v{1}", assemblyName.Name, assemblyName.Version.ToString(2));
        }

        public string PostRequestJson(Uri url, string jsonData)
        {
            using (MemoryStream jsonStream = new MemoryStream(jsonData.ToBytes()))
            {
                return this.PostRequest(url, jsonStream, "application/json");
            }
        }
        
        public string PostRequestRaw(Uri url, Stream dataStream)
        {
            return this.PostRequest(url, dataStream, "application/octet-stream");
        }

        public Stream GetRequestRaw(Uri url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = -1;
            request.UserAgent = UserAgent;

            return request.GetResponse().GetResponseStream();
        }

        private string PostRequest(Uri url, Stream dataStream, string contentType)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.ContentLength = dataStream.Length;
            request.Method = "POST";
            request.Timeout = -1;
            request.UserAgent = UserAgent;
            request.ContentType = contentType;

            using (Stream requestStream = request.GetRequestStream())
            {
                dataStream.Position = 0;

                int length = (int)Math.Min(BufferSize, dataStream.Length);
                byte[] buffer = new byte[length];
                int bytesRead;

                while ((bytesRead = dataStream.Read(buffer, 0, length)) > 0)
                {
                    requestStream.Write(buffer, 0, bytesRead);
                }
            }

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            {
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader streamReader = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        return streamReader.ReadToEnd();
                    }
                }
            }
        }
    }
}
