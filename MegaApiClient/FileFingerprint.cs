using DamienG.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CG.Web.MegaApiClient
{
    /// <summary>
    /// https://github.com/meganz/sdk/blob/d4b462efc702a9c645e90c202b57e14da3de3501/src/filefingerprint.cpp
    /// </summary>
    internal struct FileFingerprint : IEquatable<FileFingerprint>
    {
        const int CRCArrayLength = 4;
        const int CRCSize = sizeof(uint) * CRCArrayLength;
        const int FingerprintMaxSize = CRCSize + 1 + sizeof(long);

        public const int MAXFULL = 8192;
        
        private long size;
        private ulong mtime;
        private uint[] crc;
        private bool isvalid;

        public long Size { get { return size; } }
        public ulong ModificationTimeStamp { get { return mtime; } }
        public uint[] CRC { get { return crc; } }
        public bool IsValid { get { return isvalid; } }

        public DateTime ModificationDate
        {
            get { return Node.OriginalDateTime.AddSeconds(mtime).ToLocalTime(); }
        }

        public FileFingerprint(UInt32[] crc, DateTime? modificationDate = null, long size = -1L)
        {
            this.size = size;
            this.crc = crc;
            mtime = modificationDate.HasValue ? (ulong)modificationDate.Value.ToUniversalTime().Subtract(Node.OriginalDateTime).TotalSeconds : 0;
            isvalid = true;
        }

        public bool Equals(FileFingerprint other)
        {
            if (size != other.size)
                return false;

            if (mtime != other.mtime)
                return false;

            // FileFingerprints not fully available - give it the benefit of the doubt
            if (!isvalid || !other.isvalid)
            {
                return true;
            }

            bool crcEqual = crc != null ? Enumerable.SequenceEqual(this.crc, other.crc) : false;
            return crcEqual;
        }

        public bool Serialize(Stream stream)
        {
            var streamWriter = new StreamWriter(stream);
            streamWriter.Write(size);
            streamWriter.Write(mtime);
            streamWriter.Write(crc);
            streamWriter.Write(isvalid);

            return true;
        }

        public static FileFingerprint Unserialize(Stream stream)
        {
            var ff = new FileFingerprint();

            byte[] buffer = new byte[stream.Length];

            var streamReader = new BinaryReader(stream);
            
            ff.size = streamReader.ReadInt64();
            ff.mtime = streamReader.ReadUInt64();
            var crcBytes = streamReader.ReadBytes(CRCSize);
            uint[] crcArr = new uint[CRCArrayLength];
            Buffer.BlockCopy(crcBytes, 0, crcArr, 0, crcBytes.Length);
            ff.crc = crcArr;
            ff.isvalid = streamReader.ReadBoolean();
            return ff;
        }

        public bool GetFingerprint(Stream stream, DateTime? lastWriteTime)
        {
            size = -1L;
            mtime = 0;
            isvalid = false;
            crc = new uint[CRCArrayLength];

            if (stream.Position != 0)
            {
                if (!stream.CanSeek)
                    throw new InvalidOperationException("Failed to read file stream from start.");
                stream.Seek(0, SeekOrigin.Begin);
            }
            bool ignoreMTime = !lastWriteTime.HasValue;
            
            bool changed = false;
            uint[] newCrc = new uint[CRCArrayLength];
            byte[] newCrcBuffer = new byte[CRCSize];
            uint crcVal = 0;

            if (!ignoreMTime)
            {
                var fileMTime = (ulong)lastWriteTime.Value.ToUniversalTime().Subtract(Node.OriginalDateTime).TotalSeconds;
                if (fileMTime != mtime)
                {
                    mtime = fileMTime;
                    changed = !ignoreMTime;
                }
            }

            if (stream.Length != size)
            {
                size = stream.Length;
                changed = true;
            }

            if (size < 0)
            {
                size = -1;
                return true;
            }

            if (size <= CRCSize)
            {
                // tiny file: read verbatim, NUL pad
                if (0 == stream.Read(newCrcBuffer, 0, (int)size))
                {
                    size = -1;
                    return true;
                }

                Buffer.BlockCopy(newCrcBuffer, 0, newCrc, 0, newCrcBuffer.Length);
            }
            else if (size <= MAXFULL)
            {
                // small file: full coverage, four full CRC32s
                byte[] fileBuffer = new byte[size];
                int read = 0;
                while ((read += stream.Read(fileBuffer, read, (int)size - read)) < size);
                for (int i = 0; i < newCrc.Length; i++)
                {
                    int begin = (int)(i * size / newCrc.Length);
                    int end = (int)((i + 1) * size / newCrc.Length);

                    using (var crc32Hasher = new Crc32(Crypto.CryptoPPCRC32Polynomial, Crc32.DefaultSeed))
                    {
                        crc32Hasher.TransformBlock(fileBuffer, begin, end - begin, null, 0);
                        crc32Hasher.TransformFinalBlock(fileBuffer, 0, 0);
                        var crcValBytes = crc32Hasher.Hash;
                        crcVal = BitConverter.ToUInt32(crcValBytes, 0);
                    }
                    newCrc[i] = crcVal;
                }

                Buffer.BlockCopy(newCrc, 0, newCrcBuffer, 0, newCrcBuffer.Length);
            }
            else
            {
                // large file: sparse coverage, four sparse CRC32s
                byte[] block = new byte[4 * CRCSize];
                uint blocks = (uint)(MAXFULL / (block.Length * CRCArrayLength));
                long current = 0;

                for (uint i = 0; i < CRCArrayLength; i++)
                {
                    using (var crc32Hasher = new Crc32(Crypto.CryptoPPCRC32Polynomial, Crc32.DefaultSeed))
                    {
                        for (uint j = 0; j < blocks; j++)
                        {
                            long offset = (size - block.Length) * (i * blocks + j) / (CRCArrayLength * blocks - 1);

                            stream.Seek(offset - current, SeekOrigin.Current);
                            current += (offset - current);

                            int blockWritten = stream.Read(block, 0, block.Length);
                            current += blockWritten;
                            crc32Hasher.TransformBlock(block, 0, blockWritten, null, 0);
                        }

                        crc32Hasher.TransformFinalBlock(block, 0, 0);
                        var crc32ValBytes = crc32Hasher.Hash;
                        crcVal = BitConverter.ToUInt32(crc32ValBytes, 0);

                    }
                    newCrc[i] = crcVal;
                }

                Buffer.BlockCopy(newCrc, 0, newCrcBuffer, 0, CRCSize);
            }

            if (!Enumerable.SequenceEqual(crc, newCrc))
            {
                crc = newCrc;
                changed = true;
            }

            if (!isvalid)
            {
                changed = true;
                isvalid = true;
            }

            return changed;
        }

        public byte[] SerializeFingerprint()
        {
            byte[] fingerprintBuffer = new byte[FingerprintMaxSize];
            Buffer.BlockCopy(crc, 0, fingerprintBuffer, 0, CRCSize);

            int mtimeBytesTaken = Serialize64.Serialize(fingerprintBuffer, CRCSize, mtime);
            Array.Resize(ref fingerprintBuffer, fingerprintBuffer.Length - (sizeof(long) + 1) + mtimeBytesTaken);
            return fingerprintBuffer;
        }

        public bool UnserializeFingerprint(byte[] buffer)
        {
            size = -1L;
            mtime = 0;
            isvalid = false;
            crc = new uint[CRCArrayLength];

            if (buffer.Length < CRCSize + 1)
            {
                return false;
            }

            Buffer.BlockCopy(buffer, 0, crc, 0, CRCSize);
            if (Serialize64.Unserialize(buffer, CRCSize, buffer.Length - CRCSize, out mtime) < 0)
            {
                return false;
            }
            isvalid = true;
            return true;
        }
    }
}
