using System;
using System.IO;

namespace Zopfli.Net
{
    public sealed class ZopfliStream : Stream
    {
        private byte[] _buffer;
        private int _position = 0;
        private readonly Stream _innerStream;
        private readonly bool _leaveOpen;


        public ZopfliStream(Stream inner, bool leaveOpen = false, int capacity = 0)
        {
            _innerStream = inner;
            _leaveOpen = leaveOpen;
            _buffer = capacity != 0 ? new byte[capacity] : Array.Empty<byte>();
        }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override void Flush()
        {
            // Idempotent: nothing new has been written since the last flush (or since
            // construction), so there's nothing to compress. Without this, calling Flush twice
            // - including once explicitly and once automatically from Dispose - would compress
            // and write the same buffered bytes a second time.
            if (_position == 0)
                return;

            _innerStream.Compress(new ReadOnlySpan<byte>(_buffer, 0, _position));
            _innerStream.Flush();
            _position = 0;
        }

        // public override Task FlushAsync(CancellationToken cancellationToken) => _innerStream.FlushAsync(cancellationToken);

        public override long Length => _innerStream.Length;

        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException("Reading is not supported");

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException("Seeking is not supported");

        public override void SetLength(long value) => throw new NotSupportedException("Seeking is not supported");

        public override void Write(ReadOnlySpan<byte> data)
        {
            EnsureCapacity(data.Length);
            data.CopyTo(new Span<byte>(_buffer, _position, data.Length));
            _position += data.Length;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            EnsureCapacity(count);
            Buffer.BlockCopy(buffer, offset, _buffer, _position, count);
            _position += count;
        }

        private void EnsureCapacity(int capacity)
        {
            if (capacity + _position <= _buffer.Length)
                return;

            var newBuffer = new byte[_buffer.Length + capacity];
            if (_position > 0)
            {
                Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _position);
            }

            _buffer = newBuffer;
        }

        protected override void Dispose(bool disposing)
        {
            // Every BCL compression stream (DeflateStream, GZipStream, ZLibStream) flushes
            // pending writes on Dispose; without this, `using (var zs = new ZopfliStream(s))
            // { zs.Write(data); }` silently wrote nothing at all if the caller didn't also
            // remember an explicit Flush() first.
            if (disposing)
                Flush();

            base.Dispose(disposing);
            if (!_leaveOpen)
                _innerStream.Dispose();
        }
    }
}
