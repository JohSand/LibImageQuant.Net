using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibImageQuant.Net.Codec
{
    public sealed class ChunkedStream : Stream
    {
        private int _chunkIndex;
        private int _currentChunk;
        private readonly List<ReadOnlyMemory<byte>> _chunks;

        public ChunkedStream()
        {
            _chunks = new List<ReadOnlyMemory<byte>>(8);
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => _chunks.Sum(m => m.Length);

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush()
        {

        }

        public override int Read(Span<byte> buffer)
        {
            var acc = 0;
            while (true)
            {
                if (_currentChunk >= _chunks.Count)
                {
                    return acc;
                }

                var chunk = _chunks[_currentChunk];
                var slice = chunk[_chunkIndex..];
                if (slice.Length >= buffer.Length)
                {
                    slice.Span[..buffer.Length].CopyTo(buffer);
                    _chunkIndex += buffer.Length;
                    return buffer.Length;
                }
                else
                {
                    slice.Span.CopyTo(buffer);
                    _chunkIndex = 0;
                    _currentChunk++;
                    buffer = buffer[slice.Length..];
                    acc += slice.Length;
                }
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Read(new Span<byte>(buffer, offset, count));
        }

        public void Write(ReadOnlyMemory<byte> memory)
        {
            _chunks.Add(memory);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            //should not be used, but if, might as well use correctly...
            var cp = new byte[count];
            Array.Copy(buffer, offset, cp, 0, count);
            var mem = new ReadOnlyMemory<byte>(cp, offset, count);
            Write(mem);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }
    }
}