namespace CG.Web.MegaApiClient
{
  using System;

  public class ApiException : Exception
  {
    internal ApiException(ApiResultCode apiResultCode)
    {
      this.ApiResultCode = apiResultCode;
    }

    public ApiResultCode ApiResultCode { get; private set; }

    public override string Message
    {
      get { return string.Format("API response: {0}", this.ApiResultCode); }
    }
  }

  public class DownloadException : Exception
  {
    public DownloadException()
      : base("Invalid file checksum")
    {
    }
  }

  public class UploadException : Exception
  {
    public UploadException(string error)
      : base("Upload error: " + error)
    {
    }
  }

  public enum ApiResultCode
  {
    /// <summary>
    /// API_OK (0): Success
    /// </summary>
    Ok = 0,

    /// <summary>
    /// API_EINTERNAL (-1): An internal error has occurred. Please submit a bug report, detailing the exact circumstances in which this error occurred.
    /// </summary>
    InternalError = -1,

    /// <summary>
    /// API_EARGS (-2): You have passed invalid arguments to this command.
    /// </summary>
    BadArguments = -2,

    /// <summary>
    /// API_EAGAIN (-3) (always at the request level): A temporary congestion or server malfunction prevented your request from being processed. No data was altered. Retry. Retries must be spaced with exponential backoff.
    /// </summary>
    RequestFailedRetry = -3,

    /// <summary>
    /// API_ERATELIMIT (-4): You have exceeded your command weight per time quota. Please wait a few seconds, then try again (this should never happen in sane real-life applications).
    /// </summary>
    TooManyRequests = -4,

    /// <summary>
    /// API_EFAILED (-5): The upload failed. Please restart it from scratch.
    /// </summary>
    RequestFailedPermanetly = -5,

    /// <summary>
    /// API_ETOOMANY (-6): Too many concurrent IP addresses are accessing this upload target URL.
    /// </summary>
    ToManyRequestsForThisResource = -6,

    /// <summary>
    /// API_ERANGE (-7): The upload file packet is out of range or not starting and ending on a chunk boundary.
    /// </summary>
    ResourceAccessOutOfRange = -7,

    /// <summary>
    /// API_EEXPIRED (-8): The upload target URL you are trying to access has expired. Please request a fresh one.
    /// </summary>
    ResourceExpired = -8,

    /// <summary>
    /// API_EOENT (-9): Object (typically, node or user) not found
    /// </summary>
    ResourceNotExists = -9,

    /// <summary>
    /// API_ECIRCULAR (-10): Circular linkage attempted
    /// </summary>
    CircularLinkage = -10,

    /// <summary>
    /// API_EACCESS (-11): Access violation (e.g., trying to write to a read-only share)
    /// </summary>
    AccessDenied = -11,

    /// <summary>
    /// API_EEXIST (-12): Trying to create an object that already exists
    /// </summary>
    ResourceAlreadyExists = -12,

    /// <summary>
    /// API_EINCOMPLETE (-13): Trying to access an incomplete resource
    /// </summary>
    RequestIncomplete = -13,

    /// <summary>
    /// API_EKEY (-14): A decryption operation failed (never returned by the API)
    /// </summary>
    CryptographicError = -14,

    /// <summary>
    /// API_ESID (-15): Invalid or expired user session, please relogin
    /// </summary>
    BadSessionId = -15,

    /// <summary>
    /// API_EBLOCKED (-16): User blocked
    /// </summary>
    ResourceAdministrativelyBlocked = -16,

    /// <summary>
    /// API_EOVERQUOTA (-17): Request over quota
    /// </summary>
    QuotaExceeded = -17,

    /// <summary>
    /// API_ETEMPUNAVAIL (-18): Resource temporarily not available, please try again later
    /// </summary>
    ResourceTemporarilyNotAvailable = -18,

    /// <summary>
    /// API_ETOOMANYCONNECTIONS (-19): Too many connections on this resource
    /// </summary>
    TooManyConnectionsOnThisResource = -19,

    /// <summary>
    /// API_EWRITE (-20): Write failed
    /// </summary>
    FileCouldNotBeWrittenTo = -20,

    /// <summary>
    /// API_EREAD (-21): Read failed
    /// </summary>
    FileCouldNotBeReadFrom = -21,

    /// <summary>
    /// API_EAPPKEY (-22): Invalid application key; request not processed
    /// </summary>
    InvalidOrMissingApplicationKey = -22
  }
}
