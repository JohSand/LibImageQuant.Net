using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace LibImageQuant.Net.Core
{
    /// <summary>
    /// A class, not a struct: it owns a buffer rented from ArrayPool&lt;byte&gt;.Shared, and a
    /// disposable value type is a well-known footgun - every copy of a struct is an independent
    /// value that would still share (and could double-return, or read after another copy
    /// returned) the same underlying array. Dispose it, or wrap it in a using, once you're done
    /// reading from it.
    /// </summary>
    public sealed class QuantizationResult : IDisposable
    {
        private readonly byte[] _imageData;
        private readonly int _byteCount;

        private readonly Palette _palette;

        private bool _disposed;

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

        public ReadOnlySpan<byte> ImageData
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return new ReadOnlySpan<byte>(_imageData, 0, _byteCount);
            }
        }

        public ReadOnlySpan<Color> PaletteData
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                return new ReadOnlySpan<Color>(_palette.Entries, 0, _palette.Count);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            ArrayPool<byte>.Shared.Return(_imageData);
        }
    }
}