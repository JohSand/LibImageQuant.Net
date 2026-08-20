using SpanDex;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Buffers.Binary;
using Soft160.Data.Cryptography;
using LibImageQuant.Net.Core;

namespace LibImageQuant.Net.Codec
{
    using static Constants;

    /// <summary>
    /// Owns a pixel buffer rented from ArrayPool&lt;byte&gt;.Shared (Decoder.ReadPng always
    /// allocates one of these per decode, and decoded images can be large) - Dispose it, or
    /// wrap it in a using, once you're done reading pixels from it.
    /// </summary>
    public class DecodedPng(DecoderData data, byte[] bytes, Color[] palette) : IDisposable
    {
        private bool _disposed;

        public int Width => data.Width;
        public int Height => data.Height;

        public ColorType ColorType => data.ColorType;


        public ReadOnlySpan<byte> GetScanLine(int rowIndex)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            var rowLen = (Width * data.BitsPerPixel) + 1;
            var startIndex = rowLen * rowIndex + 1;
            var scanLine = new ReadOnlySpan<byte>(bytes, startIndex, Width * data.BitsPerPixel);
            return scanLine;
        }

        public Color GetPixel(int row, int column)
        {
            if (row >= Height)
                throw new ArgumentException("Argument out of bounds", nameof(row));
            if (column >= Width)
                throw new ArgumentException("Argument out of bounds", nameof(column));

            var line = GetScanLine(row);

            if (ColorType == ColorType.RGBA)
            {
                var a = line[(column * 4) + 3];
                var b = line[(column * 4) + 2];
                var g = line[(column * 4) + 1];
                var r = line[(column * 4) + 0];
                return new Color(a, r, g, b);
            }
            else if (ColorType == ColorType.PLTE)
            {
                return palette[line[column]];
            }
            else
            {
                var b = line[(column * 3) + 2];
                var g = line[(column * 3) + 1];
                var r = line[(column * 3) + 0];
                return new Color(255, r, g, b);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            ArrayPool<byte>.Shared.Return(bytes);
        }
    }

    public readonly struct DecoderData
    {
        public int Width { get; init; }
        public int Height { get; init; }
        public byte BitDepth { get; init; }

        public ColorType ColorType { get; init; }

        public byte BitsPerPixel => ColorType switch { ColorType.RGBA => 4, ColorType.RGB => 3, _ => 1 };

        public int Size => Height * Width;

        public int BufferSize => Size * BitDepth * BitsPerPixel / 8 + Height;
    }

