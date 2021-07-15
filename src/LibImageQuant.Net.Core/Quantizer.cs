using System;
using System.Runtime.InteropServices;

namespace LibImageQuant.Net.Core
{
    public sealed class Quantizer : IDisposable
    {
        internal IntPtr Attr { get; }

        public Quantizer()
        {
            Attr = LibImageQuant.liq_attr_create();
            DitheringLevel = 0.6f;
        }

        void IDisposable.Dispose() => LibImageQuant.liq_attr_destroy(Attr);

        /// <summary>
        /// Enables/disables dithering. Dithering level must be between 0 and 1 (inclusive). Dithering level 0 enables fast non-dithered remapping. 
        /// Otherwise a variation of Floyd-Steinberg error diffusion is used.
        /// Precision of the dithering algorithm depends on the speed setting, see <see cref="Speed"/>.
        /// </summary>
        public float DitheringLevel { get; set; }

        public (int min, int max) Quality
        {
            get => (LibImageQuant.liq_get_min_quality(Attr), LibImageQuant.liq_get_max_quality(Attr));
            set => LibImageQuant.liq_set_quality(Attr, value.min, value.max);
        }

        /// <summary>
        /// Features dependent on speed 
        ///     Noise-sensitive dithering	        1 to 5
        ///     Forced posterization	            8-10 or if image has more than million colors
        ///     Quantization error known	        1-7 or if minimum quality is set
        ///     Additional quantization techniques	1-6
        /// </summary>
        public int Speed
        {
            get => LibImageQuant.liq_get_speed(Attr);
            set => LibImageQuant.liq_set_speed(Attr, value);
        }

        /// <summary>
        /// Ignores given number of least significant bits in all channels, posterizing image to 2^bits levels. 0 gives full quality. 
        /// Use 2 for VGA or 16-bit RGB565 displays, 4 if image is going to be output on a RGB444/RGBA4444 display (e.g. low-quality textures on Android).
        /// </summary>
        public int MinPosterization
        {
            get => LibImageQuant.liq_get_min_posterization(Attr);
            set => LibImageQuant.liq_set_min_posterization(Attr, value);
        }

        /// <summary>
        /// Specifies maximum number of colors to use. The default is 256. Instead of setting a fixed limit it's better to use <see cref="Quality"/>.
        /// </summary>
        public int MaxColors
        {
            get => LibImageQuant.liq_get_max_colors(Attr);
            set => LibImageQuant.liq_set_max_colors(Attr, value);
        }

        [Obsolete("This was a workaround for Internet Explorer 6, but because this browser is not used any more, this option has been deprecated and removed.")]
        public int MinOpacity
        {
            get => LibImageQuant.liq_get_min_opacity(Attr);
            set => LibImageQuant.liq_set_min_opacity(Attr, value);
        }

        /// <summary>
        /// Quantizes an image represented by <paramref name="imageBytes"/>. Image is assumed to be a simple, contiguous RGBA image
        /// </summary>
        /// <param name="imageBytes"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns>QuantizationResult</returns>
        public unsafe QuantizationResult Quantize(ReadOnlySpan<byte> imageBytes, int width, int height)
        {
            fixed (byte* p = imageBytes)
            {
                return Quantize(p, width, height);
            }
        }

        /// <summary>
        /// Quantizes an image represented by <paramref name="imageBytes"/>. Image is assumed to be a simple, contiguous RGBA image
        /// </summary>
        /// <param name="imageBytes"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns>QuantizationResult</returns>
        public unsafe QuantizationResult Quantize(IntPtr imageBytes, int width, int height)
        {
            return Quantize((byte*)imageBytes.ToPointer(), width, height);
        }

        /// <summary>
        /// For RGB, ABGR, YUV and all other formats that can be converted on-the-fly to RGBA (you have to supply the conversion function)
        /// </summary>
        /// <param name="ipi"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public QuantizationResult Quantize(IProvideImages ipi, int width, int height)
        {
            var handle = GCHandle.Alloc(ipi);
            try
            {
                return Quantize(width, height, handle);
            }
            finally
            {
                handle.Free();
            }
        }

        private unsafe QuantizationResult Quantize(int width, int height, GCHandle handle)
        {
            [UnmanagedCallersOnly]
            static unsafe void Callback(IntPtr rowPtr, int rowIndex, int width, IntPtr info)
            {
                var rowOut = new Span<Color>(rowPtr.ToPointer(), width);
                var provider = (IProvideImages)GCHandle.FromIntPtr(info).Target;
                provider.ProvideImageRow(rowOut, rowIndex);
            }

            var unmanagedImage = LibImageQuant.liq_image_create_custom(Attr, &Callback, GCHandle.ToIntPtr(handle), width, height, 0);
            try
            {
                return CreateQuantizer(width, height, unmanagedImage);
            }
            finally
            {
                LibImageQuant.liq_image_destroy(unmanagedImage);
            }
        }

        private unsafe QuantizationResult Quantize(byte* p, int width, int height)
        {
            var unmanagedImage = LibImageQuant.liq_image_create_rgba(Attr, p, width, height, 0);
            try
            {
                return CreateQuantizer(width, height, unmanagedImage);
            }
            finally
            {
                LibImageQuant.liq_image_destroy(unmanagedImage);
            }
        }


        private QuantizationResult CreateQuantizer(int width, int height, IntPtr unmanagedImage)
        {
            var quantizationResult = LibImageQuant.liq_quantize_image(Attr, unmanagedImage);
            try
            {
                LibImageQuant.liq_set_dithering_level(quantizationResult, DitheringLevel);

                return new QuantizationResult(quantizationResult, unmanagedImage, width * height);
            }
            finally
            {
                LibImageQuant.liq_result_destroy(quantizationResult);
            }
        }
    }
}