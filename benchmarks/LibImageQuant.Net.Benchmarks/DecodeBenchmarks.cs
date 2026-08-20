using BenchmarkDotNet.Attributes;
using LibImageQuant.Net.Codec;
using System.IO;
using Decoder = LibImageQuant.Net.Codec.Decoder;

namespace LibImageQuant.Net.Benchmarks
{
    /// <summary>
    /// Decoder.ReadPng, dominated by ApplyPngFilters - the managed per-byte scanline
    /// reconstruction loop, the main hand-written hot path on the decode side.
    /// </summary>
    [MemoryDiagnoser]
    public class DecodeBenchmarks
    {
        private byte[] _panda = null!;
        private byte[] _image06 = null!;
        private byte[] _frau = null!;

        [GlobalSetup]
        public void Setup()
        {
            _panda = File.ReadAllBytes("panda.png");
            _image06 = File.ReadAllBytes("image06.png");
            _frau = File.ReadAllBytes("frau-mode-vintage-illustration-1622417428ANN.png");
        }

        [Benchmark]
        public DecodedPng Decode_Panda_200x150_RGBA() => Decoder.ReadPng(_panda);

        [Benchmark]
        public DecodedPng Decode_Image06_800x500_RGB() => Decoder.ReadPng(_image06);

        [Benchmark]
        public DecodedPng Decode_Frau_1920x1920_RGBA() => Decoder.ReadPng(_frau);
    }
}
