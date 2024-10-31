using SpanDex;
using System;
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
    using static System.Runtime.InteropServices.JavaScript.JSType;

    public class DecodedPng(DecoderData data, byte[] bytes)
    {
        public int Width => data.Width;
        public int Height => data.Height;

        public ColorType ColorType => data.ColorType;


        public ReadOnlySpan<byte> GetScanLine(int rowIndex)
        {
            var rowLen = (Width * data.BitsPerPixel) + 1;
            var startIndex = rowLen * rowIndex + 1;
            var scanLine = new ReadOnlySpan<byte>(bytes, startIndex, Width * data.BitsPerPixel);
            return scanLine;
        }

        public Color GetPixel(int row, int column)
        {
            if (row > Height)
                throw new ArgumentException("Argument out of bounds", nameof(row));
            if (column > Width)
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
            else
            {
                var b = line[(column * 3) + 2];
                var g = line[(column * 3) + 1];
                var r = line[(column * 3) + 0];
                return new Color(255, r, g, b);
            }
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
        private static int PaethPredictor(int a, int b, int c)
        {
            //var p = a + b - c;
            var pa = Math.Abs(b - c);
            var pb = Math.Abs(a - c);
            var pc = Math.Abs(a + b - c - c);
            // return nearest of a, b, c,
            // breaking ties in order a, b, c.
            if (pa <= pb && pa <= pc)
                return a;
            else if (pb <= pc)
                return b;
            else return c;
        }

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
                        var sub = x + prev;
                        x = unchecked((byte)(sub % 256));
                    }
                }
                else if (type == 2)//filter up
                {
                    if (rowIndex == 0) continue;
                    var priorRow = new Span<byte>(bytes, rowLen * (rowIndex - 1) + 1, pngData.Width * pngData.BitsPerPixel);
                    for (int i = 0; i < pngData.Width * pngData.BitsPerPixel; i++)
                    {
                        ref var x = ref scanLine[i];
                        x = unchecked((byte)((x + priorRow[i]) % 256));
                    }
                }

                else if (type == 3)//filter avg
                {
                    //todo, check if this was the first row?
                    var priorRow = new Span<byte>(bytes, rowLen * (rowIndex - 1) + 1, pngData.Width * pngData.BitsPerPixel);
                    for (int i = 0; i < pngData.BitsPerPixel; i++)
                    {
                        var avg = 0 + priorRow[i] / 2;
                        ref var x = ref scanLine[i];
                        x = unchecked((byte)((x + avg) % 256));
                    }

                    for (int i = pngData.BitsPerPixel; i < pngData.Width * pngData.BitsPerPixel; i++)
                    {
                        var avg = (scanLine[i - pngData.BitsPerPixel] + priorRow[i]) / 2;
                        ref var x = ref scanLine[i];
                        x = unchecked((byte)((x + avg) % 256));
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
                            var paeth = PaethPredictor(a, b, c);

                            ref var x = ref scanLine[i];
                            x = unchecked((byte)((x + paeth) % 256));
                        }

                        for (int i = pngData.BitsPerPixel; i < pngData.Width * pngData.BitsPerPixel; i++)
                        {
                            var a = scanLine[i - pngData.BitsPerPixel];
                            var b = 0;
                            var c = 0;
                            var paeth = PaethPredictor(a, b, c);

                            ref var x = ref scanLine[i];
                            x = unchecked((byte)((x + paeth) % 256));
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
                            var paeth = PaethPredictor(a, b, c);

                            ref var x = ref scanLine[i];
                            x = unchecked((byte)((x + paeth) % 256));
                        }

                        for (int i = pngData.BitsPerPixel; i < pngData.Width * pngData.BitsPerPixel; i++)
                        {
                            var a = scanLine[i - pngData.BitsPerPixel];
                            var b = priorRow[i];
                            var c = priorRow[i - pngData.BitsPerPixel];
                            var paeth = PaethPredictor(a, b, c);

                            ref var x = ref scanLine[i];
                            x = unchecked((byte)((x + paeth) % 256));
                        }
                    }
                }
            }
        }

        private static DecoderData ReadPngData(ref MemoryReader reader, ChunkedStream buffer)
        {
            DecoderData pngData = default;

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
                var pngData = ReadPngData(ref reader, buffer);

                var bytes = new byte[pngData.BufferSize];

                using var inflater = new ZLibStream(buffer, CompressionMode.Decompress, false);

                var bytesRead = ReadToEnd(inflater, bytes);

                Debug.Assert(bytesRead == bytes.Length);

                ApplyPngFilters(in pngData, bytes);

                return new DecodedPng(pngData, bytes);
            }
            else
            {
                throw new ArgumentException("Parameter was not valid png data", nameof(inData));
            }
        }
    }
}
