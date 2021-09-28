using System;

namespace CG.Web.MegaApiClient.Tests
{
  using System.Threading;

  public class EventTester<T>
  {
    private readonly Action<T> _callback;

    private long _counter;

    public EventTester()
    {
    }

    public EventTester(Action<T> callback)
    {
      _callback = callback;
    }

    public void OnRaised(T value)
    {
      Interlocked.Increment(ref _counter);
      _callback?.Invoke(value);
    }

    public void Reset()
    {
      Interlocked.Exchange(ref _counter, 0);
    }

    public bool Raised => Calls > 0;

    public long Calls => Interlocked.Read(ref _counter);
  }
}
