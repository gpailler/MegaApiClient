using System;
using System.IO;
using System.Net;
using Polly;

namespace CG.Web.MegaApiClient.Tests
{
    class PollyWebClient : IWebClient
    {
        private const int Timeout = 60000;

        private readonly WebClient _webClient;
        private readonly Policy _policy;

        public PollyWebClient()
        {
            this._webClient = new WebClient(Timeout);

            this._policy = Policy
                .Handle<WebException>()
                .WaitAndRetry(1, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                );
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
