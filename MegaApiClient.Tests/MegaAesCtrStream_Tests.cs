namespace CG.Web.MegaApiClient.Tests
{
  using System;
  using System.IO;
  using Xunit;

  public class MegaAesCtrStreamTests : IDisposable
  {
    private readonly byte[] _originalData;
    private readonly Stream _decryptedStream;

    public MegaAesCtrStreamTests()
    {
      _originalData = new byte[123];
      new Random().NextBytes(_originalData);

      var encryptedData = new byte[_originalData.Length];
      using var encryptedStream = new MegaAesCtrStreamCrypter(new MemoryStream(_originalData));
      encryptedStream.Read(encryptedData, 0, encryptedData.Length);

      _decryptedStream = new MegaAesCtrStreamDecrypter(
        new MemoryStream(encryptedData),
        encryptedData.Length,
        encryptedStream.FileKey,
        encryptedStream.Iv,
        encryptedStream.MetaMac);
    }

    public void Dispose()
    {
      _decryptedStream.Dispose();
    }

    [Theory]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(123)]
    [InlineData(150)]
    [InlineData(20)]
    [InlineData(6133)]
    public void Read_DifferentBufferSize_Succeeds(int bufferSize)
    {
      // Arrange
      var decryptedData = new byte[_decryptedStream.Length];
      var subBuffer = new byte[bufferSize];
      var pos = 0;

      int read;
      // Act
      while ((read = _decryptedStream.Read(subBuffer, 0, subBuffer.Length)) > 0)
      {
        Array.Copy(subBuffer, 0, decryptedData, pos, read);
        pos += read;
      }

      // Assert
      Assert.Equal(_originalData, decryptedData);
    }

    [Fact]
    public void Read_TooSmallBufferSize_Throws()
    {
      // Arrange
      var subBuffer = new byte[15];

      // Act + Assert
      var exception = Assert.Throws<NotSupportedException>(() => _decryptedStream.Read(subBuffer, 0, subBuffer.Length));
      Assert.Equal("Invalid 'count' argument. Minimal read operation must be greater than 16 bytes (except for last read operation).", exception.Message);
    }

    [Theory]
    [InlineData(120)]
    [InlineData(16)]
    public void Read_MixedBuffersSize_Succeeds(int firstBufferSize)
    {
      // Arrange
      var decryptedData = new byte[_decryptedStream.Length];
      var pos = 0;

      // Act
      var subBuffer = new byte[firstBufferSize];
      var read = _decryptedStream.Read(subBuffer, 0, subBuffer.Length);
      Array.Copy(subBuffer, 0, decryptedData, pos, read);
      pos += read;

      subBuffer = new byte[decryptedData.Length - firstBufferSize + (decryptedData.Length - read)];
      read = _decryptedStream.Read(subBuffer, 0, subBuffer.Length);
      Array.Copy(subBuffer, 0, decryptedData, pos, read);

      // Assert
      Assert.Equal(_originalData, decryptedData);
    }
  }
}
