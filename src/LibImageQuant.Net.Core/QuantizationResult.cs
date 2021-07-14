using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace LibImageQuant.Net.Core
{
    public readonly struct QuantizationResult : IDisposable
    {
        private readonly byte[] _imageData;
        private readonly int _byteCount;

        private readonly Palette _palette;

        public QuantizationResult(IntPtr quantizationResult, IntPtr unmanagedImage, int byteCount)
        {
            _palette = Marshal.PtrToStructure<Palette>(LibImageQuant.liq_get_palette(quantizationResult));

            var imageData = ArrayPool<byte>.Shared.Rent(byteCount);
#if DEBUG
            var quantErr = LibImageQuant.liq_get_quantization_error(quantizationResult);
            System.Diagnostics.Debug.WriteLine("Quantization error: " + quantErr);
            var quantQual = LibImageQuant.liq_get_quantization_quality(quantizationResult);
            System.Diagnostics.Debug.WriteLine("Quantization quality: " + quantQual);
#endif

            var remapResult = LibImageQuant.liq_write_remapped_image(quantizationResult, unmanagedImage, imageData, (UIntPtr)byteCount);

#if DEBUG
            var remmapErr2 = LibImageQuant.liq_get_remapping_error(quantizationResult);
            System.Diagnostics.Debug.WriteLine("Remapping error: " + remmapErr2);
            var remapQual2 = LibImageQuant.liq_get_remapping_quality(quantizationResult);
            System.Diagnostics.Debug.WriteLine("Remapping quality: " + remapQual2);
#endif

            if (remapResult != LiqError.LIQ_OK)
            {
                ArrayPool<byte>.Shared.Return(imageData);
                throw new Exception("" + remapResult);
            }

            _imageData = imageData;
            _byteCount = byteCount;
        }

        public ReadOnlySpan<byte> ImageData => new(_imageData, 0, _byteCount);

        public ReadOnlySpan<Color> PaletteData => new(_palette.Entries);

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(_imageData);
        }
    }
}