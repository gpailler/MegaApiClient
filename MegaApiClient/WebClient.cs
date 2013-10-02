#region License

/*
The MIT License (MIT)

Copyright (c) 2013 Gregoire Pailler

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

#endregion

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
