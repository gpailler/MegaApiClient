using System;
using System.IO;

namespace CG.Web.MegaApiClient
{
    public interface IWebClient
    {
        string SendPostRequestJson(Uri url, string jsonData);

        string SendPostRequestRaw(Uri url, Stream dataStream);
    }
}
