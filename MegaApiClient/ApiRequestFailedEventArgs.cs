namespace CG.Web.MegaApiClient
{
  using System;

  public class ApiRequestFailedEventArgs : EventArgs
  {
    public ApiRequestFailedEventArgs(Uri url, int attemptNum, int delayMilliseconds, ApiResultCode apiResult, string responseJson)
    {
      this.ApiUrl = url;
      this.AttemptNum = attemptNum;
      this.DelayMilliseconds = delayMilliseconds;
      this.ApiResult = apiResult;
      this.ResponseJson = responseJson;
    }

    public Uri ApiUrl { get; private set; }

    public ApiResultCode ApiResult { get; private set; }

    public string ResponseJson { get; private set; }

    public int AttemptNum { get; private set; }

    public int DelayMilliseconds { get; private set; }
  }
}