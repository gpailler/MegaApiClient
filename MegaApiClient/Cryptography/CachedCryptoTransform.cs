namespace CG.Web.MegaApiClient.Cryptography
{
  using System;
  using System.Security.Cryptography;

  internal class CachedCryptoTransform : ICryptoTransform
  {
    private readonly Func<ICryptoTransform> factory;
    private readonly bool isKnownReusable;
    private ICryptoTransform cachedInstance;

    public CachedCryptoTransform(Func<ICryptoTransform> factory, bool isKnownReusable)
    {
        this.factory = factory;
        this.isKnownReusable = isKnownReusable;
    }

    public void Dispose()
    {
      this.cachedInstance?.Dispose();
    }

    public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
    {
      return this.Forward(x => x.TransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset));
    }

    public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
    {
      if (isKnownReusable && cachedInstance != null)
      {
        // Fast path.
        return cachedInstance.TransformFinalBlock(inputBuffer, inputOffset, inputCount);
      }
      else
      {
        return this.Forward(x => x.TransformFinalBlock(inputBuffer, inputOffset, inputCount));
      }
    }

    public int InputBlockSize { get { return this.Forward(x => x.InputBlockSize); } }

    public int OutputBlockSize { get { return this.Forward(x => x.OutputBlockSize); } }

    public bool CanTransformMultipleBlocks { get { return this.Forward(x => x.CanTransformMultipleBlocks); } }

    public bool CanReuseTransform { get { return this.Forward(x => x.CanReuseTransform); } }

    private T Forward<T>(Func<ICryptoTransform, T> action)
    {
      ICryptoTransform instance = this.cachedInstance ?? this.factory();

      try
      {
        return action(instance);
      }
      finally
      {
        if (!isKnownReusable && instance.CanReuseTransform == false) // Try to avoid a virtual call to CanReuseTransform.
        {
          instance.Dispose();
        }
        else
        {
          this.cachedInstance = instance;
        }
      }
    }
  }
}
