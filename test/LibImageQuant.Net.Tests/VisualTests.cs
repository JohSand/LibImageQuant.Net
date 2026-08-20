using LibImageQuant.Net.Codec;
using LibImageQuant.Net.Core;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.IO;
using System.Threading.Tasks;
using VerifyXunit;
using Decoder = LibImageQuant.Net.Codec.Decoder;

namespace LibImageQuant.Net.Tests
{
    /// <summary>
    /// Snapshot tests for the actual quantized+encoded PNG, via Verify
    /// (https://github.com/VerifyTests/Verify) and Verify.ImageSharp. Unlike the
    /// pixel-tolerance/exact-match assertions elsewhere in this suite, these store the real
    /// output PNG next to the test (Snapshots/*.verified.png) so a person can just open and
    /// look at it, and fail loudly - with a *.received.png sitting alongside it for a
    /// side-by-side look - the moment the pipeline's actual output changes for any reason (an
    /// algorithm change upstream, a setting change here, a regression), not just when a
    /// numeric invariant breaks.
    ///
    /// Comparison is via SSIM (see ModuleInit.cs), decoding our own PLTE output through a
    /// completely independent decoder (ImageSharp) as a side effect. SSIM tolerance is what
    /// lets this run on every CI leg rather than being restricted to one fixed platform/core
    /// count - see ModuleInit.cs for why exact comparison couldn't.
    ///
    /// To accept a changed snapshot after confirming it's correct: delete the matching
    /// .verified.png and rename the .received.png to .verified.png (or use a Verify-aware
    /// IDE plugin / diff tool - see https://github.com/VerifyTests/Verify#snapshot-management).
    /// </summary>
    public class VisualTests
    {
        private static IProvideImages GetProvider(DecodedPng dec) => dec.ColorType switch
        {
            ColorType.RGBA => new ManagedProvider<ARGBFiller>(dec),
            ColorType.RGB => new ManagedProvider<RGBFiller>(dec),
            _ => throw new ArgumentException("Colortype does not support quantization"),
        };

        [Theory]
        [InlineData("panda.png", 32)]
        [InlineData("image06.png", 16)]
        [InlineData("frau-mode-vintage-illustration-1622417428ANN.png", 64)]
        public Task QuantizedOutput_MatchesApprovedSnapshot(string fileName, int maxColors)
        {
            var bytes = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), fileName));
            using var dec = Decoder.ReadPng(bytes);
            using var quantizer = new Quantizer { DitheringLevel = 0.6f, MaxColors = maxColors };
            using var result = quantizer.Quantize(GetProvider(dec), dec.Width, dec.Height);
            var encoded = new Coder(dec.Width, dec.Height).CreateBytes(result);

            using var image = Image.Load<Rgba32>(encoded);
            return Verifier.Verify(image)
                .UseDirectory("Snapshots")
                .UseParameters(fileName, maxColors);
        }
    }
}
