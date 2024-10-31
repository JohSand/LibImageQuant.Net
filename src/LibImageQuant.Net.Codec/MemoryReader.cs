using System;
using LibImageQuant.Net.Codec;
using SpanDex.Extensions;

namespace LibImageQuant.Net.Codec
{
    /// <summary>
    /// Provides an easy way to read from a Span
    /// </summary>
    public ref struct MemoryReader
    {
        private readonly ReadOnlyMemory<byte> _memory;
        private int cursor;

        /// <summary>
        /// Initializes a SpanReader using the given memory
        /// </summary>
        /// <param name="memory">The memory to use</param>
        /// <param name="cursor">Optional initial cursor position (defaults to 0)</param>
        public MemoryReader(ReadOnlyMemory<byte> memory, int cursor = 0)
        {
            this._memory = memory;
            this.cursor = cursor;
        }

        public static implicit operator MemoryReader(byte[] array) => new MemoryReader(array);
        public static implicit operator MemoryReader(ArraySegment<byte> segment) => new MemoryReader(segment);


        /// <summary>
        /// The current cursor position
        /// </summary>
        public int Cursor => cursor;

        /// <summary>
        /// The space remaining in the memory
        /// </summary>
        public int Remaining => _memory.Length - cursor;

        /// <summary>
        /// The length of the memory
        /// </summary>
        public int Length => _memory.Length;

        /// <summary>
        /// Manually advances the cursor by the given length
        /// </summary>
        /// <param name="length">The amount (in bytes) to move the cursor</param>
        public void Advance(int length)
        {
            if (length <= 0)
                throw new ArgumentException("Length must be a non-negative integer");

            cursor = checked(cursor + length);
        }

        public short ReadInt16BigEndian() => _memory.ReadInt16BigEndian(ref cursor);

        public short ReadInt16LittleEndian() => _memory.ReadInt16LittleEndian(ref cursor);

        public int ReadInt32BigEndian() => _memory.ReadInt32BigEndian(ref cursor);

        public int ReadInt32LittleEndian() => _memory.ReadInt32LittleEndian(ref cursor);

        public long ReadInt64BigEndian() => _memory.ReadInt64BigEndian(ref cursor);

        public long ReadInt64LittleEndian() => _memory.ReadInt64LittleEndian(ref cursor);

        public ushort ReadUInt16BigEndian() => _memory.ReadUInt16BigEndian(ref cursor);

        public ushort ReadUInt16LittleEndian() => _memory.ReadUInt16LittleEndian(ref cursor);

        public uint ReadUInt32BigEndian() => _memory.ReadUInt32BigEndian(ref cursor);

        public uint ReadUInt32LittleEndian() => _memory.ReadUInt32LittleEndian(ref cursor);

        public ulong ReadUInt64BigEndian() => _memory.ReadUInt64BigEndian(ref cursor);

        public ulong ReadUInt64LittleEndian() => _memory.ReadUInt64LittleEndian(ref cursor);

        public ReadOnlyMemory<byte> ReadMemory(int size) => size > 0 ? _memory.ReadMemory(size, ref cursor) : ReadOnlyMemory<byte>.Empty;

        public ReadOnlySpan<byte> ReadSpan(int size) => size > 0 ? _memory.ReadMemory(size, ref cursor).Span : [];

        public string ReadAsciiString(int size) => _memory.ReadAsciiString(size, ref cursor);

        public string ReadUtf8string(int size) => _memory.ReadUtf8String(size, ref cursor);

        public byte ReadByte() => _memory.ReadByte(ref cursor);

        public bool TryReadInt16BigEndian(out short value) => _memory.TryReadInt16BigEndian(out value, ref cursor);

        public bool TryReadInt16LittleEndian(out short value) => _memory.TryReadInt16LittleEndian(out value, ref cursor);

        public bool TryReadInt32BigEndian(out int value) => _memory.TryReadInt32BigEndian(out value, ref cursor);

        public bool TryReadInt32LittleEndian(out int value) => _memory.TryReadInt32LittleEndian(out value, ref cursor);

        public bool TryReadInt64BigEndian(out long value) => _memory.TryReadInt64BigEndian(out value, ref cursor);

        public bool TryReadInt64LittleEndian(out long value) => _memory.TryReadInt64LittleEndian(out value, ref cursor);

        public bool TryReadUInt16BigEndian(out ushort value) => _memory.TryReadUInt16BigEndian(out value, ref cursor);

        public bool TryReadUInt16LittleEndian(out ushort value) => _memory.TryReadUInt16LittleEndian(out value, ref cursor);

        public bool TryReadUInt32BigEndian(out uint value) => _memory.TryReadUInt32BigEndian(out value, ref cursor);

        public bool TryReadUInt32LittleEndian(out uint value) => _memory.TryReadUInt32LittleEndian(out value, ref cursor);

        public bool TryReadUInt64BigEndian(out ulong value) => _memory.TryReadUInt64BigEndian(out value, ref cursor);

        public bool TryReadUInt64LittleEndian(out ulong value) => _memory.TryReadUInt64LittleEndian(out value, ref cursor);

        public bool TryReadSpan(out ReadOnlyMemory<byte> value, int size) => _memory.TryReadSpan(out value, size, ref cursor);

        public bool TryReadAsciiString(out string value, int size) => _memory.TryReadAsciiString(out value, size, ref cursor);

        public bool TryReadUtf8string(out string value, int size) => _memory.TryReadUtf8String(out value, size, ref cursor);

        public bool TryReadByte(out byte value) => _memory.TryReadByte(out value, ref cursor);
    }
}
