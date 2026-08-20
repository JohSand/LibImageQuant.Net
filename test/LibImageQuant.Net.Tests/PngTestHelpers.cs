using LibImageQuant.Net.Codec;
using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace LibImageQuant.Net.Tests
{
    /// <summary>
    /// Hand-rolled minimal PNG construction, kept independent of LibImageQuant.Net.Codec's own
    /// writer (Coder/Extensions) so tests built with this can genuinely exercise the decoder
    /// rather than just checking it agrees with the project's own encoder.
    /// </summary>
    internal static class PngTestHelpers
    {
        private static readonly byte[] Sig = { 137, 80, 78, 71, 13, 10, 26, 10 };

        public static byte[] BuildMinimalPng(int width, int height, ColorType colorType, byte[] rawScanlines)
        {
            var ihdrData = new byte[13];
            BinaryPrimitives.WriteInt32BigEndian(ihdrData, width);
            BinaryPrimitives.WriteInt32BigEndian(ihdrData.AsSpan(4), height);
            ihdrData[8] = 8; // bit depth
            ihdrData[9] = (byte)colorType;
            ihdrData[10] = 0; // compression method
            ihdrData[11] = 0; // filter method
            ihdrData[12] = 0; // interlace method

            using var compressed = new MemoryStream();
            using (var zlib = new ZLibStream(compressed, CompressionMode.Compress, leaveOpen: true))
            {
                zlib.Write(rawScanlines);
            }

            return Sig
                .Concat(Chunk("IHDR", ihdrData))
                .Concat(Chunk("IDAT", compressed.ToArray()))
                .Concat(Chunk("IEND", Array.Empty<byte>()))
                .ToArray();
        }

        public static byte[] Chunk(string type, byte[] data)
        {
            var result = new byte[4 + 4 + data.Length + 4];
            BinaryPrimitives.WriteInt32BigEndian(result, data.Length);
            Encoding.ASCII.GetBytes(type).CopyTo(result.AsSpan(4));
            data.CopyTo(result.AsSpan(8));
            var crc = Crc32(result.AsSpan(4, 4 + data.Length));
            BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(8 + data.Length), crc);
            return result;
        }

        /// <summary>
        /// Forward-applies a single PNG scanline filter (0=None, 1=Sub, 2=Up, 3=Average,
        /// 4=Paeth) to raw pixel bytes, per the PNG spec (section 9.2). Written independently
        /// of DecodedPng.ApplyPngFilters's reconstruction logic - including using the spec's
        /// own (differently-shaped, algebraically equivalent) Paeth formula - so tests built
        /// on this are a genuine independent check, not a tautology against the same bug.
        /// </summary>
        public static byte[] ApplyFilter(byte filterType, byte[] rawRow, byte[] priorRow, int bpp)
        {
            var filtered = new byte[rawRow.Length];
            for (var i = 0; i < rawRow.Length; i++)
            {
                int raw = rawRow[i];
                int a = i >= bpp ? rawRow[i - bpp] : 0;
                int b = priorRow?[i] ?? 0;
                int c = i >= bpp ? (priorRow?[i - bpp] ?? 0) : 0;

                int predictor = filterType switch
                {
                    0 => 0,
                    1 => a,
                    2 => b,
                    3 => (a + b) / 2,
                    4 => Paeth(a, b, c),
                    _ => throw new ArgumentOutOfRangeException(nameof(filterType)),
                };
                filtered[i] = unchecked((byte)(raw - predictor));
            }
            return filtered;
        }

        private static int Paeth(int a, int b, int c)
        {
            var p = a + b - c;
            var pa = Math.Abs(p - a);
            var pb = Math.Abs(p - b);
            var pc = Math.Abs(p - c);
            if (pa <= pb && pa <= pc) return a;
            if (pb <= pc) return b;
            return c;
        }

        // Standard PNG/zlib CRC-32 (polynomial 0xEDB88320), per the PNG spec's own reference
        // implementation (Appendix D) - kept self-contained here rather than depending on
        // which CRC package the Codec project happens to use internally.
        private static readonly uint[] Crc32Table = BuildCrc32Table();

        private static uint[] BuildCrc32Table()
        {
            var table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                var c = n;
                for (var k = 0; k < 8; k++)
                    c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
                table[n] = c;
            }
            return table;
        }

        public static uint Crc32(ReadOnlySpan<byte> data)
        {
            var c = 0xFFFFFFFFu;
            foreach (var b in data)
                c = Crc32Table[(c ^ b) & 0xFF] ^ (c >> 8);
            return c ^ 0xFFFFFFFFu;
        }
    }
}
