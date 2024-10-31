using LibImageQuant.Net.Core;
using Microsoft.IO;
using Soft160.Data.Cryptography;
using SpanDex;
using System;
using System.IO;

namespace LibImageQuant.Net.Codec
{
    using static Constants;
    public static class Extensions
    {
        internal static readonly RecyclableMemoryStreamManager Manager = new();

        public static int ReadInt32BigEndian(this BinaryReader reader) =>
            System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(reader.ReadBytes(sizeof(int)));

        public static uint ReadUInt32BigEndian(this BinaryReader reader) =>
            System.Buffers.Binary.BinaryPrimitives.ReadUInt32BigEndian(reader.ReadBytes(sizeof(int)));

        public static int GetLastAlphaIndex(this in ReadOnlySpan<Color> palette)
        {
            for (int i = 0; i < palette.Length; i++)
            {
                if (palette[i].Alpha == 255)
                    return i;
            }
            return palette.Length;
        }

        public static void WriteHeader(this ref SpanWriter writer, int height, int width, byte bpp)
        {
            writer.WriteInt32BigEndian(13);//size
            writer.WriteSpan(IHDR);//type header.
            //data
            writer.WriteInt32BigEndian(width);
            writer.WriteInt32BigEndian(height);
            writer.WriteByte(bpp); //bit depth
            writer.WriteByte((byte)ColorType.PLTE);
            writer.WriteByte(0); //Compression
            writer.WriteByte(0); //FilterMethod
            writer.WriteByte(0); //InterlaceMethod
            //CRC
            var crc = writer.CalculateCrc(4 + 13);
            writer.WriteUInt32BigEndian(crc);
        }

        public static void WritePalette(this ref SpanWriter writer, in ReadOnlySpan<Color> palette)
        {
            var size = palette.Length * 3;
            writer.WriteInt32BigEndian(size);//size
            writer.WriteSpan(PLTE);//type header.
            //data
            for (var i = 0; i < palette.Length; i++)
            {
                writer.WriteByte(palette[i].Red);
                writer.WriteByte(palette[i].Green);
                writer.WriteByte(palette[i].Blue);
            }
            //CRC
            var crc = writer.CalculateCrc(4 + size);
            writer.WriteUInt32BigEndian(crc);
        }

        public static void WriteData(this ref SpanWriter writer, in ReadOnlySpan<byte> deflatedData)
        {
            var size = deflatedData.Length;
            writer.WriteInt32BigEndian(size);//size
            writer.WriteSpan(IDAT);//type header.
                                   //data
            writer.WriteSpan(deflatedData);
            //CRC
            var crc = writer.CalculateCrc(4 + size);
            writer.WriteUInt32BigEndian(crc);
        }

        public static void WriteEnd(this ref SpanWriter writer)
        {
            writer.WriteInt32BigEndian(0);//size
            writer.WriteSpan(IEND);
            //CRC
            var crc = writer.CalculateCrc(4);
            writer.WriteUInt32BigEndian(crc);
        }
        public static void WriteTransparency(this ref SpanWriter writer, in ReadOnlySpan<Color> palette)
        {
            var size = palette.Length;
            writer.WriteInt32BigEndian(size);//size
            writer.WriteSpan(tRNS);//type header.
            //data
            for (var i = 0; i < palette.Length; i++)
            {
                writer.WriteByte(palette[i].Alpha);
                //writer.WriteByte(255);
            }
            //CRC
            var crc = writer.CalculateCrc(4 + size);
            writer.WriteUInt32BigEndian(crc);
        }

        private static uint CalculateCrc(this ref SpanWriter writer, int lookBack)
        {
            var crc = CRC.Crc32(writer.Span[(writer.Cursor - lookBack)..writer.Cursor]);
            return crc;
        }
    }
}
