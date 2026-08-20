using LibImageQuant.Net.Codec;
using LibImageQuant.Net.Core;
using System;
using System.IO;
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
            var png = PngTestHelpers.BuildMinimalPng(width: 2, height: 1, ColorType.RGB, rawScanline);

            using var dec = Decoder.ReadPng(png);

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
            using var dec = Decoder.ReadPng(bytes);
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
            using var dec = Decoder.ReadPng(bytes);

            Assert.Throws<ArgumentException>(() => { dec.GetPixel(dec.Height, 0); });
            Assert.Throws<ArgumentException>(() => { dec.GetPixel(0, dec.Width); });
        }
    }
}
