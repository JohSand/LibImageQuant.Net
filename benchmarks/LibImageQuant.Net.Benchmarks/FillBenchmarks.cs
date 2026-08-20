using BenchmarkDotNet.Attributes;
using LibImageQuant.Net.Codec;
using LibImageQuant.Net.Core;
using System;
using System.IO;
using Decoder = LibImageQuant.Net.Codec.Decoder;

namespace LibImageQuant.Net.Benchmarks
{
    /// <summary>
    /// Isolates ARGBFiller.Fill (the per-row RGBA byte-scanline -> Color[] channel shuffle fed
    /// to libimagequant via the row callback), without going through native quantization at
    /// all, to find out whether it's worth optimizing before touching it.
    ///
    /// Measured result: filling every row of the 1920x1920 fixture (a full image's worth of
    /// this loop) takes ~3.1ms, against a ~445ms full quantize+encode pipeline for the same
    /// image (QuantizeAndEncodeBenchmarks) - under 1% of end-to-end time. A SIMD channel-swizzle
    /// rewrite of Fill (the natural next idea - it's a textbook byte-shuffle) was deliberately
    /// not pursued on that basis: even eliminating this loop entirely wouldn't be measurable
    /// end-to-end, and hand-rolled SIMD is real complexity/platform-risk to take on for it.
    /// </summary>
    [MemoryDiagnoser]
    public class FillBenchmarks
    {
        private DecodedPng _frau = null!;
        private Color[] _row = null!;

        [GlobalSetup]
        public void Setup()
        {
            var bytes = File.ReadAllBytes("frau-mode-vintage-illustration-1622417428ANN.png");
            _frau = Decoder.ReadPng(bytes);
            _row = new Color[_frau.Width];
        }

        [GlobalCleanup]
        public void Cleanup() => _frau.Dispose();

        [Benchmark]
        public Color FillAllRows_Frau_1920x1920_RGBA()
        {
            var filler = default(ARGBFiller);
            for (var row = 0; row < _frau.Height; row++)
            {
                var scanLine = _frau.GetScanLine(row);
                var rowOut = _row.AsSpan();
                filler.Fill(ref rowOut, in scanLine);
            }
            return _row[0];
        }
    }
}
