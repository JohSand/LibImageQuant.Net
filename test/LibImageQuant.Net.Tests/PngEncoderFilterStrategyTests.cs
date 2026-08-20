using LibImageQuant.Net.Codec;
using LibImageQuant.Net.Core;
using System;
using System.IO;
using Xunit;
using Decoder = LibImageQuant.Net.Codec.Decoder;

namespace LibImageQuant.Net.Tests
{
    /// <summary>
    /// Round-trips Coder's filter strategies (MinSum/Entropy per-row adaptive selection, and
    /// Adaptive's zopflipng-style whole-image search) back through Decoder's independent
    /// unfiltering logic, and checks that Adaptive actually earns its cost against the
    /// previous always-None default.
    /// </summary>
    public class PngEncoderFilterStrategyTests
    {
        private static IProvideImages GetProvider(DecodedPng dec) => dec.ColorType switch
        {
            ColorType.RGBA => new ManagedProvider<ARGBFiller>(dec),
            ColorType.RGB => new ManagedProvider<RGBFiller>(dec),
            _ => throw new ArgumentException("Colortype does not support quantization"),
        };

        [Theory]
        [InlineData(PngFilterStrategy.None)]
        [InlineData(PngFilterStrategy.Sub)]
        [InlineData(PngFilterStrategy.Up)]
        [InlineData(PngFilterStrategy.Average)]
        [InlineData(PngFilterStrategy.Paeth)]
        [InlineData(PngFilterStrategy.MinSum)]
        [InlineData(PngFilterStrategy.Entropy)]
        [InlineData(PngFilterStrategy.Adaptive)]
        public void EncodedPng_DecodesBackToExactlyTheQuantizedPixels_RegardlessOfFilterStrategy(PngFilterStrategy strategy)
        {
            var bytes = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "panda.png"));
            using var dec = Decoder.ReadPng(bytes);
            using var quantizer = new Quantizer { DitheringLevel = 0f, MaxColors = 64 };
            using var result = quantizer.Quantize(GetProvider(dec), dec.Width, dec.Height);

            var encoded = new Coder(dec.Width, dec.Height, filterStrategy: strategy).CreateBytes(result);
            using var redecoded = Decoder.ReadPng(encoded);

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
                        $"Pixel ({row},{col}) with strategy {strategy}: expected A={expected.Alpha},R={expected.Red},G={expected.Green},B={expected.Blue} " +
                        $"but decoded A={actual.Alpha},R={actual.Red},G={actual.Green},B={actual.Blue}");
                }
            }
        }

        [Theory]
        [InlineData("panda.png", 64)]
        [InlineData("frau-mode-vintage-illustration-1622417428ANN.png", 64)]
        [InlineData("image06.png", 32)]
        public void Adaptive_NeverProducesLargerOutputThanNone(string fileName, int maxColors)
        {
            var bytes = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), fileName));
            using var dec = Decoder.ReadPng(bytes);
            using var quantizer = new Quantizer { DitheringLevel = 0f, MaxColors = maxColors };
            using var result = quantizer.Quantize(GetProvider(dec), dec.Width, dec.Height);

            var none = new Coder(dec.Width, dec.Height, filterStrategy: PngFilterStrategy.None).CreateBytes(result);
            var adaptive = new Coder(dec.Width, dec.Height, filterStrategy: PngFilterStrategy.Adaptive).CreateBytes(result);

            Assert.True(adaptive.Length <= none.Length,
                $"Adaptive ({adaptive.Length} bytes) should be no larger than None ({none.Length} bytes)");
        }
    }
}
