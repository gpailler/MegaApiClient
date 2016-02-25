using System.IO;
using System;

namespace CG.Web.MegaApiClient
{
    internal class ProgressionStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly IProgress<double> _progress;

        private long _chunkSize = 0;

        public ProgressionStream(Stream baseStream, IProgress<double> progress)
        {
            this._baseStream = baseStream;
            this._progress = progress;
        }

        public override int Read(byte[] array, int offset, int count)
        {
            int bytesRead = this._baseStream.Read(array, offset, count);
            this.ReportProgress(bytesRead);

            return bytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            this._baseStream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            this._progress.Report(100);
        }

        #region Forwards

        public override void Flush()
        {
            this._baseStream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return this._baseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            this._baseStream.SetLength(value);
        }

        public override bool CanRead => this._baseStream.CanRead;

        public override bool CanSeek => this._baseStream.CanSeek;

        public override bool CanWrite => this._baseStream.CanWrite;

        public override long Length => this._baseStream.Length;

        public override long Position
        {
            get { return this._baseStream.Position; }
            set { this._baseStream.Position = value; }
        }

        #endregion

        private void ReportProgress(int count)
        {
            this._chunkSize += count;
            if (this._chunkSize >= MegaApiClient.ReportProgressChunkSize)
            {
                this._chunkSize = 0;
                this._progress.Report(this.Position / (double)this.Length * 100);
            }
        }
    }
}