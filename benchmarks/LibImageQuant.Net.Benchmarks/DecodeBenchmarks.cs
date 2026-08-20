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

        // Disposes within the benchmark rather than returning DecodedPng directly, so each
        // iteration actually returns its pooled buffer to ArrayPool for the next iteration to
        // reuse - representative of real decode-then-dispose usage, and what makes the pooled
        // buffer's allocation reduction visible in MemoryDiagnoser at all (returning the
        // undisposed object would just rent a fresh array every time, same as before pooling).
        [Benchmark]
        public int Decode_Panda_200x150_RGBA()
        {
            using var dec = Decoder.ReadPng(_panda);
            return dec.Width;
        }

        [Benchmark]
        public int Decode_Image06_800x500_RGB()
        {
            using var dec = Decoder.ReadPng(_image06);
            return dec.Width;
        }

        [Benchmark]
        public int Decode_Frau_1920x1920_RGBA()
        {
            using var dec = Decoder.ReadPng(_frau);
            return dec.Width;
        }
    }
}
