using System;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace CG.Web.MegaApiClient
{
    internal class Crypto
    {
        private static readonly Rijndael Rijndael;
        private static readonly byte[] DefaultIv = new byte[16];

        static Crypto()
        {
            Rijndael = Rijndael.Create();
            Rijndael.Padding = PaddingMode.None;
            Rijndael.Mode = CipherMode.CBC;
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

        #endregion

        #region Aes

        public static byte[] DecryptAes(byte[] data, byte[] key)
        {
            using (ICryptoTransform decryptor = Rijndael.CreateDecryptor(key, DefaultIv))
            {
                return decryptor.TransformFinalBlock(data, 0, data.Length);
            }
        }

        public static byte[] EncryptAes(byte[] data, byte[] key)
        {
            using (ICryptoTransform encryptor = Rijndael.CreateEncryptor(key, DefaultIv))
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
            dataBytes = dataBytes.CopySubArray(dataBytes.Length + 16 - (dataBytes.Length % 16));

            return EncryptAes(dataBytes, nodeKey);
        }

        public static Attributes DecryptAttributes(byte[] attributes, byte[] nodeKey)
        {
            byte[] decryptedAttributes = DecryptAes(attributes, nodeKey);

            // Remove MEGA prefix
            return JsonConvert.DeserializeObject<Attributes>(decryptedAttributes.ToUTF8String().Substring(4));
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
