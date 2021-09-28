namespace CG.Web.MegaApiClient.Cryptography
{
  using System;
  using System.Security.Cryptography;

  using CG.Web.MegaApiClient.Serialization;

  using Newtonsoft.Json;

  internal class Crypto
  {
    private static readonly Aes s_aesCbc;
    private static readonly bool s_isKnownReusable;
    private static readonly byte[] s_defaultIv = new byte[16];

    static Crypto()
    {
#if NETSTANDARD1_3 || NETSTANDARD2_0
      s_aesCbc = Aes.Create(); // More per-call overhead but supported everywhere.
      s_isKnownReusable = false;
#else
      s_aesCbc = new AesManaged();
      s_isKnownReusable = true;
#endif

      s_aesCbc.Padding = PaddingMode.None;
      s_aesCbc.Mode = CipherMode.CBC;
    }

    #region Key

    public static byte[] DecryptKey(byte[] data, byte[] key)
    {
      var result = new byte[data.Length];

      for (var idx = 0; idx < data.Length; idx += 16)
      {
        var block = data.CopySubArray(16, idx);
        var decryptedBlock = DecryptAes(block, key);
        Array.Copy(decryptedBlock, 0, result, idx, 16);
      }

      return result;
    }

    public static byte[] EncryptKey(byte[] data, byte[] key)
    {
      var result = new byte[data.Length];
      using (var encryptor = CreateAesEncryptor(key))
      {
        for (var idx = 0; idx < data.Length; idx += 16)
        {
          var block = data.CopySubArray(16, idx);
          var encryptedBlock = EncryptAes(block, encryptor);
          Array.Copy(encryptedBlock, 0, result, idx, 16);
        }
      }

      return result;
    }

    public static void GetPartsFromDecryptedKey(byte[] decryptedKey, out byte[] iv, out byte[] metaMac, out byte[] fileKey)
    {
      // Extract Iv and MetaMac
      iv = new byte[8];
      metaMac = new byte[8];
      Array.Copy(decryptedKey, 16, iv, 0, 8);
      Array.Copy(decryptedKey, 24, metaMac, 0, 8);

      // For files, key is 256 bits long. Compute the key to retrieve 128 AES key
      fileKey = new byte[16];
      for (var idx = 0; idx < 16; idx++)
      {
        fileKey[idx] = (byte)(decryptedKey[idx] ^ decryptedKey[idx + 16]);
      }
    }

    #endregion

    #region Aes

    public static byte[] DecryptAes(byte[] data, byte[] key)
    {
      using (var decryptor = s_aesCbc.CreateDecryptor(key, s_defaultIv))
      {
        return decryptor.TransformFinalBlock(data, 0, data.Length);
      }
    }

    public static ICryptoTransform CreateAesEncryptor(byte[] key)
    {
      return new CachedCryptoTransform(() => s_aesCbc.CreateEncryptor(key, s_defaultIv), s_isKnownReusable);
    }

    public static byte[] EncryptAes(byte[] data, ICryptoTransform encryptor)
    {
      return encryptor.TransformFinalBlock(data, 0, data.Length);
    }

    public static byte[] EncryptAes(byte[] data, byte[] key)
    {
      using (var encryptor = CreateAesEncryptor(key))
      {
        return encryptor.TransformFinalBlock(data, 0, data.Length);
      }
    }

    public static byte[] CreateAesKey()
    {
      using (var aes = Aes.Create())
      {
        aes.Mode = CipherMode.CBC;
        aes.KeySize = 128;
        aes.Padding = PaddingMode.None;
        aes.GenerateKey();
        return aes.Key;
      }
    }

    #endregion

    #region Attributes

    public static byte[] EncryptAttributes(Attributes attributes, byte[] nodeKey)
    {
      var data = "MEGA" + JsonConvert.SerializeObject(attributes, Formatting.None);
      var dataBytes = data.ToBytes();
      dataBytes = dataBytes.CopySubArray(dataBytes.Length + 16 - (dataBytes.Length % 16));

      return EncryptAes(dataBytes, nodeKey);
    }

    public static Attributes DecryptAttributes(byte[] attributes, byte[] nodeKey)
    {
      var decryptedAttributes = DecryptAes(attributes, nodeKey);

      // Remove MEGA prefix
      try
      {
        var json = decryptedAttributes.ToUTF8String().Substring(4);
        var nullTerminationIndex = json.IndexOf('\0');
        if (nullTerminationIndex != -1)
        {
          json = json.Substring(0, nullTerminationIndex);
        }

        return JsonConvert.DeserializeObject<Attributes>(json);
      }
      catch (Exception ex)
      {
        return new Attributes(string.Format("Attribute deserialization failed: {0}", ex.Message));
      }
    }

    #endregion

    #region Rsa

    public static BigInteger[] GetRsaPrivateKeyComponents(byte[] encodedRsaPrivateKey, byte[] masterKey)
    {
      // We need to add padding to obtain multiple of 16
      encodedRsaPrivateKey = encodedRsaPrivateKey.CopySubArray(encodedRsaPrivateKey.Length + (16 - encodedRsaPrivateKey.Length % 16));
      var rsaPrivateKey = DecryptKey(encodedRsaPrivateKey, masterKey);

      // rsaPrivateKeyComponents[0] => First factor p
      // rsaPrivateKeyComponents[1] => Second factor q
      // rsaPrivateKeyComponents[2] => Private exponent d
      var rsaPrivateKeyComponents = new BigInteger[4];
      for (var i = 0; i < 4; i++)
      {
        rsaPrivateKeyComponents[i] = rsaPrivateKey.FromMPINumber();

        // Remove already retrieved part
        var dataLength = ((rsaPrivateKey[0] * 256 + rsaPrivateKey[1] + 7) / 8);
        rsaPrivateKey = rsaPrivateKey.CopySubArray(rsaPrivateKey.Length - dataLength - 2, dataLength + 2);
      }

      return rsaPrivateKeyComponents;
    }

    public static byte[] RsaDecrypt(BigInteger data, BigInteger p, BigInteger q, BigInteger d)
    {
      return data.modPow(d, p * q).getBytes();
    }

    #endregion
  }
}
