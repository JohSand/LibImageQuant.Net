using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Zopfli.Net
{
    /// <summary>
    /// Zopfli format options
    /// </summary>
    public enum ZopfliFormat
    {
        ZOPFLI_FORMAT_GZIP,
        ZOPFLI_FORMAT_ZLIB,
        ZOPFLI_FORMAT_DEFLATE
    };

    public static class Zopfli
    {
        public static unsafe void Compress(this Stream s, ReadOnlySpan<byte> span)
        {
            fixed (byte* p = span)
            {
                IntPtr result = IntPtr.Zero;
                uint bytesWritten = 0;
                try
                {
                    var opts = ZopfliOptions.Default();
                    ZopfliCompress(ref opts, ZopfliFormat.ZOPFLI_FORMAT_ZLIB, p, span.Length, ref result, ref bytesWritten);
                    var @out = new Span<byte>(result.ToPointer(), (int)bytesWritten);
                    s.Write(@out);
                }
                finally
                {
                    // Free unmanaged memory
                    Marshal.FreeHGlobal(result);
                }
            }
        }

        /// <summary>
        /// Compresses according to the given output format and appends the result to the output.
        /// </summary>
        /// <param name="options">Zopfli program options</param>
        /// <param name="output_type">The output format to use</param>
        /// <param name="data">Pointer to the data</param>
        /// <param name="data_size">This is the size of the memory block pointed to by data</param>
        /// <param name="data_out">Pointer to the dynamic output array to which the result is appended</param>
        /// <param name="data_out_size">This is the size of the memory block pointed to by the dynamic output array size</param>
        [DllImport("zopfli", CallingConvention = CallingConvention.Cdecl)]
        internal static unsafe extern void ZopfliCompress(ref ZopfliOptions options, ZopfliFormat output_type, byte* data, int data_size, ref IntPtr data_out, ref uint data_out_size);
    }
}
