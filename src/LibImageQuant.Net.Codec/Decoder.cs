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

    public class Decoder
    {
        private int size = 0;
        private byte bitDepth = 0;
        private byte BitsPerPixel = 0;
        private byte[] bytes = null;

        public int Width { get; private set; }
        public int Height { get; private set; }

        public ColorType ColorType { get; private set; }

        public void ReadPng(byte[] data)
        {
            var reader = new MemoryReader(data);
            if (reader.ReadMemory(8).Span.SequenceEqual(Sig))
            {
                ReadPng(ref reader);
                Decode();
            }
            else
            {
                throw new ArgumentException("Parameter was not valid png data", nameof(data));
            }
        }

        private void Decode()
        {
            for (var rowIndex = 0; rowIndex < Height; rowIndex++)
            {
                var rowLen = Width * BitsPerPixel + 1;
                var startIndex = rowLen * rowIndex + 1;
                var scanLine = new Span<byte>(bytes, startIndex, Width * BitsPerPixel);
                var type = bytes[startIndex - 1];
                //todo check bit depth?
                if (type == 1)//filter sub
                {
                    for (int i = BitsPerPixel; i < Width * BitsPerPixel; i++)
                    {
                        ref var x = ref scanLine[i];
                        var prev = scanLine[i - BitsPerPixel];
                        var sub = x + prev;
                        x = unchecked((byte)(sub % 256));
                    }
                }
                else if (type == 2)//filter up
                {
                    if (rowIndex == 0) continue;
                    var priorRow = new Span<byte>(bytes, rowLen * (rowIndex - 1) + 1, Width * BitsPerPixel);
                    for (int i = 0; i < Width * BitsPerPixel; i++)
                    {
                        ref var x = ref scanLine[i];
                        x = unchecked((byte)((x + priorRow[i]) % 256));
                    }
                }

                else if (type == 3)//filter avg
                {
                    //todo, check if this was the first row?
                    var priorRow = new Span<byte>(bytes, rowLen * (rowIndex - 1) + 1, Width * BitsPerPixel);
                    for (int i = 0; i < BitsPerPixel; i++)
                    {
                        var avg = 0 + priorRow[i] / 2;
                        ref var x = ref scanLine[i];
                        x = unchecked((byte)((x + avg) % 256));
                    }

                    for (int i = BitsPerPixel; i < Width * BitsPerPixel; i++)
                    {
                        var avg = (scanLine[i - BitsPerPixel] + priorRow[i]) / 2;
                        ref var x = ref scanLine[i];
                        x = unchecked((byte)((x + avg) % 256));
                    }
                }

                else if (type == 4)//filter paeth
                {
                    if (rowIndex == 0)
                    {
                        for (int i = 0; i < BitsPerPixel; i++)
                        {
                            var a = 0;
                            var b = 0;
                            var c = 0;
                            var paeth = PaethPredictor(a, b, c);

                            ref var x = ref scanLine[i];
                            x = unchecked((byte)((x + paeth) % 256));
                        }

                        for (int i = BitsPerPixel; i < Width * BitsPerPixel; i++)
                        {
                            var a = scanLine[i - BitsPerPixel];
                            var b = 0;
                            var c = 0;
                            var paeth = PaethPredictor(a, b, c);

                            ref var x = ref scanLine[i];
                            x = unchecked((byte)((x + paeth) % 256));
                        }
                    }

                    else
                    {
                        var priorRow = new Span<byte>(bytes, rowLen * (rowIndex - 1) + 1, Width * BitsPerPixel);
                        for (int i = 0; i < BitsPerPixel; i++)
                        {
                            var a = 0;
                            var b = priorRow[i];
                            var c = 0;
                            var paeth = PaethPredictor(a, b, c);

                            ref var x = ref scanLine[i];
                            x = unchecked((byte)((x + paeth) % 256));
                        }

                        for (int i = BitsPerPixel; i < Width * BitsPerPixel; i++)
                        {
                            var a = scanLine[i - BitsPerPixel];
                            var b = priorRow[i];
                            var c = priorRow[i - BitsPerPixel];
                            var paeth = PaethPredictor(a, b, c);

                            ref var x = ref scanLine[i];
                            x = unchecked((byte)((x + paeth) % 256));
                        }
                    }
                }
            }
        }

        public ReadOnlySpan<byte> GetScanLine(int rowIndex)
        {
            var rowLen = (Width * BitsPerPixel) + 1;
            var startIndex = rowLen * rowIndex + 1;
            var scanLine = new ReadOnlySpan<byte>(bytes, startIndex, Width * BitsPerPixel);
            return scanLine;
        }

        public Color GetPixel(int row, int column)
        {
            if (row > Height)
                throw new ArgumentException("Argument out of bounds", nameof(row));
            if (column > Width)
                throw new ArgumentException("Argument out of bounds", nameof(column));

            var line = GetScanLine(row);

            if(ColorType == ColorType.RGBA)
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

        // a = left, b = above, c = upper left
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

        private void ReadPng(ref MemoryReader reader)
        {
            using var buffer = new ChunkedStream();           
            while (reader.Remaining > 0)
            {
                var length = reader.ReadInt32BigEndian();
                var type = reader.ReadMemory(4);
                var data = length > 0 ? reader.ReadMemory(length) : Memory<byte>.Empty;
                if (type.Span.SequenceEqual(IHDR))
                {
                    ReadHeader(data.Span);
                }
                else if (type.Span.SequenceEqual(IDAT))
                {
                    buffer.Write(data);
                }
                var crc = reader.ReadUInt32BigEndian();
                var calculatedCrc = CRC.Crc32(data, CRC.Crc32(type));
                Debug.Assert(crc == calculatedCrc, "Invalid CRC");
            }

            //buffer.Position = 0;
            using var inflater = new ZLibStream(buffer, CompressionMode.Decompress, false);
            var bytesRead = inflater.Read(bytes);
            Debug.Assert(bytesRead == bytes.Length);
        }

        private void ReadPng(Stream s)
        {
            using var reader = new BinaryReader(s);
            using var buffer = Extensions.Manager.GetStream();
            Span<byte> type = stackalloc byte[4];

            while (reader.PeekChar() != -1)
            {
                var length = reader.ReadInt32BigEndian();                
                reader.Read(type);
               
                if (length > 0)
                {
                    var rent = System.Buffers.ArrayPool<byte>.Shared.Rent(length);
                    var data = new ReadOnlySpan<byte>(rent, 0, length);
                    try
                    {
                        HandleData(buffer, type, data);
                        var crc = reader.ReadUInt32BigEndian();
                        var calculatedCrc = CRC.Crc32(data, CRC.Crc32(type));
                        Debug.Assert(crc == calculatedCrc, "Invalid CRC");
                    }
                    finally
                    {
                        System.Buffers.ArrayPool<byte>.Shared.Return(rent);
                    }
                }
                else
                {
                    var crc = reader.ReadUInt32BigEndian();
                    var calculatedCrc = CRC.Crc32(Span<byte>.Empty, CRC.Crc32(type));
                    Debug.Assert(crc == calculatedCrc, "Invalid CRC");
                }

            }

            buffer.Position = 0;
            using var inflater = new ZLibStream(buffer, CompressionMode.Decompress, true);
            var bytesRead = inflater.Read(bytes);
            Debug.Assert(bytesRead == bytes.Length);
        }

        private void HandleData(MemoryStream buffer, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
        {
            if (type.SequenceEqual(IHDR))
            {
                ReadHeader(data);
            }
            else if (type.SequenceEqual(PLTE)) { }
            else if (type.SequenceEqual(IDAT))
            {
                buffer.Write(data);
            }
            else if (type.SequenceEqual(IEND))
            {

            }
            else
            {
                var otherType = Encoding.ASCII.GetString(type);
            }
        }

        private void ReadHeader(ReadOnlySpan<byte> data)
        {
            Width = BinaryPrimitives.ReadInt32BigEndian(data);
            Height = BinaryPrimitives.ReadInt32BigEndian(data[4..]);
            bitDepth = data[8];
            size = Height * Width;
            ColorType = (ColorType)data[9];
            if (ColorType == ColorType.RGBA)
            {
                BitsPerPixel = 4;
            }
            if (ColorType == ColorType.RGB)
            {
                BitsPerPixel = 3;
            }
            bytes = new byte[size * bitDepth * BitsPerPixel / 8 + Height];
        }
    }
}
