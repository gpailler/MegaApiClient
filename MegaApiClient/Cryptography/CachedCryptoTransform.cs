namespace CG.Web.MegaApiClient.Cryptography
{
  using System;
  using System.Security.Cryptography;

  internal class CachedCryptoTransform : ICryptoTransform
  {
    private readonly Func<ICryptoTransform> _factory;
    private readonly bool _isKnownReusable;
    private ICryptoTransform _cachedInstance;

    public CachedCryptoTransform(Func<ICryptoTransform> factory, bool isKnownReusable)
    {
      _factory = factory;
      _isKnownReusable = isKnownReusable;
    }

    public void Dispose()
    {
      _cachedInstance?.Dispose();
    }

    public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
    {
      return Forward(x => x.TransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset));
    }

    public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
    {
      if (_isKnownReusable && _cachedInstance != null)
      {
        // Fast path.
        return _cachedInstance.TransformFinalBlock(inputBuffer, inputOffset, inputCount);
      }
      else
      {
        return Forward(x => x.TransformFinalBlock(inputBuffer, inputOffset, inputCount));
      }
    }

    public int InputBlockSize => Forward(x => x.InputBlockSize);

    public int OutputBlockSize => Forward(x => x.OutputBlockSize);

    public bool CanTransformMultipleBlocks => Forward(x => x.CanTransformMultipleBlocks);

    public bool CanReuseTransform => Forward(x => x.CanReuseTransform);

    private T Forward<T>(Func<ICryptoTransform, T> action)
    {
      var instance = _cachedInstance ?? _factory();

      try
      {
        return action(instance);
      }
      finally
      {
        if (!_isKnownReusable && instance.CanReuseTransform == false) // Try to avoid a virtual call to CanReuseTransform.
        {
          instance.Dispose();
        }
        else
        {
          _cachedInstance = instance;
        }
      }
    }
  }
}
