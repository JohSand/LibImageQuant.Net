using System;
using System.Runtime.InteropServices;

namespace LibImageQuant.Net.Core
{
    using liq_attr_ptr = IntPtr;
    using liq_image_ptr = IntPtr;
    using liq_result_ptr = IntPtr;
    using size_t = UIntPtr;
    public enum LiqError
    {
        LIQ_OK = 0,
        LIQ_QUALITY_TOO_LOW = 99,
        LIQ_VALUE_OUT_OF_RANGE = 100,
        LIQ_OUT_OF_MEMORY,
        LIQ_ABORTED,
        LIQ_BITMAP_NOT_AVAILABLE,
        LIQ_BUFFER_TOO_SMALL,
        LIQ_INVALID_POINTER,
    };

    public static partial class LibImageQuant
    {
        [LibraryImport(@"imagequant")]
        internal static partial liq_attr_ptr liq_attr_create();
        [LibraryImport(@"imagequant")]
        internal static partial liq_attr_ptr liq_attr_copy(liq_attr_ptr attr);
        [LibraryImport(@"imagequant")]
        internal static partial void liq_attr_destroy(liq_attr_ptr attr);

        [LibraryImport(@"imagequant")]
        internal static partial LiqError liq_set_max_colors(liq_attr_ptr attr, int colors);
        [LibraryImport(@"imagequant")]
        internal static partial int liq_get_max_colors(liq_attr_ptr attr);
        [LibraryImport(@"imagequant")]
        internal static partial LiqError liq_set_speed(liq_attr_ptr attr, int speed);
        [LibraryImport(@"imagequant")]
        internal static partial int liq_get_speed(liq_attr_ptr attr);
        [LibraryImport(@"imagequant")]
        internal static partial LiqError liq_set_min_opacity(liq_attr_ptr attr, int min);
        [LibraryImport(@"imagequant")]
        internal static partial int liq_get_min_opacity(liq_attr_ptr attr);
        [LibraryImport(@"imagequant")]
        internal static partial LiqError liq_set_min_posterization(liq_attr_ptr attr, int bits);
        [LibraryImport(@"imagequant")]
        internal static partial int liq_get_min_posterization(liq_attr_ptr attr);
        [LibraryImport(@"imagequant")]
        internal static partial LiqError liq_set_quality(liq_attr_ptr attr, int minimum, int maximum);
        [LibraryImport(@"imagequant")]
        internal static partial int liq_get_min_quality(liq_attr_ptr attr);
        [LibraryImport(@"imagequant")]
        internal static partial int liq_get_max_quality(liq_attr_ptr attr);
        [LibraryImport(@"imagequant")]
        internal static partial void liq_set_last_index_transparent(liq_attr_ptr attr, int is_last);

        [LibraryImport(@"imagequant")]
        unsafe internal static partial liq_image_ptr liq_image_create_rgba(liq_attr_ptr attr, byte* bitmap, int width, int height, double gamma);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void rgb_to_rgba_callback(
            [In, Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] Color[] row_out,
            int row_index,
            int width,
            liq_image_ptr user_info);

        [LibraryImport(@"imagequant")]
        unsafe internal static partial liq_image_ptr liq_image_create_custom(
            liq_attr_ptr attr,
            delegate* unmanaged<liq_image_ptr, int, int, liq_image_ptr, void> row_callback,
            liq_image_ptr user_info,
            int width,
            int height,
            double gamma);
        //public static partial liq_image_ptr liq_image_create_custom(liq_attr_ptr attr, rgb_to_rgba_callback row_callback, IntPtr user_info, int width, int height, double gamma);

        [LibraryImport(@"imagequant")]
        internal static partial LiqError liq_image_set_memory_ownership(liq_image_ptr image, int ownership_flags);
        [LibraryImport(@"imagequant")]
        internal static partial LiqError liq_image_add_fixed_color(liq_image_ptr img, Color color);
        [LibraryImport(@"imagequant")]
        internal static partial int liq_image_get_width(liq_image_ptr img);
        [LibraryImport(@"imagequant")]
        internal static partial int liq_image_get_height(liq_image_ptr img);
        [LibraryImport(@"imagequant")]
        internal static partial void liq_image_destroy(liq_image_ptr img);

        [LibraryImport(@"imagequant")]
        internal static partial liq_result_ptr liq_quantize_image(liq_attr_ptr attr, liq_image_ptr input_image);

        [LibraryImport(@"imagequant")]
        internal static partial LiqError liq_set_dithering_level(liq_result_ptr res, float dither_level);
        [LibraryImport(@"imagequant")]
        internal static partial LiqError liq_set_output_gamma(liq_result_ptr res, double gamma);
        [LibraryImport(@"imagequant")]
        internal static partial double liq_get_output_gamma(liq_result_ptr res);

        [LibraryImport(@"imagequant")]
        internal static partial liq_image_ptr liq_get_palette(liq_result_ptr res);

        [LibraryImport(@"imagequant")]
        internal static partial LiqError liq_write_remapped_image(liq_result_ptr res, liq_image_ptr input_image, [Out, MarshalAs(UnmanagedType.LPArray)] byte[] buffer, size_t buffer_size);

        [LibraryImport(@"imagequant")]
        internal static partial double liq_get_quantization_error(liq_result_ptr res);
        [LibraryImport(@"imagequant")]
        internal static partial int liq_get_quantization_quality(liq_result_ptr res);
        [LibraryImport(@"imagequant")]
        internal static partial double liq_get_remapping_error(liq_result_ptr res);
        [LibraryImport(@"imagequant")]
        internal static partial int liq_get_remapping_quality(liq_result_ptr res);

        [LibraryImport(@"imagequant")]
        internal static partial void liq_result_destroy(liq_result_ptr res);

        [LibraryImport(@"imagequant")]
        internal static partial int liq_version();
    }
}