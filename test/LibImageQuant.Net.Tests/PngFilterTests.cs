using LibImageQuant.Net.Codec;
using LibImageQuant.Net.Core;
using System;
using Xunit;
using Decoder = LibImageQuant.Net.Codec.Decoder;

namespace LibImageQuant.Net.Tests
{
    /// <summary>
    /// Exercises DecodedPng.ApplyPngFilters against every PNG scanline filter type (0=None,
    /// 1=Sub, 2=Up, 3=Average, 4=Paeth), both on the first row (no prior row - the exact
    /// class of bug found in the Average filter, which crashed there) and on a later row
    /// (general case, referencing an actual prior row). Expected bytes are computed by
    /// PngTestHelpers.ApplyFilter, an independent implementation of the PNG spec's forward
    /// filters, not by copying the decoder's own logic.
    /// </summary>
    public class PngFilterTests
    {
        // Two arbitrary, distinct RGB pixels - chosen so no channel accidentally repeats a
        // value from another channel/pixel, which would mask a transposition bug.
        private static readonly byte[] Pixel0 = { 12, 34, 56 };
        private static readonly byte[] Pixel1 = { 200, 150, 90 };
        private const int Bpp = 3;

        [Theory]
        [InlineData((byte)0)] // None
        [InlineData((byte)1)] // Sub
        [InlineData((byte)2)] // Up
        [InlineData((byte)3)] // Average
        [InlineData((byte)4)] // Paeth
        public void Decoder_ReconstructsFilterType_OnFirstRow(byte filterType)
        {
            var rawRow = Concat(Pixel0, Pixel1);
            var filtered = PngTestHelpers.ApplyFilter(filterType, rawRow, priorRow: null, Bpp);

            var png = PngTestHelpers.BuildMinimalPng(width: 2, height: 1, ColorType.RGB,
                Concat(new[] { filterType }, filtered));

            var dec = Decoder.ReadPng(png);

            Assert.Equal(new Color(255, Pixel0[0], Pixel0[1], Pixel0[2]), dec.GetPixel(0, 0));
            Assert.Equal(new Color(255, Pixel1[0], Pixel1[1], Pixel1[2]), dec.GetPixel(0, 1));
        }

        [Theory]
        [InlineData((byte)1)] // Sub
        [InlineData((byte)2)] // Up
        [InlineData((byte)3)] // Average
        [InlineData((byte)4)] // Paeth
        public void Decoder_ReconstructsFilterType_OnSubsequentRow(byte filterType)
        {
            // Row 0 (None) establishes a real, non-zero prior row for row 1 to reference,
            // so this exercises the "actually read the prior row" path rather than the
            // row-0 special case covered above.
            var row0Raw = Concat(Pixel0, Pixel1);
            var row1Raw = Concat(new byte[] { 5, 250, 128 }, new byte[] { 77, 3, 199 });

            var row0Filtered = PngTestHelpers.ApplyFilter(0, row0Raw, priorRow: null, Bpp);
            var row1Filtered = PngTestHelpers.ApplyFilter(filterType, row1Raw, row0Raw, Bpp);

            var scanlines = Concat(
                Concat(new byte[] { 0 }, row0Filtered),
                Concat(new[] { filterType }, row1Filtered));
            var png = PngTestHelpers.BuildMinimalPng(width: 2, height: 2, ColorType.RGB, scanlines);

            var dec = Decoder.ReadPng(png);

            Assert.Equal(new Color(255, Pixel0[0], Pixel0[1], Pixel0[2]), dec.GetPixel(0, 0));
            Assert.Equal(new Color(255, Pixel1[0], Pixel1[1], Pixel1[2]), dec.GetPixel(0, 1));
            Assert.Equal(new Color(255, 5, 250, 128), dec.GetPixel(1, 0));
            Assert.Equal(new Color(255, 77, 3, 199), dec.GetPixel(1, 1));
        }

        private static byte[] Concat(byte[] a, byte[] b)
        {
            var result = new byte[a.Length + b.Length];
            a.CopyTo(result, 0);
            b.CopyTo(result, a.Length);
            return result;
        }
    }
}
