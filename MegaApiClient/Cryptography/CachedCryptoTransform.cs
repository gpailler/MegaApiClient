namespace CG.Web.MegaApiClient.Cryptography
{
  using System;
  using System.Security.Cryptography;

  internal class CachedCryptoTransform : ICryptoTransform
  {
    private readonly Func<ICryptoTransform> factory;
    private ICryptoTransform cachedInstance;

    public CachedCryptoTransform(Func<ICryptoTransform> factory)
    {
      this.factory = factory;
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
      return this.Forward(x => x.TransformFinalBlock(inputBuffer, inputOffset, inputCount));
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
        if (instance.CanReuseTransform == false)
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
