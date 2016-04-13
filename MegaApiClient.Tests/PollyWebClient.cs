using System;
using System.IO;
using System.Net;
using Polly;

namespace CG.Web.MegaApiClient.Tests
{
    internal class PollyWebClient : IWebClient
    {
        private readonly IWebClient _webClient;
        private readonly Policy _policy;

        public PollyWebClient(IWebClient webClient, int maxRetry)
        {
            this._webClient = webClient;
            this._policy = Policy
                .Handle<WebException>(ex => ex.Status == WebExceptionStatus.Timeout)
                .WaitAndRetry(maxRetry, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, ts) => Console.WriteLine(ex.Message));
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
