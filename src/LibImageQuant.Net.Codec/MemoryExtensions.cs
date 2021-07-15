using System;
using System.Buffers.Binary;
using System.Text;

namespace LibImageQuant.Net.Codec
{
    internal static class MemoryExtensions
    {
        internal static short ReadInt16BigEndian(this ReadOnlyMemory<byte> source, ref int cursor)
        {
            ValidateCursor(cursor);
            return BinaryPrimitives.ReadInt16BigEndian(source.SliceAndAdvance(ref cursor, 2).Span);
        }

        internal static short ReadInt16LittleEndian(this ReadOnlyMemory<byte> source, ref int cursor)
        {
            ValidateCursor(cursor);
            return BinaryPrimitives.ReadInt16LittleEndian(source.SliceAndAdvance(ref cursor, 2).Span);
        }

        internal static int ReadInt32BigEndian(this ReadOnlyMemory<byte> source, ref int cursor)
        {
            ValidateCursor(cursor);
            return BinaryPrimitives.ReadInt32BigEndian(source.SliceAndAdvance(ref cursor, 4).Span);
        }

        internal static int ReadInt32LittleEndian(this ReadOnlyMemory<byte> source, ref int cursor)
        {
            ValidateCursor(cursor);
            return BinaryPrimitives.ReadInt32LittleEndian(source.SliceAndAdvance(ref cursor, 4).Span);
        }

        internal static long ReadInt64BigEndian(this ReadOnlyMemory<byte> source, ref int cursor)
        {
            ValidateCursor(cursor);
            return BinaryPrimitives.ReadInt64BigEndian(source.SliceAndAdvance(ref cursor, 8).Span);
        }

        internal static long ReadInt64LittleEndian(this ReadOnlyMemory<byte> source, ref int cursor)
        {
            ValidateCursor(cursor);
            return BinaryPrimitives.ReadInt64LittleEndian(source.SliceAndAdvance(ref cursor, 8).Span);
        }

        internal static ushort ReadUInt16BigEndian(this ReadOnlyMemory<byte> source, ref int cursor)
        {
            ValidateCursor(cursor);
            return BinaryPrimitives.ReadUInt16BigEndian(source.SliceAndAdvance(ref cursor, 2).Span);
        }

        internal static ushort ReadUInt16LittleEndian(this ReadOnlyMemory<byte> source, ref int cursor)
        {
            ValidateCursor(cursor);
            return BinaryPrimitives.ReadUInt16LittleEndian(source.SliceAndAdvance(ref cursor, 2).Span);
        }

        internal static uint ReadUInt32BigEndian(this ReadOnlyMemory<byte> source, ref int cursor)
        {
            ValidateCursor(cursor);
            return BinaryPrimitives.ReadUInt32BigEndian(source.SliceAndAdvance(ref cursor, 4).Span);
        }

        internal static uint ReadUInt32LittleEndian(this ReadOnlyMemory<byte> source, ref int cursor)
        {
            ValidateCursor(cursor);
            return BinaryPrimitives.ReadUInt32LittleEndian(source.SliceAndAdvance(ref cursor, 4).Span);
        }

        internal static ulong ReadUInt64BigEndian(this ReadOnlyMemory<byte> source, ref int cursor)
        {
            ValidateCursor(cursor);
            return BinaryPrimitives.ReadUInt64BigEndian(source.SliceAndAdvance(ref cursor, 8).Span);
        }

        internal static ulong ReadUInt64LittleEndian(this ReadOnlyMemory<byte> source, ref int cursor)
        {
            ValidateCursor(cursor);
            return BinaryPrimitives.ReadUInt64LittleEndian(source.SliceAndAdvance(ref cursor, 8).Span);
        }

        internal static ReadOnlyMemory<byte> ReadMemory(this ReadOnlyMemory<byte> source, int size, ref int cursor)
        {
            ValidateCursor(cursor);
            return source.SliceAndAdvance(ref cursor, size);
        }

