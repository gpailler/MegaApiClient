namespace CG.Web.MegaApiClient
{
  using System;

  public class Options
  {
    public const string DefaultApplicationKey = "axhQiYyQ";
    public const bool DefaultSynchronizeApiRequests = true;

    public const int DefaultApiRequestAttempts = 60;
    public const int DefaultApiRequestDelay = 500;
    public const float DefaultApiRequestDelayExponentialFactor = 1.5f;

    public const int DefaultBufferSize = 1024 * 64;
    public const int DefaultChunksPackSize = 1024 * 1024;

#if ASYNC
    public const long DefaultReportProgressChunkSize = DefaultBufferSize;
#endif

    public Options(
      string applicationKey = DefaultApplicationKey,
      bool synchronizeApiRequests = DefaultSynchronizeApiRequests,
      int apiRequestAttempts = DefaultApiRequestAttempts,
      int apiRequestDelay = DefaultApiRequestDelay,
      float apiRequestDelayExponentialFactor = DefaultApiRequestDelayExponentialFactor,
      int bufferSize = DefaultBufferSize,
      int chunksPackSize = DefaultChunksPackSize
#if ASYNC
      ,
      long reportProgressChunkSize = DefaultReportProgressChunkSize
#endif
      )
    {
      this.ApplicationKey = applicationKey;
      this.SynchronizeApiRequests = synchronizeApiRequests;

      this.ApiRequestAttempts = apiRequestAttempts;
      this.ApiRequestDelay = apiRequestDelay;
      this.ApiRequestDelayExponentialFactor = apiRequestDelayExponentialFactor;

      this.BufferSize = bufferSize;
      this.ChunksPackSize = chunksPackSize;

#if ASYNC
      if (reportProgressChunkSize < this.BufferSize)
      {
        throw new ArgumentException(
          $"ReportProgressChunkSize ({reportProgressChunkSize}) cannot have a value lower than BufferSize ({bufferSize})",
          nameof(reportProgressChunkSize));
      }
      this.ReportProgressChunkSize = reportProgressChunkSize;
#endif
    }

    public string ApplicationKey { get; }

    public bool SynchronizeApiRequests { get; }

    public int ApiRequestAttempts { get; }

    public int ApiRequestDelay { get; }

    public float ApiRequestDelayExponentialFactor { get; }

    /// <summary>
    /// Size of the buffer used when downloading files
    /// This value has an impact on the progression.
    /// A lower value means more progression reports but a possible higher CPU usage
    /// </summary>
    public int BufferSize { get; }

    /// <summary>
    /// Upload is splitted in multiple fragments (useful for big uploads)
    /// The size of the fragments is defined by mega.nz and are the following:
    /// 0 / 128K / 384K / 768K / 1280K / 1920K / 2688K / 3584K / 4608K / ... (every 1024 KB) / EOF
    /// The upload method tries to upload multiple fragments at once.
    /// Fragments are merged until the total size reaches this value.
    /// The special value -1 merges all chunks in a single fragment and a single upload
    /// </summary>
    public int ChunksPackSize { get; internal set; }

#if ASYNC
    public long ReportProgressChunkSize { get; internal set;}
#endif
  }
}
