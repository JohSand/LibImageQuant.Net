using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LibImageQuant.Net.Core
{
    [DebuggerDisplay("{Alpha}, {Red}, {Green}, {Blue}")]
    [StructLayout(LayoutKind.Explicit)]
    public readonly struct Color : IEquatable<Color>
    {
        [FieldOffset(3)]
        public readonly byte Alpha;
        [FieldOffset(2)]
        public readonly byte Red;
        [FieldOffset(1)]
        public readonly byte Green;
        [FieldOffset(0)]
        public readonly byte Blue;

        [FieldOffset(0)]
        public readonly int Argb;

        public Color(int argb) : this()
        {
            Argb = argb;
            Debug.Assert(Alpha == (uint)argb >> 24);
            Debug.Assert(Red == ((uint)(argb >> 16) & 255));
            Debug.Assert(Green == ((uint)(argb >> 8) & 255));
            Debug.Assert(Blue == ((uint)argb & 255));
        }

        public Color(byte a, byte r, byte g, byte b) : this()
        {
            Alpha = a;
            Red = r;
            Green = g;
            Blue = b;
        }

        public bool Equals(Color other) => other.Argb == Argb;

        public override int GetHashCode() => Argb;

        public static Color operator +(in Color c1, in Color c2) => new
            (
                unchecked((byte)((c1.Alpha + c2.Alpha) % 256)),
                unchecked((byte)((c1.Red + c2.Red) % 256)),
                unchecked((byte)((c1.Green + c2.Green) % 256)),
                unchecked((byte)((c1.Blue + c2.Blue) % 256))
            );

        public static Color operator -(in Color c1, in Color c2) => new 
            (
                unchecked((byte)((c1.Alpha - c2.Alpha) % 256)),
                unchecked((byte)((c1.Red - c2.Red) % 256)),
                unchecked((byte)((c1.Green - c2.Green) % 256)),
                unchecked((byte)((c1.Blue - c2.Blue) % 256))
            );

        public override bool Equals(object obj) => obj is Color c && Equals(c);

        public static bool operator ==(Color left, Color right) => left.Equals(right);

        public static bool operator !=(Color left, Color right) => !(left == right);
    }
}