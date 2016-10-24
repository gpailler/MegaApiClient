using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Polly;

namespace CG.Web.MegaApiClient.Tests
{
    internal class TestWebClient : IWebClient
    {
        private readonly IWebClient _webClient;
        private readonly Policy _policy;

        public TestWebClient(IWebClient webClient, int maxRetry)
        {
            this._webClient = webClient;
            this._policy = Policy
                .Handle<WebException>()
                .Or<SocketException>()
                .WaitAndRetry(maxRetry, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, ts) => Console.WriteLine(ts.TotalSeconds + " " + ex.Message));
        }

        public string PostRequestJson(Uri url, string jsonData)
        {
            return this._policy.Execute(() => this._webClient.PostRequestJson(url, jsonData));
        }

        public string PostRequestRaw(Uri url, Stream dataStream)
        {
            return this._policy.Execute(() => this._webClient.PostRequestRaw(url, dataStream));
        }

        public Stream GetRequestRaw(Uri url)
        {
            return this._policy.Execute(() => this._webClient.GetRequestRaw(url));
        }
  }
}
