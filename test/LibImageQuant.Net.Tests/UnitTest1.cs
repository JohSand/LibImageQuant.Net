using LibImageQuant.Net.Codec;
using LibImageQuant.Net.Core;
using System;
using System.IO;
using System.Linq;
using Xunit;
using Zopfli.Net;

namespace LibImageQuant.Net.Tests
{
    public class UnitTest1
    {

        private static IProvideImages GetProvider(DecodedPng dec) => dec.ColorType switch
        {
            ColorType.RGBA => new ManagedProvider<ARGBFiller>(dec),
            ColorType.RGB => new ManagedProvider<RGBFiller>(dec),
            _ => throw new ArgumentException("Colortype does not support quantization"),
        };

        private static byte[] Compress(byte[] imageBytes, Func<Stream, Stream> compressorStream = null)
        {
            var dec = Decoder.ReadPng(imageBytes);
            using var quantizer = new Quantizer { DitheringLevel = 0.6f, Quality = (0, 80) };
            using var result = quantizer.Quantize(GetProvider(dec), dec.Width, dec.Height);
            return new Coder(dec.Width, dec.Height, compressorStream).CreateBytes(result);
        }



        [Fact]
        public void TestCompressionSize()
        {
            var fileName = @"frau-mode-vintage-illustration-1622417428ANN.png";
            var bytes = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), fileName));
            var result = Compress(bytes);
            Assert.True(bytes.Length > result.Length * 2);
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "out.png"), result);
        }

        [Fact]
        public void Test2()
        {
            var arr = Enumerable.Repeat((byte)8, 2000).ToArray().AsMemory();
            var chunked = new ChunkedStream();
            for (int i = 0; i < 10; i++)
            {
                var s = arr.Slice(i * 200, 200);
                chunked.Write(s);
            }

            var ms = new MemoryStream();
            chunked.CopyTo(ms);
            var outarr = ms.ToArray();
        }

        [Fact]
        public void TestCompressionZopfli()
        {
            var fileName = @"panda.png";
            var bytes = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), fileName));

            var result = Compress(bytes, s => new ZopfliStream(s, true, bytes.Length));
            Assert.True(bytes.Length > result.Length * 3);
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "panda_out_zopfli.png"), result);
        }

        [Fact]
        public void Test4()
        {
            var fileName = @"panda.png";
            var bytes = File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), fileName));
            var result = Compress(bytes);
            Assert.True(bytes.Length > result.Length);
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), "panda_out.png"), result);
        }

        [Fact]
        public void TestQuality()
        {
            var dec = Decoder.ReadPng(File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), @"panda.png")));
            using var quantizer = new Quantizer { DitheringLevel = 0.6f, Quality = (70, 100) };
            using var result = quantizer.Quantize(GetProvider(dec), dec.Width, dec.Height);
            double nrmse = GetError(dec, result);
            Assert.True(nrmse < 1);
            ;
        }

        [Fact]
        public void TestQualityNoAlpha()
        {
            var dec = Decoder.ReadPng(File.ReadAllBytes(Path.Combine(Directory.GetCurrentDirectory(), @"image06.png")));
            using var quantizer = new Quantizer { DitheringLevel = 0.0f, Quality = (0, 100) };
            using var result = quantizer.Quantize(GetProvider(dec), dec.Width, dec.Height);
            double nrmse = GetError(dec, result);
            Assert.Equal(0, nrmse, 3);
        }

        private static double GetError(DecodedPng dec, QuantizationResult result)
        {
            double sse = 0;
            for (var row = 0; row < dec.Height; row++)
            {
                for (var col = 0; col < dec.Width; col++)
                {
                    var index = row * dec.Width + col;
                    var c1 = dec.GetPixel(row, col);
                    var c2 = result.PaletteData[result.ImageData[index]];
                    var q1 = Math.Pow(c1.Red - c2.Red, 2);
                    var q2 = Math.Pow(c1.Green - c2.Green, 2);
                    var q3 = Math.Pow(c1.Blue - c2.Blue, 2);
                    var q4 = Math.Pow(c1.Alpha - c2.Alpha, 2);
                    sse += (q1 + q2 + q3 + q4);
                }
            }
            return Math.Sqrt(sse / (dec.Width * dec.Height)) / 255;
        }
    }
}
