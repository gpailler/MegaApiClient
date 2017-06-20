using System;

namespace CG.Web.MegaApiClient.Tests
{
  internal class SyncProgress<T> : IProgress<T>
  {
    private readonly Action<T> callback;

    public SyncProgress(Action<T> callback)
    {
      this.callback = callback;
    }

    public void Report(T value)
    {
      this.callback(value);
    }
  }
}
