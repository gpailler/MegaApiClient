#region License

/*
The MIT License (MIT)

Copyright (c) 2015 Gregoire Pailler

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
using System.Threading;

namespace CG.Web.MegaApiClient
{
    public class WebClient : IWebClient
    {
        private const int DefaultResponseTimeout = Timeout.Infinite;
        private const uint BufferSize = 8192;

        private readonly int _responseTimeout;
        private readonly string _userAgent;

        public WebClient()
            : this(DefaultResponseTimeout)
        {
        }

        internal WebClient(int responseTimeout)
        {
            this._responseTimeout = responseTimeout;
            this._userAgent = this.GenerateUserAgent();
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
            HttpWebRequest request = this.CreateRequest(url);
            request.Method = "GET";

            return request.GetResponse().GetResponseStream();
        }

        private string PostRequest(Uri url, Stream dataStream, string contentType)
        {
            HttpWebRequest request = this.CreateRequest(url);
            request.ContentLength = dataStream.Length;
            request.Method = "POST";
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

        private HttpWebRequest CreateRequest(Uri url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = this._responseTimeout;
            request.UserAgent = this._userAgent;

            return request;
        }

        private string GenerateUserAgent()
        {
            AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
            return string.Format("{0} v{1}", assemblyName.Name, assemblyName.Version.ToString(2));
        }
    }
}