    public static class Decoder
    {
        private static void ApplyPngFilters(in DecoderData pngData, byte[] bytes)
        {
            for (var rowIndex = 0; rowIndex < pngData.Height; rowIndex++)
            {
                var rowLen = pngData.Width * pngData.BitsPerPixel + 1;
                var startIndex = rowLen * rowIndex + 1;
                var scanLine = new Span<byte>(bytes, startIndex, pngData.Width * pngData.BitsPerPixel);
                var type = bytes[startIndex - 1];
                //todo check bit depth?
                if (type == 1)//filter sub
                {
                    for (int i = pngData.BitsPerPixel; i < pngData.Width * pngData.BitsPerPixel; i++)
                    {
                        ref var x = ref scanLine[i];
                        var prev = scanLine[i - pngData.BitsPerPixel];
                        // Casting to byte already truncates to the low 8 bits, which is
                        // identical to % 256 here since x + prev is always non-negative
                        // (both operands are byte-derived, 0-255) - so the modulo (a div
                        // instruction) is pure overhead in this hot per-byte loop.
                        x = unchecked((byte)(x + prev));
                    }
                }
                else if (type == 2)//filter up
                {
                    if (rowIndex == 0) continue;
                    var priorRow = new Span<byte>(bytes, rowLen * (rowIndex - 1) + 1, pngData.Width * pngData.BitsPerPixel);
                    for (int i = 0; i < pngData.Width * pngData.BitsPerPixel; i++)
                    {
                        ref var x = ref scanLine[i];
                        x = unchecked((byte)(x + priorRow[i]));
                    }
                }

                else if (type == 3)//filter avg
                {
                    if (rowIndex == 0)
                    {
                        // Prior(x) is 0 for the first row. For i < bpp, Raw(x-bpp) is also 0,
                        // so avg is 0 and those bytes are already correct as-is.
                        for (int i = pngData.BitsPerPixel; i < pngData.Width * pngData.BitsPerPixel; i++)
                        {
                            var avg = scanLine[i - pngData.BitsPerPixel] / 2;
                            ref var x = ref scanLine[i];
                            x = unchecked((byte)(x + avg));
                        }
                    }
                    else
                    {
                        var priorRow = new Span<byte>(bytes, rowLen * (rowIndex - 1) + 1, pngData.Width * pngData.BitsPerPixel);
                        for (int i = 0; i < pngData.BitsPerPixel; i++)
                        {
                            var avg = priorRow[i] / 2;
                            ref var x = ref scanLine[i];
                            x = unchecked((byte)(x + avg));
                        }

                        for (int i = pngData.BitsPerPixel; i < pngData.Width * pngData.BitsPerPixel; i++)
                        {
                            var avg = (scanLine[i - pngData.BitsPerPixel] + priorRow[i]) / 2;
                            ref var x = ref scanLine[i];
                            x = unchecked((byte)(x + avg));
                        }
                    }
                }

                else if (type == 4)//filter paeth
                {
                    if (rowIndex == 0)
                    {
                        for (int i = 0; i < pngData.BitsPerPixel; i++)
                        {
                            var a = 0;
                            var b = 0;
                            var c = 0;
                            var paeth = PngFilter.PaethPredictor(a, b, c);

                            ref var x = ref scanLine[i];
                            x = unchecked((byte)(x + paeth));
                        }

                        for (int i = pngData.BitsPerPixel; i < pngData.Width * pngData.BitsPerPixel; i++)
                        {
                            var a = scanLine[i - pngData.BitsPerPixel];
                            var b = 0;
                            var c = 0;
                            var paeth = PngFilter.PaethPredictor(a, b, c);

                            ref var x = ref scanLine[i];
                            x = unchecked((byte)(x + paeth));
                        }
                    }

                    else
                    {
                        var priorRow = new Span<byte>(bytes, rowLen * (rowIndex - 1) + 1, pngData.Width * pngData.BitsPerPixel);
                        for (int i = 0; i < pngData.BitsPerPixel; i++)
                        {
                            var a = 0;
                            var b = priorRow[i];
                            var c = 0;
                            var paeth = PngFilter.PaethPredictor(a, b, c);

                            ref var x = ref scanLine[i];
                            x = unchecked((byte)(x + paeth));
                        }

                        for (int i = pngData.BitsPerPixel; i < pngData.Width * pngData.BitsPerPixel; i++)
                        {
                            var a = scanLine[i - pngData.BitsPerPixel];
                            var b = priorRow[i];
                            var c = priorRow[i - pngData.BitsPerPixel];
                            var paeth = PngFilter.PaethPredictor(a, b, c);

                            ref var x = ref scanLine[i];
                            x = unchecked((byte)(x + paeth));
                        }
                    }
                }
            }
        }

