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

    public static partial class Zopfli
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
                    // ZopfliCompress allocates this buffer with its own malloc/realloc, not
                    // with whatever backs Marshal.AllocHGlobal (LocalAlloc on Windows - a
                    // different heap). Free it through the matching allocator instead; see
                    // native/zopfli_free_shim.c.
                    ZopfliNetFree(result);
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
        [LibraryImport("zopfli")]
        unsafe internal static partial void ZopfliCompress(ref ZopfliOptions options, ZopfliFormat output_type, byte* data, int data_size, ref IntPtr data_out, ref uint data_out_size);

        /// <summary>
        /// Frees a buffer allocated by <see cref="ZopfliCompress"/>, via zopfli's own allocator
        /// (see native/zopfli_free_shim.c) rather than Marshal.FreeHGlobal.
        /// </summary>
        [LibraryImport("zopflibridge")]
        internal static partial void ZopfliNetFree(IntPtr ptr);
    }
}
