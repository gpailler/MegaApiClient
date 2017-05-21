using System;

namespace CG.Web.MegaApiClient.Tests
{
    using System.Threading;

    public class EventTester<T>
    {
        private readonly Action<T> callback;

        private long counter;

        public EventTester()
        {
        }

        public EventTester(Action<T> callback)
        {
            this.callback = callback;
        }

        public void OnRaised(T value)
        {
            Interlocked.Increment(ref this.counter);
            this.callback?.Invoke(value);
        }

        public void Reset()
        {
            Interlocked.Exchange(ref this.counter, 0);
        }

        public bool Raised
        {
            get { return this.Calls > 0; }
        }

        public long Calls
        {
            get { return Interlocked.Read(ref this.counter); }
        }
    }
}
