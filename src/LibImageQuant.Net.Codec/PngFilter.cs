using System;
using System.Numerics;

namespace LibImageQuant.Net.Codec
{
    /// <summary>
    /// Forward (encode-side) PNG scanline filtering for 1-byte-per-pixel rows - the shape
    /// Coder always produces, since it only ever writes 8-bit indexed-color (PLTE) images.
    /// Shares its Paeth predictor with Decoder.ApplyPngFilters, which reverses these same five
    /// filter types on the way back in; the two must agree exactly for round-tripping to work.
    /// </summary>
    internal static class PngFilter
    {
        public const int FilterCount = 5; // None, Sub, Up, Average, Paeth

        internal static int PaethPredictor(int a, int b, int c)
        {
            var pa = Math.Abs(b - c);
            var pb = Math.Abs(a - c);
            var pc = Math.Abs(a + b - c - c);
            if (pa <= pb && pa <= pc)
                return a;
            else if (pb <= pc)
                return b;
            else return c;
        }

        /// <summary>
        /// Applies one fixed filter type to a single row. prevRow must be the same length as
        /// row; pass a zero-filled span for the image's first row (Prior(x) is defined as 0
        /// there, per the PNG spec, section 9.2).
        /// </summary>
        internal static void Filter(ReadOnlySpan<byte> row, ReadOnlySpan<byte> prevRow, Span<byte> dest, int filterType)
        {
            switch (filterType)
            {
                case 0: // None
                    row.CopyTo(dest);
                    break;
                case 1: // Sub
                    dest[0] = row[0];
                    for (var i = 1; i < row.Length; i++)
                        dest[i] = unchecked((byte)(row[i] - row[i - 1]));
                    break;
                case 2: // Up
                    for (var i = 0; i < row.Length; i++)
                        dest[i] = unchecked((byte)(row[i] - prevRow[i]));
                    break;
                case 3: // Average
                    for (var i = 0; i < row.Length; i++)
                    {
                        var a = i > 0 ? row[i - 1] : 0;
                        dest[i] = unchecked((byte)(row[i] - ((a + prevRow[i]) / 2)));
                    }
                    break;
                case 4: // Paeth
                    for (var i = 0; i < row.Length; i++)
                    {
                        var a = i > 0 ? row[i - 1] : 0;
                        var c = i > 0 ? prevRow[i - 1] : 0;
                        dest[i] = unchecked((byte)(row[i] - PaethPredictor(a, prevRow[i], c)));
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(filterType));
            }
        }

        /// <summary>
        /// Per-scanline adaptive selection using the PNG spec's own "minimum sum of absolute
        /// differences" heuristic (lodepng's LFS_MINSUM, and zopflipng's default): try each of
        /// the 5 filter types, score by summing filtered bytes (as signed values for anything
        /// but None), and keep the smallest-sum candidate. Writes the winning filtered bytes
        /// into dest and returns its filter type. attempts must hold FilterCount arrays, each
        /// at least row.Length long.
        /// </summary>
        internal static byte SelectMinSum(ReadOnlySpan<byte> row, ReadOnlySpan<byte> prevRow, Span<byte> dest, byte[][] attempts)
        {
            var bestType = 0;
            long bestSum = -1;
            for (var type = 0; type < FilterCount; type++)
            {
                var attempt = attempts[type].AsSpan(0, row.Length);
                Filter(row, prevRow, attempt, type);

                long sum = 0;
                foreach (var b in attempt)
                    // Filter type 0 isn't a difference, so its bytes are summed unsigned; the
                    // other four are differences, so a byte >=128 represents a negative value
                    // and should count as its (smaller) magnitude, not its raw byte value.
                    sum += type == 0 ? b : (b < 128 ? b : 255 - b);

                if (bestSum < 0 || sum < bestSum)
                {
                    bestSum = sum;
                    bestType = type;
                }
            }
            attempts[bestType].AsSpan(0, row.Length).CopyTo(dest);
            return (byte)bestType;
        }

        /// <summary>
        /// Per-scanline adaptive selection using lodepng's LFS_ENTROPY heuristic: try each
        /// filter type, estimate the byte-histogram entropy of the result, and keep whichever
        /// scores lowest (most compressible).
        /// </summary>
        internal static byte SelectEntropy(ReadOnlySpan<byte> row, ReadOnlySpan<byte> prevRow, Span<byte> dest, byte[][] attempts)
        {
            var bestType = 0;
            long bestScore = -1;
            Span<int> counts = stackalloc int[256];
            for (var type = 0; type < FilterCount; type++)
            {
                var attempt = attempts[type].AsSpan(0, row.Length);
                Filter(row, prevRow, attempt, type);

                counts.Clear();
                foreach (var b in attempt)
                    counts[b]++;
                counts[type]++; // the filter-type byte itself is part of the scanline

                long score = 0;
                foreach (var count in counts)
                    score += Log2ICount(count);

                // sum(count * log2(count)) is maximized exactly when total entropy bits
                // (~= N*log2(N) - sum(count*log2(count)), N constant across candidates) is
                // minimized, so the largest score is the most compressible candidate.
                if (bestScore < 0 || score > bestScore)
                {
                    bestScore = score;
                    bestType = type;
                }
            }
            attempts[bestType].AsSpan(0, row.Length).CopyTo(dest);
            return (byte)bestType;
        }

        /// <summary>Integer approximation of count * log2(count), 0 for count == 0.</summary>
        private static long Log2ICount(int count)
        {
            if (count == 0)
                return 0;
            var l = BitOperations.Log2((uint)count);
            return ((long)count * l) + (((long)count - (1L << l)) << 1);
        }
    }
}
