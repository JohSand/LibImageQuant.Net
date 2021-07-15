using FsCheck.Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Zopfli.Net;

namespace LibImageQuant.Net.Tests
{
    public class Zopfli
    {
        [Property]
        public void ZipRoundTrip(byte[] bytes)
        {
            var bufferStream = new MemoryStream();
            bufferStream.Compress(bytes);

            bufferStream.Position = 0;
            using var inflater = Codec.DeflateStreamHelpers.ZlibStream(bufferStream, CompressionMode.Decompress, false, 15, -1);
            var dst = new byte[bytes.Length];
            inflater.Read(dst);
            Assert.Equal(bytes, dst);
        }
    }
}
