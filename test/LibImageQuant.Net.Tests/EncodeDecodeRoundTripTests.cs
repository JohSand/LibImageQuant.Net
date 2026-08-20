using LibImageQuant.Net.Codec;
using LibImageQuant.Net.Core;
using System;
using System.IO;
using Xunit;
using Decoder = LibImageQuant.Net.Codec.Decoder;

namespace LibImageQuant.Net.Tests
{
    /// <summary>
    /// Encodes a quantized image with Coder and decodes that encoder's own output back with
    /// Decoder, checking every pixel against the QuantizationResult that produced it. This is
    /// what originally surfaced two real bugs: GetPixel couldn't read indexed-color (PLTE)
    /// output at all, and the tRNS chunk silently dropped transparency for palette entries
    /// that landed after the first fully-opaque one (libimagequant doesn't guarantee the
    /// alpha-sorted palette Coder used to assume).
    /// </summary>
    public class EncodeDecodeRoundTripTests
    {
        private static IProvideImages GetProvider(DecodedPng dec) => dec.ColorType switch
        {
            ColorType.RGBA => new ManagedProvider<ARGBFiller>(dec),
            ColorType.RGB => new ManagedProvider<RGBFiller>(dec),
            _ => throw new ArgumentException("Colortype does not support quantization"),
        };

        [Theory]
        [InlineData("panda.png", 64)]
        [InlineData("image06.png", 32)]
        // MaxColors=64 against this specific image is the case that produces a palette
        // libimagequant does NOT sort fully-opaque-last - the regression case for the tRNS bug.
        [InlineData("frau-mode-vintage-illustration-1622417428ANN.png", 64)]
        [InlineData("panda.png", 256)]
        public void EncodedPng_DecodesBackToExactlyTheQuantizedPixels(string fileName, int maxColors)
        {
            var bytes = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), fileName));
            var dec = Decoder.ReadPng(bytes);
            using var quantizer = new Quantizer { DitheringLevel = 0f, MaxColors = maxColors };
            using var result = quantizer.Quantize(GetProvider(dec), dec.Width, dec.Height);

            var encoded = new Coder(dec.Width, dec.Height).CreateBytes(result);
            var redecoded = Decoder.ReadPng(encoded);

            Assert.Equal(ColorType.PLTE, redecoded.ColorType);
            Assert.Equal(dec.Width, redecoded.Width);
            Assert.Equal(dec.Height, redecoded.Height);

            for (var row = 0; row < dec.Height; row++)
            {
                for (var col = 0; col < dec.Width; col++)
                {
                    var expectedIndex = result.ImageData[(row * dec.Width) + col];
                    var expected = result.PaletteData[expectedIndex];
                    var actual = redecoded.GetPixel(row, col);
                    Assert.True(expected.Equals(actual),
                        $"Pixel ({row},{col}): expected A={expected.Alpha},R={expected.Red},G={expected.Green},B={expected.Blue} " +
                        $"but decoded A={actual.Alpha},R={actual.Red},G={actual.Green},B={actual.Blue}");
                }
            }
        }
    }
}
