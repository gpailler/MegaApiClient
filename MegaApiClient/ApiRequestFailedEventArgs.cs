namespace CG.Web.MegaApiClient
{
  using System;

  public class ApiRequestFailedEventArgs : EventArgs
  {
    public ApiRequestFailedEventArgs(Uri url, int attemptNum, TimeSpan retryDelay, ApiResultCode apiResult, string responseJson)
      : this(url, attemptNum, retryDelay, apiResult, responseJson, null)
    {
    }

    public ApiRequestFailedEventArgs(Uri url, int attemptNum, TimeSpan retryDelay, ApiResultCode apiResult, Exception exception)
      : this(url, attemptNum, retryDelay, apiResult, null, exception)
    {
    }

    private ApiRequestFailedEventArgs(Uri url, int attemptNum, TimeSpan retryDelay, ApiResultCode apiResult, string responseJson, Exception exception)
    {
      ApiUrl = url;
      AttemptNum = attemptNum;
      RetryDelay = retryDelay;
      ApiResult = apiResult;
      ResponseJson = responseJson;
      Exception = exception;
    }

    public Uri ApiUrl { get; }

    public ApiResultCode ApiResult { get; }

    public string ResponseJson { get; }

    public int AttemptNum { get; }

    public TimeSpan RetryDelay { get; }

    public Exception Exception { get; }
  }
}
