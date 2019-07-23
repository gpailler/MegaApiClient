namespace CG.Web.MegaApiClient {
  using System;
  using System.IO;

  public class SeekableReadStream : Stream {
        public delegate int Reader(byte[] buffer, int bufferOffset = 0, long offset = 0,
            int count = -1);

        readonly Reader reader;

        public SeekableReadStream(long length, Reader reader) {
            Length = length;
            this.reader = reader;
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length { get; }

        public override long Position { get; set; }

        public override void Flush() {
            // Intentionally doing nothing.
        }

        public override long Seek(long offset, SeekOrigin origin) {
            switch (origin) {
                case SeekOrigin.Begin:
                    Position = offset;
                    break;
                case SeekOrigin.Current:
                    Position += offset;
                    break;
                case SeekOrigin.End:
                    Position = Length + offset;
                    break;
            }

            if (Position < 0) {
                throw new ArgumentException(nameof(offset));
            }

            return Position;
        }

        public override void SetLength(long value) =>
            throw new NotSupportedException($"{nameof(SeekableReadStream)} is not writable.");

        public override int Read(byte[] buffer, int offset, int count) {
            if (buffer == null) {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0) {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            count = (int) Math.Min(count, Length - Position);

            if (buffer.Length - offset < count) {
                throw new ArgumentException();
            }

            if (Position >= Length) {
                return 0;
            }

            var readCount = reader(buffer, offset, Position, count);
            Position += readCount;
            return readCount;
        }

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException($"{nameof(SeekableReadStream)} is not writable.");
    }
}
