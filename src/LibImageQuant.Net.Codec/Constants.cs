using System.Text;

namespace LibImageQuant.Net.Codec
{
    public static class Constants
    {
        public static readonly byte[] Sig = { 137, 80, 78, 71, 13, 10, 26, 10 };
        public static readonly byte[] IHDR = Encoding.ASCII.GetBytes("IHDR");
        public static readonly byte[] PLTE = Encoding.ASCII.GetBytes("PLTE");
        public static readonly byte[] IDAT = Encoding.ASCII.GetBytes("IDAT");
        public static readonly byte[] IEND = Encoding.ASCII.GetBytes("IEND");
        public static readonly byte[] tRNS = { 116, 82, 78, 83 };
        public static readonly byte[] LineFilter = { 0 };
    }
}
