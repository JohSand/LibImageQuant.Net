using LibImageQuant.Net.Core;
using Xunit;

namespace LibImageQuant.Net.Tests
{
    /// <summary>
    /// Smoke tests for Quantizer's property getters/setters - each one is a thin P/Invoke
    /// passthrough to libimagequant's liq_attr, so a round trip here also guards against a
    /// marshaling regression in the underlying LibraryImport declarations.
    /// </summary>
    public class QuantizerTests
    {
        [Fact]
        public void Quality_RoundTripsThroughNativeAttr()
        {
            using var quantizer = new Quantizer();
            quantizer.Quality = (10, 90);
            Assert.Equal((10, 90), quantizer.Quality);
        }

        [Fact]
        public void MaxColors_RoundTripsThroughNativeAttr()
        {
            using var quantizer = new Quantizer();
            quantizer.MaxColors = 32;
            Assert.Equal(32, quantizer.MaxColors);
        }

        [Fact]
        public void Speed_RoundTripsThroughNativeAttr()
        {
            using var quantizer = new Quantizer();
            quantizer.Speed = 3;
            Assert.Equal(3, quantizer.Speed);
        }

        [Fact]
        public void MinPosterization_RoundTripsThroughNativeAttr()
        {
            using var quantizer = new Quantizer();
            quantizer.MinPosterization = 2;
            Assert.Equal(2, quantizer.MinPosterization);
        }

        [Fact]
        public void DitheringLevel_DefaultsToPointSix()
        {
            using var quantizer = new Quantizer();
            Assert.Equal(0.6f, quantizer.DitheringLevel);
        }

        [Fact]
        public void MaxColors_DefaultsTo256()
        {
            using var quantizer = new Quantizer();
            Assert.Equal(256, quantizer.MaxColors);
        }
    }
}
