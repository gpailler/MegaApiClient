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

  public enum ApiResultCode
  {
    Ok = 0,
    InternalError = -1,
    BadArguments = -2,
    RequestFailedRetry = -3,
    TooManyRequests = -4,
    RequestFailedPermanetly = -5,
    ToManyRequestsForThisResource = -6,
    ResourceAccessOutOfRange = -7,
    ResourceExpired = -8,
    ResourceNotExists = -9,
    CircularLinkage = -10,
    AccessDenied = -11,
    ResourceAlreadyExists = -12,
    RequestIncomplete = -13,
    CryptographicError = -14,
    BadSessionId = -15,
    ResourceAdministrativelyBlocked = -16,
    QuotaExceeded = -17,
    ResourceTemporarilyNotAvailable = -18,
    TooManyConnectionsOnThisResource = -19,
    FileCouldNotBeWrittenTo = -20,
    FileCouldNotBeReadFrom = -21,
    InvalidOrMissingApplicationKey = -22
  }
}
