using LibImageQuant.Net.Codec;
using LibImageQuant.Net.Core;
using System;
using System.IO;
using System.Threading.Tasks;
using VerifyXunit;
using Xunit;
using Decoder = LibImageQuant.Net.Codec.Decoder;

namespace LibImageQuant.Net.Tests
{
    /// <summary>
    /// Snapshot tests for the actual quantized+encoded PNG bytes, via Verify
    /// (https://github.com/VerifyTests/Verify). Unlike the pixel-tolerance/exact-match
    /// assertions elsewhere in this suite, these store the real output PNG next to the test
    /// (Snapshots/*.verified.png) so a person can just open and look at it, and fail loudly -
    /// with a *.received.png sitting alongside it for a side-by-side look - the moment the
    /// pipeline's actual output changes for any reason (an algorithm change upstream, a
    /// setting change here, a regression), not just when a numeric invariant breaks.
    ///
    /// To accept a changed snapshot after confirming it's correct: delete the matching
    /// .verified.png and rename the .received.png to .verified.png (or use a Verify-aware
    /// IDE plugin / diff tool - see https://github.com/VerifyTests/Verify#snapshot-management).
    ///
    /// Quantization here is fully deterministic on a given machine (confirmed: 10 repeated
    /// runs of the same input produced byte-identical output), but libimagequant v4 uses
    /// multi-threading, whose work-stealing order can in principle affect floating-point
    /// accumulation order differently across CPU core counts or architectures - so these are
    /// intentionally not run on the Windows CI leg, only linux-x64, to avoid a spurious
    /// cross-platform byte-diff. If this ever turns out to be flaky even on a single fixed
    /// runner, switch to a pixel-tolerance comparison instead of the exact byte match Verify
    /// does by default.
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
        [Trait("Category", "Visual")]
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

            return Verifier.Verify(encoded, extension: "png")
                .UseDirectory("Snapshots")
                .UseParameters(fileName, maxColors);
        }
    }
}
