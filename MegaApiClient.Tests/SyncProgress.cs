using System;

namespace CG.Web.MegaApiClient.Tests
{
  internal class SyncProgress<T> : IProgress<T>
  {
    private readonly Action<T> _callback;

    public SyncProgress(Action<T> callback)
    {
      _callback = callback;
    }

    public void Report(T value)
    {
      _callback(value);
    }
  }
}
