﻿namespace CG.Web.MegaApiClient
{
  using System;
  using System.IO;

  public interface IWebClient
  {
    int BufferSize { get; set; }

    string PostRequestJson(Uri url, string jsonData);

    string PostRequestRaw(Uri url, Stream dataStream);

    Stream GetRequestRaw(Uri url);

    long GetLength(Uri url);

    Stream GetRequestRawWithRange(Uri url, long startByte, long endByte);
  }
}
