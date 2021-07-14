using LibImageQuant.Net.Core;
using SpanDex;
using System;
using System.IO;
using System.IO.Compression;

namespace LibImageQuant.Net.Codec
{
    using static Constants;
    public class Coder
    {
        private static byte[] DeflateData(ReadOnlySpan<byte> rawData, int height, int width)
        {
            using var buffer = new MemoryStream();
            //using var inf = new ZopfliStream(buffer, capacity: height * width + height);
            using var inf = DeflateStreamHelpers.ZlibStream(buffer, CompressionMode.Compress, false, 15, -1);

            //using var inf = new DeflateStream(buffer, CompressionLevel.Optimal);
            //using var inf = new DeflaterOutputStream(buffer, new Deflater(Deflater.BEST_COMPRESSION));
            //var inf = new ZopfliDeflater(buffer);
            for (var i = 0; i < height; i++)
            {
                var row = rawData.Slice(i * width, width);
                inf.WriteByte(0);
                inf.Write(row);

                //inf.Deflate(LineFilter);
                //inf.Deflate(row);
            }
            inf.Flush();

            var arr = buffer.ToArray();
            return arr;
        }

        public byte[] CreateBytes(in QuantizationResult result, int width, int height)
        {
            var palette = result.PaletteData;//new byte[3] {182, 32, 32};

            //Alpha values have the same interpretation as in an 8-bit full alpha channel: 0 is fully transparent, 255 is fully opaque
            //tRNS can contain fewer values than there are palette entries
            //In this case, the alpha value for all remaining palette entries is assumed to be 255
            //lib image quant should have ordered the palette, so that all opaque alphas are at the end of the palette, allowing us to minimize the tRNS-chunk.
            var lastTranspIndex = palette.GetLastAlphaIndex();
            var alphas = palette[0..lastTranspIndex];
            var s = width * height;

            var rawData = result.ImageData;

            var arr = DeflateData(rawData, height, width);

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
            writer.WriteHeader(height, width, 8);
            writer.WritePalette(palette);
            if (alphas.Length > 0)
            {
                writer.WriteTransparency(alphas);
            }
            writer.WriteData(arr);
            writer.WriteEnd();
            return outArray;
        }
    }
}
