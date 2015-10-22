#region License

/*
The MIT License (MIT)

Copyright (c) 2015 Gregoire Pailler

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

#endregion

using System;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace CG.Web.MegaApiClient
{
    internal class Crypto
    {
        private static readonly Rijndael RijndaelCbc;
        private static readonly Rijndael RijndaelEcb;
        private static readonly byte[] DefaultIv = new byte[16];

        static Crypto()
        {
            RijndaelCbc = Rijndael.Create();
            RijndaelCbc.Padding = PaddingMode.None;
            RijndaelCbc.Mode = CipherMode.CBC;

            RijndaelEcb = Rijndael.Create();
            RijndaelEcb.Padding = PaddingMode.None;
            RijndaelEcb.Mode = CipherMode.ECB;
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

        public static byte[] EncryptAesEcb(byte[] data, byte[] key)
        {
            using (ICryptoTransform encryptor = RijndaelEcb.CreateEncryptor(key, DefaultIv))
            {
                return encryptor.TransformFinalBlock(data, 0, data.Length);
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
            try
            {
                return JsonConvert.DeserializeObject<Attributes>(decryptedAttributes.ToUTF8String().Substring(4));
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