        private static DecoderData ReadPngData(ref MemoryReader reader, ChunkedStream buffer, out byte[] paletteRgb, out byte[] paletteAlpha)
        {
            DecoderData pngData = default;
            paletteRgb = [];
            paletteAlpha = [];

            while (reader.Remaining > 0)
            {
                var length = reader.ReadInt32BigEndian();
                var type = reader.ReadSpan(4);

                if (type.SequenceEqual(IHDR))
                {
                    var chunk = length > 0 ? reader.ReadSpan(length) : [];
                    var width = BinaryPrimitives.ReadInt32BigEndian(chunk);
                    var height = BinaryPrimitives.ReadInt32BigEndian(chunk[4..]);
                    var bitDepth = chunk[8];
                    var colorType = (ColorType)chunk[9];
                    pngData = new DecoderData
                    {
                        Width = width,
                        Height = height,
                        ColorType = colorType,
                        BitDepth = bitDepth
                    };

                    var crc = reader.ReadUInt32BigEndian();
                    var calculatedCrc = CRC.Crc32(chunk, CRC.Crc32(type));
                    Debug.Assert(crc == calculatedCrc, "Invalid CRC");
                }
                else if (type.SequenceEqual(IDAT))
                {
                    var chunk = length > 0 ? reader.ReadMemory(length) : ReadOnlyMemory<byte>.Empty;
                    buffer.Write(chunk);
                    var crc = reader.ReadUInt32BigEndian();
                    var calculatedCrc = CRC.Crc32(chunk, CRC.Crc32(type));
                    Debug.Assert(crc == calculatedCrc, "Invalid CRC");
                }
                else if (type.SequenceEqual(PLTE))
                {
                    var chunk = length > 0 ? reader.ReadSpan(length) : [];
                    paletteRgb = chunk.ToArray();
                    var crc = reader.ReadUInt32BigEndian();
                    var calculatedCrc = CRC.Crc32(chunk, CRC.Crc32(type));
                    Debug.Assert(crc == calculatedCrc, "Invalid CRC");
                }
                else if (type.SequenceEqual(tRNS))
                {
                    var chunk = length > 0 ? reader.ReadSpan(length) : [];
                    paletteAlpha = chunk.ToArray();
                    var crc = reader.ReadUInt32BigEndian();
                    var calculatedCrc = CRC.Crc32(chunk, CRC.Crc32(type));
                    Debug.Assert(crc == calculatedCrc, "Invalid CRC");
                }
                else
                {
                    var data = length > 0 ? reader.ReadSpan(length) : [];
#if (DEBUG)
                    var name = Encoding.UTF8.GetString(type.ToArray());
#endif
                    var crc = reader.ReadUInt32BigEndian();
                    var calculatedCrc = CRC.Crc32(data, CRC.Crc32(type));
                    Debug.Assert(crc == calculatedCrc, "Invalid CRC");
                }

            }
            return pngData;
        }

        /// <summary>
        /// Combines a PLTE chunk's RGB triples with an optional tRNS chunk's alpha values into
        /// a lookup palette for indexed-color (PLTE) pixels. Per the PNG spec, tRNS may contain
        /// fewer entries than PLTE; entries without a corresponding tRNS value are fully opaque.
        /// </summary>
        private static Color[] BuildPalette(byte[] paletteRgb, byte[] paletteAlpha)
        {
            var count = paletteRgb.Length / 3;
            var palette = new Color[count];
            for (var i = 0; i < count; i++)
            {
                var r = paletteRgb[(i * 3) + 0];
                var g = paletteRgb[(i * 3) + 1];
                var b = paletteRgb[(i * 3) + 2];
                var a = i < paletteAlpha.Length ? paletteAlpha[i] : (byte)255;
                palette[i] = new Color(a, r, g, b);
            }
            return palette;
        }

        private static int ReadToEnd(Stream s, Span<byte> buffer)
        {
            var read = 0;
            var totalBytes = 0;
            do
            {
                read = s.Read(buffer);
                totalBytes += read;
                buffer = buffer[read..];

            } while (read > 0);

            return totalBytes;
        }

        public static DecodedPng ReadPng(byte[] inData)
        {
            var reader = new MemoryReader(inData);
            if (reader.ReadSpan(8).SequenceEqual(Sig))
            {
                using var buffer = new ChunkedStream();
                var pngData = ReadPngData(ref reader, buffer, out var paletteRgb, out var paletteAlpha);

                // Rented, not `new`-allocated: decoded images can be several megabytes, and
                // this is exactly the kind of large, short-lived-or-reused buffer ArrayPool
                // exists for. The caller owns the returned DecodedPng and must Dispose it
                // (or use a `using`) to actually return the buffer to the pool.
                var bytes = ArrayPool<byte>.Shared.Rent(pngData.BufferSize);
                var bufferSpan = bytes.AsSpan(0, pngData.BufferSize);

                using var inflater = new ZLibStream(buffer, CompressionMode.Decompress, false);

                var bytesRead = ReadToEnd(inflater, bufferSpan);

                Debug.Assert(bytesRead == bufferSpan.Length);

                ApplyPngFilters(in pngData, bytes);

                return new DecodedPng(pngData, bytes, BuildPalette(paletteRgb, paletteAlpha));
            }
            else
            {
                throw new ArgumentException("Parameter was not valid png data", nameof(inData));
            }
        }
    }
}