        internal static string ReadAsciiString(this ReadOnlyMemory<byte> source, int size, ref int cursor)
        {
            ValidateCursor(cursor);
            var slice = ReadMemory(source, size, ref cursor);
            return Encoding.ASCII.GetString(slice.ToArray());
        }

        internal static string ReadUtf8String(this ReadOnlyMemory<byte> source, int size, ref int cursor)
        {
            ValidateCursor(cursor);
            var slice = ReadMemory(source, size, ref cursor);
            return Encoding.UTF8.GetString(slice.ToArray());
        }

        internal static byte ReadByte(this ReadOnlyMemory<byte> source, ref int cursor)
        {
            ValidateCursor(cursor);
            var bite = source.Span[cursor];
            IncrementCursor(ref cursor, 1);
            return bite;
        }

        internal static bool TryReadInt16BigEndian(this ReadOnlyMemory<byte> source, out short value, ref int cursor)
        {
            if (!ValidateTryRead(source, out value, cursor, 2))
                return false;

            return TryAdvance(BinaryPrimitives.TryReadInt16BigEndian(source.Slice(cursor, 2).Span, out value), ref cursor, 2);
        }

        internal static bool TryReadInt16LittleEndian(this ReadOnlyMemory<byte> source, out short value, ref int cursor)
        {
            if (!ValidateTryRead(source, out value, cursor, 2))
                return false;

            return TryAdvance(BinaryPrimitives.TryReadInt16LittleEndian(source.Slice(cursor, 2).Span, out value), ref cursor, 2);
        }

        internal static bool TryReadInt32BigEndian(this ReadOnlyMemory<byte> source, out int value, ref int cursor)
        {
            if (!ValidateTryRead(source, out value, cursor, 4))
                return false;

            return TryAdvance(BinaryPrimitives.TryReadInt32BigEndian(source.Slice(cursor, 4).Span, out value), ref cursor, 4);
        }

        internal static bool TryReadInt32LittleEndian(this ReadOnlyMemory<byte> source, out int value, ref int cursor)
        {
            if (!ValidateTryRead(source, out value, cursor, 4))
                return false;

            return TryAdvance(BinaryPrimitives.TryReadInt32LittleEndian(source.Slice(cursor, 4).Span, out value), ref cursor, 4);
        }

        internal static bool TryReadInt64BigEndian(this ReadOnlyMemory<byte> source, out long value, ref int cursor)
        {
            if (!ValidateTryRead(source, out value, cursor, 8))
                return false;

            return TryAdvance(BinaryPrimitives.TryReadInt64BigEndian(source.Slice(cursor, 8).Span, out value), ref cursor, 8);
        }

        internal static bool TryReadInt64LittleEndian(this ReadOnlyMemory<byte> source, out long value, ref int cursor)
        {
            if (!ValidateTryRead(source, out value, cursor, 8))
                return false;

            return TryAdvance(BinaryPrimitives.TryReadInt64LittleEndian(source.Slice(cursor, 8).Span, out value), ref cursor, 8);
        }

        internal static bool TryReadUInt16BigEndian(this ReadOnlyMemory<byte> source, out ushort value, ref int cursor)
        {
            if (!ValidateTryRead(source, out value, cursor, 2))
                return false;

            return TryAdvance(BinaryPrimitives.TryReadUInt16BigEndian(source.Slice(cursor, 2).Span, out value), ref cursor, 2);
        }

        internal static bool TryReadUInt16LittleEndian(this ReadOnlyMemory<byte> source, out ushort value, ref int cursor)
        {
            if (!ValidateTryRead(source, out value, cursor, 2))
                return false;

            return TryAdvance(BinaryPrimitives.TryReadUInt16LittleEndian(source.Slice(cursor, 2).Span, out value), ref cursor, 2);
        }

