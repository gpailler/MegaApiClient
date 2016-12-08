using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CG.Web.MegaApiClient
{
    /// <summary>
    /// https://github.com/meganz/sdk/blob/master/src/serialize64.cpp
    /// </summary>
    public static class Serialize64
    {
        public static int Serialize(byte[] b, int startIndex, UInt64 v)
        {
            byte p = 0;
            while (v != 0)
            {
                b[startIndex + ++p] = (byte)v;
                v >>= 8;
            }

            return (b[startIndex] = p) + 1;
        }

        public static int Unserialize(byte[] b, int startIndex, int blen, out UInt64 v)
        {
            byte p = b[startIndex];

            v = 0;

            if ((p > sizeof(UInt64)) || (p >= blen))
            {
                return -1;
            }


            while (p > 0)
            {
                v = (v << 8) + b[startIndex + (int)p--];
            }

            return b[startIndex] + 1;
        }
    }
}
