using System;
using System.Runtime.CompilerServices;
using LibImageQuant.Net.Core;

namespace LibImageQuant.Net.Codec
{
    public interface IFiller
    {
        void Fill(ref Span<Color> rowOut, in ReadOnlySpan<byte> buffer);
    }

    public readonly struct ARGBFiller : IFiller
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(ref Span<Color> rowOut, in ReadOnlySpan<byte> scanLine)
        {
            for (var i = 0; i < rowOut.Length; i++)
            {
                var a = scanLine[(i * 4) + 3];
                var b = scanLine[(i * 4) + 2];
                var g = scanLine[(i * 4) + 1];
                var r = scanLine[(i * 4) + 0];
                rowOut[i] = new Color(a, r, g, b);
            }
        }
    }

    public readonly struct RGBFiller : IFiller
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Fill(ref Span<Color> rowOut, in ReadOnlySpan<byte> scanLine)
        {
            for (var i = 0; i < rowOut.Length; i++)
            {
                var b = scanLine[(i * 3) + 2];
                var g = scanLine[(i * 3) + 1];
                var r = scanLine[(i * 3) + 0];
                rowOut[i] = new Color(255, r, g, b);
            }
        }
    }

    public class ManagedProvider<T> : IProvideImages where T : struct, IFiller
    {
        public int Width => Decoder.Width;
        public int Height => Decoder.Height;

        private readonly T _filler;
        public ManagedProvider(Decoder decoder)
        {
            Decoder = decoder;
            _filler = default;
        }

        private Decoder Decoder { get; }

        public void ProvideImageRow(Span<Color> rowOut, int rowIndex)
        {
            var scanLine = Decoder.GetScanLine(rowIndex);
            _filler.Fill(ref rowOut, in scanLine);
        }


    }
}
