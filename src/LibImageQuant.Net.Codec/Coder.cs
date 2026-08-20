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
        private static readonly PngFilterStrategy[] AdaptiveCandidates =
        {
            PngFilterStrategy.None, PngFilterStrategy.Sub, PngFilterStrategy.Up,
            PngFilterStrategy.Average, PngFilterStrategy.Paeth,
            PngFilterStrategy.MinSum, PngFilterStrategy.Entropy,
        };

        private readonly int _width;
        private readonly int _height;
        private readonly PngFilterStrategy _filterStrategy;
        private Func<Stream, Stream> CreateCompressorStream { get; }

        public Coder(int width, int height, Func<Stream, Stream> compressorStream = null,
            PngFilterStrategy filterStrategy = PngFilterStrategy.None)
        {
            _width = width;
            _height = height;
            _filterStrategy = filterStrategy;
            CreateCompressorStream =
                compressorStream ?? (buffer => new ZLibStream(buffer, CompressionMode.Compress, leaveOpen: true));
        }

        /// <summary>
        /// Filters every scanline of rawData under one concrete strategy (never Adaptive - that
        /// must already have been resolved to one of the strategies below by the caller) and
        /// returns the result as filter-type-byte + row pairs, ready to deflate. Rented from
        /// ArrayPool - the caller owns the returned array and must return it.
        /// </summary>
        private byte[] BuildFilteredScanlines(ReadOnlySpan<byte> rawData, PngFilterStrategy strategy)
        {
            var filteredLength = _height * (_width + 1);
            var result = ArrayPool<byte>.Shared.Rent(filteredLength);
            var zero = ArrayPool<byte>.Shared.Rent(_width);
            Array.Clear(zero, 0, _width);
            byte[][] attempts = null;
            try
            {
                var needsAttempts = strategy is PngFilterStrategy.MinSum or PngFilterStrategy.Entropy;
                if (needsAttempts)
                {
                    attempts = new byte[PngFilter.FilterCount][];
                    for (var i = 0; i < attempts.Length; i++)
                        attempts[i] = ArrayPool<byte>.Shared.Rent(_width);
                }

                ReadOnlySpan<byte> prevRow = zero.AsSpan(0, _width);
                for (var i = 0; i < _height; i++)
                {
                    var row = rawData.Slice(i * _width, _width);
                    var rowOffset = i * (_width + 1);
                    var dest = result.AsSpan(rowOffset + 1, _width);

                    var filterType = strategy switch
                    {
                        PngFilterStrategy.None or PngFilterStrategy.Sub or PngFilterStrategy.Up
                            or PngFilterStrategy.Average or PngFilterStrategy.Paeth
                            => ApplyFixedFilter(row, prevRow, dest, (int)strategy),
                        PngFilterStrategy.MinSum => PngFilter.SelectMinSum(row, prevRow, dest, attempts),
                        PngFilterStrategy.Entropy => PngFilter.SelectEntropy(row, prevRow, dest, attempts),
                        _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy,
                            "Adaptive must be resolved to a concrete strategy before filtering"),
                    };
                    result[rowOffset] = filterType;

                    prevRow = row;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(zero);
                if (attempts != null)
                    foreach (var a in attempts)
                        ArrayPool<byte>.Shared.Return(a);
            }
            return result;
        }

        private static byte ApplyFixedFilter(ReadOnlySpan<byte> row, ReadOnlySpan<byte> prevRow, Span<byte> dest, int filterType)
        {
            PngFilter.Filter(row, prevRow, dest, filterType);
            return (byte)filterType;
        }

        /// <summary>
        /// Mirrors zopflipng's own filter search: trial-compresses every candidate strategy with
        /// a fast in-process compressor (ranking only - the real compressor, e.g. Zopfli, would
        /// be far too slow to run once per candidate) and keeps whichever produced the smallest
        /// output. Returns the winning filtered buffer directly, rather than re-filtering the
        /// image a second time - the caller owns it and must return it to the pool.
        /// </summary>
        private (PngFilterStrategy Strategy, byte[] Filtered) ChooseAdaptiveStrategy(ReadOnlySpan<byte> rawData)
        {
            var filteredLength = _height * (_width + 1);
            var best = AdaptiveCandidates[0];
            byte[] bestFiltered = null;
            var bestSize = long.MaxValue;

            foreach (var candidate in AdaptiveCandidates)
            {
                var filtered = BuildFilteredScanlines(rawData, candidate);
                var size = QuickCompressedSize(filtered.AsSpan(0, filteredLength));
                if (size < bestSize)
                {
                    bestSize = size;
                    best = candidate;
                    if (bestFiltered != null)
                        ArrayPool<byte>.Shared.Return(bestFiltered);
                    bestFiltered = filtered;
                }
                else
                {
                    ArrayPool<byte>.Shared.Return(filtered);
                }
            }
            return (best, bestFiltered);
        }

        private static long QuickCompressedSize(ReadOnlySpan<byte> filtered)
        {
            using var counting = new MemoryStream();
            using (var deflate = new DeflateStream(counting, CompressionLevel.Fastest, leaveOpen: true))
                deflate.Write(filtered);
            return counting.Length;
        }

        public byte[] CreateBytes(QuantizationResult result)
        {
            var backingArr = ArrayPool<byte>.Shared.Rent(result.ImageData.Length);
            byte[] filteredScanlines = null;
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

                var strategy = _filterStrategy;
                if (strategy == PngFilterStrategy.Adaptive)
                {
                    (strategy, filteredScanlines) = ChooseAdaptiveStrategy(result.ImageData);
                }
                else
                {
                    filteredScanlines = BuildFilteredScanlines(result.ImageData, strategy);
                }
                var filteredLength = _height * (_width + 1);

                //using var inf = new ZopfliStream(buffer, capacity: height * width + height);
                ReadOnlySpan<byte> arr;
                using (var buffer = new MemoryStream(backingArr))
                using (var deflater = CreateCompressorStream(buffer))
                {
                    deflater.Write(filteredScanlines, 0, filteredLength);
                    deflater.Flush();
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
                if (filteredScanlines != null)
                    ArrayPool<byte>.Shared.Return(filteredScanlines);
            }
        }
    }
}
