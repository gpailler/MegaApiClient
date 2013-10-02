#region License

/*
The MIT License (MIT)

Copyright (c) 2013 Gregoire Pailler

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
using System.Text;

namespace CG.Web.MegaApiClient
{
    internal static class Extensions
    {
        public static byte[] ToBytes(this uint[] data)
        {
            byte[] result = new byte[data.Length * 4];

            for (int idx = 0; idx < data.Length; idx++)
            {
                byte[] packet = BitConverter.GetBytes(data[idx]);

                for (int packetIdx = 0; packetIdx < 4; packetIdx++)
                {
                    // Reverse order to convert from bigendian
                    result[idx * 4 + packetIdx] = packet[3 - packetIdx];
                }
            }

            return result;
        }
        
        public static string ToBase64(this byte[] data)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Convert.ToBase64String(data));
            sb.Replace('+', '-');
            sb.Replace('/', '_');
            sb.Replace("=", string.Empty);
            
            return sb.ToString();
        }

        public static byte[] FromBase64(this string data)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(data);
            sb.Append(string.Empty.PadRight((4 - data.Length % 4) % 4, '='));
            sb.Replace('-', '+');
            sb.Replace('_', '/');
            sb.Replace(",", string.Empty);

            return Convert.FromBase64String(sb.ToString());
        }

        public static string ToUTF8String(this byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }

        public static byte[] ToBytes(this string data)
        {
            return Encoding.UTF8.GetBytes(data);
        }

        public static T[] CopySubArray<T>(this T[] source, int length, int offset = 0)
        {
            T[] result = new T[length];
            while (--length >= 0)
            {
                if (source.Length > offset + length)
                {
                    result[length] = source[offset + length];
                }
            }

            return result;
        }

        public static BigInteger FromMPINumber(this byte[] data)
        {
            // First 2 bytes defines the size of the component
            int dataLength = (data[0] * 256 + data[1] + 7) / 8;

            byte[] result = new byte[dataLength];
            Array.Copy(data, 2, result, 0, result.Length);

            return new BigInteger(result);
        }
    }
}
