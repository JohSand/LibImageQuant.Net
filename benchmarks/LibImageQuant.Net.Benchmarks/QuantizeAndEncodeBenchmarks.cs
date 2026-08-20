using BenchmarkDotNet.Attributes;
using LibImageQuant.Net.Codec;
using LibImageQuant.Net.Core;
using System;
using System.IO;
using Decoder = LibImageQuant.Net.Codec.Decoder;

namespace LibImageQuant.Net.Benchmarks
{
    /// <summary>
    /// The full pipeline (decode source -> quantize -> encode). Quantize is native
    /// libimagequant work and dominates end-to-end time by roughly 4-8x over the managed
    /// decode+encode combined - these benchmarks make that split visible rather than assumed.
    /// </summary>
    [MemoryDiagnoser]
    public class QuantizeAndEncodeBenchmarks
    {
        private DecodedPng _frau = null!;
        private IProvideImages _provider = null!;

        [GlobalSetup]
        public void Setup()
        {
            var bytes = File.ReadAllBytes("frau-mode-vintage-illustration-1622417428ANN.png");
            _frau = Decoder.ReadPng(bytes);
            _provider = GetProvider(_frau);
        }

        private static IProvideImages GetProvider(DecodedPng dec) => dec.ColorType switch
        {
            ColorType.RGBA => new ManagedProvider<ARGBFiller>(dec),
            ColorType.RGB => new ManagedProvider<RGBFiller>(dec),
            _ => throw new ArgumentException("Colortype does not support quantization"),
        };

        [Benchmark(Baseline = true)]
        public byte[] QuantizeAndEncode_MaxColors64()
        {
            using var quantizer = new Quantizer { DitheringLevel = 0f, MaxColors = 64 };
            using var result = quantizer.Quantize(_provider, _frau.Width, _frau.Height);
            return new Coder(_frau.Width, _frau.Height).CreateBytes(result);
        }

        [Benchmark]
        public byte[] QuantizeAndEncode_MaxColors256()
        {
            using var quantizer = new Quantizer { DitheringLevel = 0f, MaxColors = 256 };
            using var result = quantizer.Quantize(_provider, _frau.Width, _frau.Height);
            return new Coder(_frau.Width, _frau.Height).CreateBytes(result);
        }

        [Benchmark]
        public int Quantize_MaxColors64_Only()
        {
            using var quantizer = new Quantizer { DitheringLevel = 0f, MaxColors = 64 };
            using var result = quantizer.Quantize(_provider, _frau.Width, _frau.Height);
            return result.PaletteData.Length;
        }
    }
}
