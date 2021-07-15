﻿using System.Runtime.InteropServices;

namespace Zopfli.Net
{
    /// <summary>
    /// Zopfli Options
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ZopfliOptions
    {
        // Whether to print output
        public int verbose;

        // Whether to print more detailed output
        public int verbose_more;

        // Maximum amount of times to rerun forward and backward pass to optimize LZ77
        // compression cost. Good values: 10, 15 for small files, 5 for files over
        // several MB in size or it will be too slow.
        public int numiterations;

        // If true, splits the data in multiple deflate blocks with optimal choice
        // for the block boundaries. Block splitting gives better compression. Default:
        // true (1).
        public int blocksplitting;

        // If true, chooses the optimal block split points only after doing the iterative
        // LZ77 compression. If false, chooses the block split points first, then does
        // iterative LZ77 on each individual block. Depending on the file, either first
        // or last gives the best compression. Default: false (0).
        public int blocksplittinglast;

        // Maximum amount of blocks to split into (0 for unlimited, but this can give
        // extreme results that hurt compression on some files). Default value: 15.
        public int blocksplittingmax;

        /// <summary>
        /// Initializes options used throughout the program with default values.
        /// </summary>
        public static ZopfliOptions Default()
        {
            return new ZopfliOptions
            {
                verbose = 0,
                verbose_more = 0,
                numiterations = 15,
                blocksplitting = 1,
                blocksplittinglast = 0,
                blocksplittingmax = 15,
            };
        }
    }
}
