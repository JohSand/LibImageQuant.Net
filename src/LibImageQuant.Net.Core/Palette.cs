using System.Runtime.InteropServices;

namespace LibImageQuant.Net.Core
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Palette
    {
        public readonly int Count;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public readonly Color[] Entries;
    };
}