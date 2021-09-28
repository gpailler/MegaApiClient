namespace CG.Web.MegaApiClient.Serialization
{
  using System;
  using System.IO;
  using System.Runtime.Serialization;
  using DamienG.Security.Cryptography;
  using Newtonsoft.Json;

  internal class Attributes
  {
    private const int CrcArrayLength = 4;
    private const int CrcSize = sizeof(uint) * CrcArrayLength;
    private const int FingerprintMaxSize = CrcSize + 1 + sizeof(long);
    private const int MaxFull = 8192;
    private const uint CryptoPPCRC32Polynomial = 0xEDB88320;

    [JsonConstructor]
    private Attributes()
    {
    }

    public Attributes(string name)
    {
      Name = name;
    }

    public Attributes(string name, Attributes originalAttributes)
    {
      Name = name;
      SerializedFingerprint = originalAttributes.SerializedFingerprint;
    }

    public Attributes(string name, Stream stream, DateTime? modificationDate = null)
    {
      Name = name;

      if (modificationDate.HasValue)
      {
        var fingerprintBuffer = new byte[FingerprintMaxSize];

        var crc = ComputeCrc(stream);
        Buffer.BlockCopy(crc, 0, fingerprintBuffer, 0, CrcSize);

        var serializedModificationDate = modificationDate.Value.ToEpoch().SerializeToBytes();
        Buffer.BlockCopy(serializedModificationDate, 0, fingerprintBuffer, CrcSize, serializedModificationDate.Length);

        Array.Resize(ref fingerprintBuffer, fingerprintBuffer.Length - (sizeof(long) + 1) + serializedModificationDate.Length);

        SerializedFingerprint = fingerprintBuffer.ToBase64();
      }
    }

    [JsonProperty("n")]
    public string Name { get; set; }

    [JsonProperty("c", DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string SerializedFingerprint { get; set; }

    [JsonIgnore]
    public DateTime? ModificationDate
    {
      get; private set;
    }

    [OnDeserialized]
    public void OnDeserialized(StreamingContext context)
    {
      if (SerializedFingerprint != null)
      {
        var fingerprintBytes = SerializedFingerprint.FromBase64();
        ModificationDate = fingerprintBytes.DeserializeToLong(CrcSize, fingerprintBytes.Length - CrcSize).ToDateTime();
      }
    }

    private uint[] ComputeCrc(Stream stream)
    {
      // From https://github.com/meganz/sdk/blob/d4b462efc702a9c645e90c202b57e14da3de3501/src/filefingerprint.cpp

      stream.Seek(0, SeekOrigin.Begin);

      var crc = new uint[CrcArrayLength];
      var newCrcBuffer = new byte[CrcSize];
      uint crcVal = 0;

      if (stream.Length <= CrcSize)
      {
        // tiny file: read verbatim, NUL pad
        if (0 != stream.Read(newCrcBuffer, 0, (int)stream.Length))
        {
          Buffer.BlockCopy(newCrcBuffer, 0, crc, 0, newCrcBuffer.Length);
        }
      }
      else if (stream.Length <= MaxFull)
      {
        // small file: full coverage, four full CRC32s
        var fileBuffer = new byte[stream.Length];
        var read = 0;
        while ((read += stream.Read(fileBuffer, read, (int)stream.Length - read)) < stream.Length)
        {
          ;
        }

        for (var i = 0; i < crc.Length; i++)
        {
          var begin = (int)(i * stream.Length / crc.Length);
          var end = (int)((i + 1) * stream.Length / crc.Length);

          using (var crc32Hasher = new Crc32(CryptoPPCRC32Polynomial, Crc32.DefaultSeed))
          {
            var crcValBytes = crc32Hasher.ComputeHash(fileBuffer, begin, end - begin);
            crcVal = BitConverter.ToUInt32(crcValBytes, 0);
          }

          crc[i] = crcVal;
        }
      }
      else
      {
        // large file: sparse coverage, four sparse CRC32s
        var block = new byte[4 * CrcSize];
        var blocks = (uint)(MaxFull / (block.Length * CrcArrayLength));
        long current = 0;

        for (uint i = 0; i < CrcArrayLength; i++)
        {
          byte[] crc32ValBytes = null;

          var seed = Crc32.DefaultSeed;
          for (uint j = 0; j < blocks; j++)
          {
            var offset = (stream.Length - block.Length) * (i * blocks + j) / (CrcArrayLength * blocks - 1);

            stream.Seek(offset - current, SeekOrigin.Current);
            current += (offset - current);

            var blockWritten = stream.Read(block, 0, block.Length);
            current += blockWritten;

            using (var crc32Hasher = new Crc32(CryptoPPCRC32Polynomial, seed))
            {
              crc32ValBytes = crc32Hasher.ComputeHash(block, 0, blockWritten);
              var seedBytes = new byte[crc32ValBytes.Length];
              crc32ValBytes.CopyTo(seedBytes, 0);
              if (BitConverter.IsLittleEndian)
              {
                Array.Reverse(seedBytes);
              }

              seed = BitConverter.ToUInt32(seedBytes, 0);
              seed = ~seed;
            }
          }

          crcVal = BitConverter.ToUInt32(crc32ValBytes, 0);

          crc[i] = crcVal;
        }
      }

      return crc;
    }
  }
}
