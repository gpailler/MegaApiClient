namespace CG.Web.MegaApiClient.Tests
{
  using System;
  using System.IO;
  using Xunit;

  public class MegaAesCtrStream_Tests : IDisposable
  {
    private readonly byte[] originalData;
    private readonly Stream decryptedStream;
    
    public MegaAesCtrStream_Tests()
    {
      this.originalData = new byte[123];
      new Random().NextBytes(this.originalData);

      byte[] encryptedData = new byte[this.originalData.Length];
      using (var encryptedStream = new MegaAesCtrStreamCrypter(new MemoryStream(this.originalData)))
      {
        encryptedStream.Read(encryptedData, 0, encryptedData.Length);

        this.decryptedStream = new MegaAesCtrStreamDecrypter(
          new MemoryStream(encryptedData),
          encryptedData.Length,
          encryptedStream.FileKey,
          encryptedStream.Iv,
          encryptedStream.MetaMac);
      }
    }

    public void Dispose()
    {
      this.decryptedStream.Dispose();
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
      byte[] decryptedData = new byte[decryptedStream.Length];
      var subBuffer = new byte[bufferSize];
      int read = 0;
      int pos = 0;
      
      // Act
      while ((read = decryptedStream.Read(subBuffer, 0, subBuffer.Length)) > 0)
      {
        Array.Copy(subBuffer, 0, decryptedData, pos, read);
        pos += read;
      }
      
      // Assert
     Assert.Equal(this.originalData, decryptedData);
    }

    [Fact]
    public void Read_TooSmallBufferSize_Throws()
    {
      // Arrange
      var subBuffer = new byte[15];
      
      // Act + Assert
      var exception = Assert.Throws<NotSupportedException>(() => decryptedStream.Read(subBuffer, 0, subBuffer.Length));
      Assert.Equal("Invalid 'count' argument. Minimal read operation must be greater than 16 bytes (except for last read operation).", exception.Message);
    }
    
    [Theory]
    [InlineData(120)]
    [InlineData(16)]
    public void Read_MixedBuffersSize_Succeeds(int firstBufferSize)
    {
      // Arrange
      byte[] decryptedData = new byte[decryptedStream.Length];
      int pos = 0;
      int read = 0;
      
      // Act
      var subBuffer = new byte[firstBufferSize];
      read = decryptedStream.Read(subBuffer, 0, subBuffer.Length);
      Array.Copy(subBuffer, 0, decryptedData, pos, read);
      pos += read;
      
      subBuffer = new byte[decryptedData.Length - firstBufferSize + (decryptedData.Length - read)];
      read = decryptedStream.Read(subBuffer, 0, subBuffer.Length);
      Array.Copy(subBuffer, 0, decryptedData, pos, read);

      // Assert
      Assert.Equal(this.originalData, decryptedData);
    }
  }
}
