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
    private const int MAXFULL = 8192;
    private const uint CryptoPPCRC32Polynomial = 0xEDB88320;

    [JsonConstructor]
    private Attributes()
    {
    }

    public Attributes(string name)
    {
      this.Name = name;
    }

    public Attributes(string name, Attributes originalAttributes)
    {
      this.Name = name;
      this.SerializedFingerprint = originalAttributes.SerializedFingerprint;
    }

    public Attributes(string name, Stream stream, DateTime? modificationDate = null)
    {
      this.Name = name;

      if (modificationDate.HasValue)
      {
        byte[] fingerprintBuffer = new byte[FingerprintMaxSize];

        uint[] crc = this.ComputeCrc(stream);
        Buffer.BlockCopy(crc, 0, fingerprintBuffer, 0, CrcSize);

        byte[] serializedModificationDate = modificationDate.Value.ToEpoch().SerializeToBytes();
        Buffer.BlockCopy(serializedModificationDate, 0, fingerprintBuffer, CrcSize, serializedModificationDate.Length);

        Array.Resize(ref fingerprintBuffer, fingerprintBuffer.Length - (sizeof(long) + 1) + serializedModificationDate.Length);

        this.SerializedFingerprint = Convert.ToBase64String(fingerprintBuffer);
      }
    }

    [JsonProperty("n")]
    public string Name { get; set; }

    [JsonProperty("c", DefaultValueHandling = DefaultValueHandling.Ignore)]
    private string SerializedFingerprint { get; set; }

    [JsonIgnore]
    public DateTime? ModificationDate
    {
      get; private set;
    }

    [OnDeserialized]
    public void OnDeserialized(StreamingContext context)
    {
      if (this.SerializedFingerprint != null)
      {
        var fingerprintBytes = this.SerializedFingerprint.FromBase64();
        this.ModificationDate = fingerprintBytes.DeserializeToLong(CrcSize, fingerprintBytes.Length - CrcSize).ToDateTime();
      }
    }

    private uint[] ComputeCrc(Stream stream)
    {
      // From https://github.com/meganz/sdk/blob/d4b462efc702a9c645e90c202b57e14da3de3501/src/filefingerprint.cpp

      stream.Seek(0, SeekOrigin.Begin);

      uint[] crc = new uint[CrcArrayLength];
      byte[] newCrcBuffer = new byte[CrcSize];
      uint crcVal = 0;

      if (stream.Length <= CrcSize)
      {
        // tiny file: read verbatim, NUL pad
        if (0 != stream.Read(newCrcBuffer, 0, (int)stream.Length))
        {
          Buffer.BlockCopy(newCrcBuffer, 0, crc, 0, newCrcBuffer.Length);
        }
      }
      else if (stream.Length <= MAXFULL)
      {
        // small file: full coverage, four full CRC32s
        byte[] fileBuffer = new byte[stream.Length];
        int read = 0;
        while ((read += stream.Read(fileBuffer, read, (int)stream.Length - read)) < stream.Length) ;
        for (int i = 0; i < crc.Length; i++)
        {
          int begin = (int)(i * stream.Length / crc.Length);
          int end = (int)((i + 1) * stream.Length / crc.Length);

          using (var crc32Hasher = new Crc32(CryptoPPCRC32Polynomial, Crc32.DefaultSeed))
          {
#if NETCORE
            var crcValBytes = crc32Hasher.ComputeHash(fileBuffer, begin, end - begin);
#else
            crc32Hasher.TransformFinalBlock(fileBuffer, begin, end - begin);
            var crcValBytes = crc32Hasher.Hash;
#endif
            crcVal = BitConverter.ToUInt32(crcValBytes, 0);
          }
          crc[i] = crcVal;
        }
      }
      else
      {
        // large file: sparse coverage, four sparse CRC32s
        byte[] block = new byte[4 * CrcSize];
        uint blocks = (uint)(MAXFULL / (block.Length * CrcArrayLength));
        long current = 0;

        for (uint i = 0; i < CrcArrayLength; i++)
        {
          byte[] crc32ValBytes = null;

          uint seed = Crc32.DefaultSeed;
          for (uint j = 0; j < blocks; j++)
          {
            long offset = (stream.Length - block.Length) * (i * blocks + j) / (CrcArrayLength * blocks - 1);

            stream.Seek(offset - current, SeekOrigin.Current);
            current += (offset - current);

            int blockWritten = stream.Read(block, 0, block.Length);
            current += blockWritten;

            using (var crc32Hasher = new Crc32(CryptoPPCRC32Polynomial, seed))
            {
#if NETCORE
              crc32ValBytes = crc32Hasher.ComputeHash(block, 0, blockWritten);
#else
              crc32Hasher.TransformFinalBlock(block, 0, blockWritten);
              crc32ValBytes = crc32Hasher.Hash;
#endif
              var seedBytes = new byte[crc32ValBytes.Length];
              crc32ValBytes.CopyTo(seedBytes, 0);
              if (BitConverter.IsLittleEndian)
                Array.Reverse(seedBytes);

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
