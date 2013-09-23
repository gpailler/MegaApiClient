using System;
using System.IO;

namespace CG.Web.MegaApiClient
{
    public interface IWebClient
    {
        string PostRequestJson(Uri url, string jsonData);

        string PostRequestRaw(Uri url, Stream dataStream);

        Stream GetRequestRaw(Uri url);
    }
}
