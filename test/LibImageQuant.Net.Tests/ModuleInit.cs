using System.Runtime.CompilerServices;
using VerifyTests;

namespace LibImageQuant.Net.Tests
{
    public static class ModuleInit
    {
        [ModuleInitializer]
        public static void Init() =>
            // SSIM (structural similarity), not byte-exact: quantization is deterministic on a
            // given machine (confirmed empirically), but libimagequant v4's multi-threading
            // means work-stealing order isn't guaranteed to produce byte-identical
            // floating-point accumulation across different OSes/CPU core counts. 0.999 catches
            // a real regression (wrong colors, scrambled rows, a bad palette entry all drop
            // SSIM well below this) while tolerating that kind of noise - which is what lets
            // VisualTests run on every CI leg instead of being restricted to one.
            VerifyImageSharp.Initialize(ssimThreshold: 0.999);
    }
}
