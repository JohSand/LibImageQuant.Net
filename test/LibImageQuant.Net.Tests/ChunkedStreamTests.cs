using LibImageQuant.Net.Codec;
using System;
using Xunit;

namespace LibImageQuant.Net.Tests
{
    public class ChunkedStreamTests
    {
        [Fact]
        public void Read_CrossesChunkBoundaries()
        {
            using var cs = new ChunkedStream();
            cs.Write(new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 }));
            cs.Write(new ReadOnlyMemory<byte>(new byte[] { 4, 5 }));
            cs.Write(new ReadOnlyMemory<byte>(new byte[] { 6, 7, 8, 9 }));

            var buffer = new byte[9];
            var read = cs.Read(buffer, 0, buffer.Length);

            Assert.Equal(9, read);
            Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }, buffer);
        }

        [Fact]
        public void Read_ReturnsFewerBytesThanRequestedAtEndOfStream()
        {
            using var cs = new ChunkedStream();
            cs.Write(new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 }));

            var buffer = new byte[10];
            var read = cs.Read(buffer, 0, buffer.Length);
            Assert.Equal(3, read);

            var secondRead = cs.Read(buffer, 0, buffer.Length);
            Assert.Equal(0, secondRead);
        }

        [Fact]
        public void Length_SumsAllWrittenChunks()
        {
            using var cs = new ChunkedStream();
            cs.Write(new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 }));
            cs.Write(new ReadOnlyMemory<byte>(new byte[] { 4, 5 }));

            Assert.Equal(5, cs.Length);
        }

        [Fact]
        public void Read_OnEmptyStream_ReturnsZero()
        {
            using var cs = new ChunkedStream();
            var buffer = new byte[4];
            Assert.Equal(0, cs.Read(buffer, 0, buffer.Length));
        }

        [Fact]
        public void Read_PartialBufferAcrossMultipleCalls_ReturnsExpectedBytes()
        {
            using var cs = new ChunkedStream();
            cs.Write(new ReadOnlyMemory<byte>(new byte[] { 10, 20, 30, 40, 50 }));

            var first = new byte[2];
            Assert.Equal(2, cs.Read(first, 0, 2));
            Assert.Equal(new byte[] { 10, 20 }, first);

            var second = new byte[3];
            Assert.Equal(3, cs.Read(second, 0, 3));
            Assert.Equal(new byte[] { 30, 40, 50 }, second);
        }
    }
}
