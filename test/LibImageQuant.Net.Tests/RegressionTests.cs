using LibImageQuant.Net.Codec;
using LibImageQuant.Net.Core;
using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Xunit;
using Decoder = LibImageQuant.Net.Codec.Decoder;

namespace LibImageQuant.Net.Tests
{
    public class RegressionTests
    {
        private static IProvideImages GetProvider(DecodedPng dec) => dec.ColorType switch
        {
            ColorType.RGBA => new ManagedProvider<ARGBFiller>(dec),
            ColorType.RGB => new ManagedProvider<RGBFiller>(dec),
            _ => throw new ArgumentException("Colortype does not support quantization"),
        };

        [Fact]
        public void Decoder_ReconstructsAverageFilterOnFirstScanline()
        {
            // The decoder used to unconditionally read a "prior row" for filter type 3
            // (Average), even on row 0 where there isn't one, computing a negative Span
            // start index and throwing. Average on the first row is an entirely ordinary
            // choice for an adaptive PNG encoder to make.
            //
            // width=2, height=1, 8-bit RGB. Raw (unfiltered) pixels are (10,20,30) and
            // (50,60,70); encoded with filter type 3 by hand below.
            var rawScanline = new byte[] { 3, 10, 20, 30, 45, 50, 55 };
            var png = BuildMinimalRgbPng(width: 2, height: 1, rawScanline);

            var dec = Decoder.ReadPng(png);

            Assert.Equal(new Color(255, 10, 20, 30), dec.GetPixel(0, 0));
            Assert.Equal(new Color(255, 50, 60, 70), dec.GetPixel(0, 1));
        }

        [Fact]
        public void PaletteData_LengthMatchesActualColorsUsed()
        {
            // QuantizationResult.PaletteData used to always expose the full fixed
            // 256-entry native buffer, ignoring the real color count libimagequant
            // returned - so output always carried a 256-color palette (padded with
            // whatever was left in unused native memory) regardless of MaxColors.
            var bytes = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "panda.png"));
            var dec = Decoder.ReadPng(bytes);
            using var quantizer = new Quantizer { MaxColors = 4, DitheringLevel = 0f };
            using var result = quantizer.Quantize(GetProvider(dec), dec.Width, dec.Height);

            Assert.True(result.PaletteData.Length <= 4,
                $"Expected at most 4 palette entries (MaxColors=4), got {result.PaletteData.Length}");

            foreach (var index in result.ImageData)
            {
                Assert.True(index < result.PaletteData.Length,
                    $"Pixel index {index} is out of range of the {result.PaletteData.Length}-entry palette");
            }
        }

        [Fact]
        public void ChunkedStream_WriteByteArrayWithNonZeroOffset_WritesCorrectSlice()
        {
            // ChunkedStream.Write(byte[], offset, count) copied `count` bytes starting at
            // `offset` into a fresh count-length buffer, then re-used `offset` as the start
            // index into *that* buffer - which is only `count` bytes long - throwing
            // whenever offset != 0.
            var cs = new ChunkedStream();
            var buffer = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            cs.Write(buffer, 3, 4);

            var ms = new MemoryStream();
            cs.CopyTo(ms);
            Assert.Equal(new byte[] { 4, 5, 6, 7 }, ms.ToArray());
        }

        [Fact]
        public void GetPixel_ThrowsArgumentException_AtRowAndColumnBoundary()
        {
            // GetPixel's bounds checks used `>` instead of `>=`, so row == Height (or
            // column == Width) fell through the intended ArgumentException and hit an
            // unguarded ArgumentOutOfRangeException from GetScanLine instead.
            // Assert.Throws<ArgumentException> requires an exact type match, so this
            // would fail again if it regressed to throwing the ArgumentOutOfRangeException
            // subclass instead.
            var bytes = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "panda.png"));
            var dec = Decoder.ReadPng(bytes);

            Assert.Throws<ArgumentException>(() => { dec.GetPixel(dec.Height, 0); });
            Assert.Throws<ArgumentException>(() => { dec.GetPixel(0, dec.Width); });
        }

        private static byte[] BuildMinimalRgbPng(int width, int height, byte[] rawScanlines)
        {
            var ihdrData = new byte[13];
            BinaryPrimitives.WriteInt32BigEndian(ihdrData, width);
            BinaryPrimitives.WriteInt32BigEndian(ihdrData.AsSpan(4), height);
            ihdrData[8] = 8; // bit depth
            ihdrData[9] = (byte)ColorType.RGB;
            ihdrData[10] = 0; // compression method
            ihdrData[11] = 0; // filter method
            ihdrData[12] = 0; // interlace method

            using var compressed = new MemoryStream();
            using (var zlib = new ZLibStream(compressed, CompressionMode.Compress, leaveOpen: true))
            {
                zlib.Write(rawScanlines);
            }

            byte[] sig = { 137, 80, 78, 71, 13, 10, 26, 10 };
            return sig
                .Concat(Chunk("IHDR", ihdrData))
                .Concat(Chunk("IDAT", compressed.ToArray()))
                .Concat(Chunk("IEND", Array.Empty<byte>()))
                .ToArray();
        }

        private static byte[] Chunk(string type, byte[] data)
        {
            var result = new byte[4 + 4 + data.Length + 4];
            BinaryPrimitives.WriteInt32BigEndian(result, data.Length);
            Encoding.ASCII.GetBytes(type).CopyTo(result.AsSpan(4));
            data.CopyTo(result.AsSpan(8));
            var crc = Crc32(result.AsSpan(4, 4 + data.Length));
            BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(8 + data.Length), crc);
            return result;
        }

        // Standard PNG/zlib CRC-32 (polynomial 0xEDB88320), per the PNG spec's own
        // reference implementation (Appendix D) - kept self-contained here rather than
        // depending on which CRC package the Codec project happens to use internally.
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

        private static uint Crc32(ReadOnlySpan<byte> data)
        {
            var c = 0xFFFFFFFFu;
            foreach (var b in data)
                c = Crc32Table[(c ^ b) & 0xFF] ^ (c >> 8);
            return c ^ 0xFFFFFFFFu;
        }
    }
}
