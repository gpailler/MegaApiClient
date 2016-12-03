namespace CG.Web.MegaApiClient
{
  using System;
  using System.Security.Cryptography;

  using Newtonsoft.Json;

  internal class Crypto
  {
    private static readonly Rijndael RijndaelCbc;
    private static readonly byte[] DefaultIv = new byte[16];

    public static uint CryptoPPCRC32Polynomial = 0xEDB88320;

    static Crypto()
    {
      RijndaelCbc = Rijndael.Create();
      RijndaelCbc.Padding = PaddingMode.None;
      RijndaelCbc.Mode = CipherMode.CBC;
    }

    #region Key

    public static byte[] DecryptKey(byte[] data, byte[] key)
    {
      byte[] result = new byte[data.Length];

      for (int idx = 0; idx < data.Length; idx += 16)
      {
        byte[] block = data.CopySubArray(16, idx);
        byte[] decryptedBlock = DecryptAes(block, key);
        Array.Copy(decryptedBlock, 0, result, idx, 16);
      }

      return result;
    }

    public static byte[] EncryptKey(byte[] data, byte[] key)
    {
      byte[] result = new byte[data.Length];

      for (int idx = 0; idx < data.Length; idx += 16)
      {
        byte[] block = data.CopySubArray(16, idx);
        byte[] encryptedBlock = EncryptAes(block, key);
        Array.Copy(encryptedBlock, 0, result, idx, 16);
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
      for (int idx = 0; idx < 16; idx++)
      {
        fileKey[idx] = (byte)(decryptedKey[idx] ^ decryptedKey[idx + 16]);
      }
    }

    #endregion

    #region Aes

    public static byte[] DecryptAes(byte[] data, byte[] key)
    {
      using (ICryptoTransform decryptor = RijndaelCbc.CreateDecryptor(key, DefaultIv))
      {
        return decryptor.TransformFinalBlock(data, 0, data.Length);
      }
    }

    public static byte[] EncryptAes(byte[] data, byte[] key)
    {
      using (ICryptoTransform encryptor = RijndaelCbc.CreateEncryptor(key, DefaultIv))
      {
        return encryptor.TransformFinalBlock(data, 0, data.Length);
      }
    }

    public static byte[] CreateAesKey()
    {
      using (Rijndael rijndael = Rijndael.Create())
      {
        rijndael.Mode = CipherMode.CBC;
        rijndael.KeySize = 128;
        rijndael.Padding = PaddingMode.None;
        rijndael.GenerateKey();
        return rijndael.Key;
      }
    }

    #endregion

    #region Attributes

    public static byte[] EncryptAttributes(Attributes attributes, byte[] nodeKey)
    {
      string data = "MEGA" + JsonConvert.SerializeObject(attributes, Formatting.None);
      byte[] dataBytes = data.ToBytes();

        if (attributes.CRC != null)
        {
            FileFingerprint fileFingerprint = new FileFingerprint(attributes.CRC, attributes.ModificationDate);
            byte[] fingerprintBytes = fileFingerprint.SerializeFingerprint();
            attributes.SetFingerprint(ref fileFingerprint);
      string crcStr = String.Format("{0:x} {1:x} {2:x} {3:x}", attributes.CRC[0], attributes.CRC[1], attributes.CRC[2], attributes.CRC[3]);
      //var fingerprintBytes = attributes.FingerprintBase64.FromBase64();
      string fingerprintBytesStr = BitConverter.ToString(fingerprintBytes).Replace("-", "");
            //throw new Exception(String.Format("Encrypt attrs: fingerprint={0}, crc={1}, mtime={2}", fingerprintBytesStr, crcStr, attributes.ModificationDate.HasValue ? attributes.ModificationDate.Value.ToUniversalTime().Subtract(Node.OriginalDateTime).TotalSeconds : 0));
        }
      dataBytes = dataBytes.CopySubArray(dataBytes.Length + 16 - (dataBytes.Length % 16));

      return EncryptAes(dataBytes, nodeKey);
    }

    public static Attributes DecryptAttributes(byte[] attributes, byte[] nodeKey)
    {
      byte[] decryptedAttributes = DecryptAes(attributes, nodeKey);

      // Remove MEGA prefix
      try
      {
        string json = decryptedAttributes.ToUTF8String().Substring(4);
        int nullTerminationIndex = json.IndexOf('\0');
        if (nullTerminationIndex != -1)
        {
          json = json.Substring(0, nullTerminationIndex);
        }

        var attrs = JsonConvert.DeserializeObject<Attributes>(json);
        if (attrs.FingerprintBase64 != null)
        {
            FileFingerprint ff = default(FileFingerprint);
            var fingerprintBytes = attrs.FingerprintBase64.FromBase64();
            ff.UnserializeFingerprint(fingerprintBytes);

            attrs.ParseFingerprint(ff);
            //string crcStr = String.Format("{0:x} {1:x} {2:x} {3:x}", attrs.CRC[0], attrs.CRC[1], attrs.CRC[2], attrs.CRC[3]);
            //string fingerprintBytesStr = BitConverter.ToString(fingerprintBytes).Replace("-", "");
            //throw new Exception(String.Format("Decrypt attrs: fingerprint={0}, crc={1}, mtime={2}", fingerprintBytesStr, crcStr, ff.ModificationTimeStamp));
        }

        return attrs;
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
      byte[] rsaPrivateKey = DecryptKey(encodedRsaPrivateKey, masterKey);

      // rsaPrivateKeyComponents[0] => First factor p
      // rsaPrivateKeyComponents[1] => Second factor q
      // rsaPrivateKeyComponents[2] => Private exponent d
      BigInteger[] rsaPrivateKeyComponents = new BigInteger[4];
      for (int i = 0; i < 4; i++)
      {
        rsaPrivateKeyComponents[i] = rsaPrivateKey.FromMPINumber();

        // Remove already retrieved part
        int dataLength = ((rsaPrivateKey[0] * 256 + rsaPrivateKey[1] + 7) / 8);
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
