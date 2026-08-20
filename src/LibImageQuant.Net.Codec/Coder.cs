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
                compressorStream ?? (buffer => new ZLibStream(buffer, CompressionMode.Compress, leaveOpen: true));
        }



        private void DeflateData(in ReadOnlySpan<byte> rawData, Stream deflater)
        {
            for (var i = 0; i < _height; i++)
            {
                deflater.WriteByte(0);
                var row = rawData.Slice(i * _width, _width);
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

                //Alpha values have the same interpretation as in an 8-bit full alpha channel: 0 is fully transparent, 255 is fully opaque.
                //tRNS can contain fewer values than there are palette entries, in which case the remaining entries are
                //assumed opaque - but the palette isn't guaranteed sorted by alpha (libimagequant doesn't always put
                //fully-opaque entries last), so a truncated tRNS chunk can silently drop real transparency for colors
                //that land after the first opaque one. Write one alpha byte per palette entry whenever there's any
                //transparency at all, and omit tRNS entirely only when every entry is fully opaque.
                var hasTransparency = palette.HasTransparency();


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
                                        (hasTransparency ?
                                        4 + 4 + palette.Length + 4 //alpha/transparency
                                                                     : 0) +
                                        4 + 4 + arr.Length + 4 /* dat */
                                        + 12 /* IEND */];

                var writer = new SpanWriter(outArray);
                writer.WriteSpan(Sig);
                writer.WriteHeader(_height, _width, 8);
                writer.WritePalette(palette);
                if (hasTransparency)
                {
                    writer.WriteTransparency(in palette);
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