        internal static bool TryReadUInt32BigEndian(this ReadOnlyMemory<byte> source, out uint value, ref int cursor)
        {
            if (!ValidateTryRead(source, out value, cursor, 4))
                return false;

            return TryAdvance(BinaryPrimitives.TryReadUInt32BigEndian(source.Slice(cursor, 4).Span, out value), ref cursor, 4);
        }

        internal static bool TryReadUInt32LittleEndian(this ReadOnlyMemory<byte> source, out uint value, ref int cursor)
        {
            if (!ValidateTryRead(source, out value, cursor, 4))
                return false;

            return TryAdvance(BinaryPrimitives.TryReadUInt32LittleEndian(source.Slice(cursor, 4).Span, out value), ref cursor, 4);
        }

        internal static bool TryReadUInt64BigEndian(this ReadOnlyMemory<byte> source, out ulong value, ref int cursor)
        {
            if (!ValidateTryRead(source, out value, cursor, 8))
                return false;

            return TryAdvance(BinaryPrimitives.TryReadUInt64BigEndian(source.Slice(cursor, 8).Span, out value), ref cursor, 8);
        }

        internal static bool TryReadUInt64LittleEndian(this ReadOnlyMemory<byte> source, out ulong value, ref int cursor)
        {
            if (!ValidateTryRead(source, out value, cursor, 8))
                return false;

            return TryAdvance(BinaryPrimitives.TryReadUInt64LittleEndian(source.Slice(cursor, 8).Span, out value), ref cursor, 8);
        }

        internal static bool TryReadSpan(this ReadOnlyMemory<byte> source, out ReadOnlyMemory<byte> value, int size, ref int cursor)
        {
            ValidateCursor(cursor);
            value = default;
            if (source.HasSpace(cursor + size))
            {
                value = ReadMemory(source, size, ref cursor);
                return true;
            }
            return false;
        }

        internal static bool TryReadAsciiString(this ReadOnlyMemory<byte> source, out string value, int size, ref int cursor)
        {
            ValidateCursor(cursor);
            value = default;
            if (source.HasSpace(cursor + size))
            {
                value = ReadAsciiString(source, size, ref cursor);
                return true;
            }
            return false;
        }

        internal static bool TryReadUtf8String(this ReadOnlyMemory<byte> source, out string value, int size, ref int cursor)
        {
            ValidateCursor(cursor);
            value = default;
            if (source.HasSpace(cursor + size))
            {
                value = ReadUtf8String(source, size, ref cursor);
                return true;
            }
            return false;
        }

        internal static bool TryReadByte(this ReadOnlyMemory<byte> source, out byte value, ref int cursor)
        {
            ValidateCursor(cursor);

            value = default;
            if (source.HasSpace(cursor + 1))
            {
                value = ReadByte(source, ref cursor);
                return true;
            }
            return false;
        }

        private static bool ValidateTryRead<T>(this ReadOnlyMemory<byte> source, out T value, int cursor, int size)
        {
            ValidateCursor(cursor);

            value = default;
            return HasSpace(source, cursor + size);
        }

        private static ReadOnlyMemory<byte> SliceAndAdvance(this ReadOnlyMemory<byte> source, ref int cursor, int size)
        {
            if (size <= 0)
                throw new ArgumentException("Size must be a positive integer");

            ReadOnlyMemory<byte> slice = source.Slice(cursor, size);
            IncrementCursor(ref cursor, size);
            return slice;
        }

        private static bool TryAdvance(bool succeeded, ref int cursor, int size)
        {
            if (succeeded)
                IncrementCursor(ref cursor, size);
            return succeeded;
        }

        private static void IncrementCursor(ref int cursor, int size) => cursor = checked(cursor + size);

        private static bool HasSpace(this ReadOnlyMemory<byte> source, int space) => source.Length >= space;

        private static void ValidateCursor(int cursor)
        {
            if (cursor < 0)
                throw new ArgumentException("Cursor cannot be less than 0");
        }
    }
}
