using LibImageQuant.Net.Core;
using SpanDex;
using System;
using System.Buffers;
using System.IO;
using System.IO.Compression;

namespace LibImageQuant.Net.Codec
{
    using static Constants;
    public class Coder
    {
        private readonly int _width;
        private readonly int _height;
        private Func<Stream, Stream> CreateCompressorStream { get; }

        public Coder(int width, int height, Func<Stream, Stream> compressorStream = null)
        {
            _width = width;
            _height = height;
            CreateCompressorStream =
                compressorStream ?? (buffer => DeflateStreamHelpers.ZlibStream(buffer, CompressionMode.Compress, true, 15, -1));
        }



        private void DeflateData(in ReadOnlySpan<byte> rawData, Stream deflater)
        {
            for (var i = 0; i < _height; i++)
            {
                var row = rawData.Slice(i * _width, _width);
                deflater.WriteByte(0);
                deflater.Write(row);
            }
            deflater.Flush();
        }

        public byte[] CreateBytes(in QuantizationResult result)
        {
            var backingArr = ArrayPool<byte>.Shared.Rent(result.ImageData.Length);
            try
            {
                var palette = result.PaletteData;//new byte[3] {182, 32, 32};

                //Alpha values have the same interpretation as in an 8-bit full alpha channel: 0 is fully transparent, 255 is fully opaque
                //tRNS can contain fewer values than there are palette entries
                //In this case, the alpha value for all remaining palette entries is assumed to be 255
                //lib image quant should have ordered the palette, so that all opaque alphas are at the end of the palette, allowing us to minimize the tRNS-chunk.
                var lastTranspIndex = palette.GetLastAlphaIndex();
                var alphas = palette[0..lastTranspIndex];


                //using var inf = new ZopfliStream(buffer, capacity: height * width + height);
                ReadOnlySpan<byte> arr;
                using (var buffer = new MemoryStream(backingArr))
                using (var deflater = CreateCompressorStream(buffer))
                {
                    DeflateData(result.ImageData, deflater);
                    arr = new ReadOnlySpan<byte>(backingArr, 0, (int)buffer.Position);
                }

                var outArray = new byte[8 +//sig 
                                        4 + 4 + 13 + 4 /* header */ +
                                        4 + 4 + palette.Length * 3 + 4 /* pal */ +
                                        (alphas.Length > 0 ?
                                        4 + 4 + alphas.Length + 4 //alpha/transparency
                                                                     : 0) +
                                        4 + 4 + arr.Length + 4 /* dat */
                                        + 12 /* IEND */];

                var writer = new SpanWriter(outArray);
                writer.WriteSpan(Sig);
                writer.WriteHeader(_height, _width, 8);
                writer.WritePalette(in palette);
                if (alphas.Length > 0)
                {
                    writer.WriteTransparency(in alphas);
                }
                writer.WriteData(in arr);
                writer.WriteEnd();
                return outArray;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(backingArr);
            }
        }
    }
}
