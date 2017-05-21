namespace CG.Web.MegaApiClient
{
  using System;
  using System.IO;
  using System.Text;

  internal static class Extensions
  {
    private static readonly DateTime EpochStart = new DateTime(1970, 1, 1, 0, 0, 0, 0);

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

    public static void CopyTo(this Stream inputStream, Stream outputStream, int bufferSize)
    {
      // For .Net 3.5
      // From http://referencesource.microsoft.com/#mscorlib/system/io/stream.cs,98ac7cf3acb04bb1
      byte[] buffer = new byte[bufferSize];
      int read;
      while ((read = inputStream.Read(buffer, 0, buffer.Length)) != 0)
      {
        outputStream.Write(buffer, 0, read);
      }
    }

    public static DateTime ToDateTime(this long seconds)
    {
      return EpochStart.AddSeconds(seconds).ToLocalTime();
    }

    public static long ToEpoch(this DateTime datetime)
    {
      return (long)datetime.ToUniversalTime().Subtract(EpochStart).TotalSeconds;
    }

    public static long DeserializeToLong(this byte[] data, int index, int length)
    {
      byte p = data[index];

      long result = 0;

      if ((p > sizeof(UInt64)) || (p >= length))
      {
        throw new ArgumentException("Invalid value");
      }


      while (p > 0)
      {
        result = (result << 8) + data[index + p--];
      }

      return result;
    }

    public static byte[] SerializeToBytes(this long data)
    {
      byte[] result = new byte[sizeof(long) + 1];

      byte p = 0;
      while (data != 0)
      {
        result[++p] = (byte)data;
        data >>= 8;
      }

      result[0] = p;
      Array.Resize(ref result, result[0] + 1);

      return result;
    }
  }
}