namespace LibImageQuant.Net.Codec
{
    /// <summary>
    /// PNG scanline filter selection for Coder's encode path. The fixed values (None..Paeth)
    /// apply that one filter type to every row - None is the behavior Coder has always used.
    /// MinSum and Entropy pick per-row, using the PNG spec's own adaptive-filtering heuristics
    /// (also lodepng's LFS_MINSUM/LFS_ENTROPY, as used by zopflipng). Adaptive goes further,
    /// mirroring zopflipng's own filter search: it trial-compresses every strategy above and
    /// keeps whichever produces the smallest output for this specific image.
    /// </summary>
    public enum PngFilterStrategy
    {
        None = 0,
        Sub = 1,
        Up = 2,
        Average = 3,
        Paeth = 4,
        MinSum,
        Entropy,
        Adaptive,
    }
}
