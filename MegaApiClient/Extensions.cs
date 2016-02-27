namespace CG.Web.MegaApiClient
{
  using System;
  using System.Text;

  internal static class Extensions
  {
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
