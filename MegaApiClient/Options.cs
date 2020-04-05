namespace CG.Web.MegaApiClient
{
  using System;
  using System.Linq;

  public class Options
  {
    public const string DefaultApplicationKey = "axhQiYyQ";
    public const bool DefaultSynchronizeApiRequests = true;

    public const int DefaultApiRequestAttempts = 17;
    public const int DefaultApiRequestDelay = 100;
    public const float DefaultApiRequestDelayFactor = 1.5f;

    public const int DefaultBufferSize = 1024 * 64;
    public const int DefaultChunksPackSize = 1024 * 1024;

#if !NET40
    public const long DefaultReportProgressChunkSize = DefaultBufferSize;
#endif

    public delegate bool ComputeApiRequestRetryWaitDelayDelegate(int attempt, out TimeSpan delay);

    public Options(
      string applicationKey = DefaultApplicationKey,
      bool synchronizeApiRequests = DefaultSynchronizeApiRequests,
      ComputeApiRequestRetryWaitDelayDelegate computeApiRequestRetryWaitDelay = null,
      int bufferSize = DefaultBufferSize,
      int chunksPackSize = DefaultChunksPackSize
#if !NET40
      ,
      long reportProgressChunkSize = DefaultReportProgressChunkSize
#endif
      )
    {
      this.ApplicationKey = applicationKey;
      this.SynchronizeApiRequests = synchronizeApiRequests;

      this.ComputeApiRequestRetryWaitDelay = computeApiRequestRetryWaitDelay ?? this.ComputeDefaultApiRequestRetryWaitDelay;

      this.BufferSize = bufferSize;
      this.ChunksPackSize = chunksPackSize;

#if !NET40
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

    public ComputeApiRequestRetryWaitDelayDelegate ComputeApiRequestRetryWaitDelay { get; }

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

#if !NET40
    public long ReportProgressChunkSize { get; internal set;}
#endif

    private bool ComputeDefaultApiRequestRetryWaitDelay(int attempt, out TimeSpan delay)
    {
      if (attempt > DefaultApiRequestAttempts)
      {
        delay = default;
        return false;
      }
      else
      {
        var delayMilliseconds = Enumerable.Range(0, attempt)
          .Aggregate(0, (current, item) => (int)(current == 0 ? DefaultApiRequestDelay : current * DefaultApiRequestDelayFactor));
        delay = TimeSpan.FromMilliseconds(delayMilliseconds);
        return true;
      }
    }
  }
}
