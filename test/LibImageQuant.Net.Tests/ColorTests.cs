using LibImageQuant.Net.Core;
using Xunit;

namespace LibImageQuant.Net.Tests
{
    public class ColorTests
    {
        [Fact]
        public void Constructor_FromArgbInt_MatchesComponentConstructor()
        {
            var fromComponents = new Color(255, 10, 20, 30);
            // Byte layout matches native liq_color { r, g, b, a } (see Color.cs), so as a
            // little-endian int this reads high-to-low as A,B,G,R rather than the more common
            // A,R,G,B packing.
            var argb = unchecked((int)0xFF1E140Au); // A=FF, B=1E(30), G=14(20), R=0A(10)
            var fromArgb = new Color(argb);

            Assert.Equal(fromComponents, fromArgb);
        }

        [Fact]
        public void Equals_ComparesAllChannels()
        {
            var a = new Color(255, 1, 2, 3);
            var b = new Color(255, 1, 2, 3);
            var differentAlpha = new Color(254, 1, 2, 3);
            var differentRed = new Color(255, 9, 2, 3);

            Assert.Equal(a, b);
            Assert.True(a == b);
            Assert.NotEqual(a, differentAlpha);
            Assert.NotEqual(a, differentRed);
            Assert.True(a != differentRed);
        }

        [Fact]
        public void GetHashCode_IsConsistentWithEquality()
        {
            var a = new Color(200, 50, 100, 150);
            var b = new Color(200, 50, 100, 150);

            Assert.Equal(a.GetHashCode(), b.GetHashCode());
        }

        [Theory]
        [InlineData((byte)250, (byte)10, (byte)4)]   // 260 wraps to 4
        [InlineData((byte)100, (byte)50, (byte)150)] // no wrap
        public void AdditionOperator_WrapsModulo256(byte left, byte right, byte expected)
        {
            var c1 = new Color(0, left, 0, 0);
            var c2 = new Color(0, right, 0, 0);
            var sum = c1 + c2;
            Assert.Equal(expected, sum.Red);
        }

        [Fact]
        public void SubtractionOperator_WrapsModulo256()
        {
            var c1 = new Color(0, 5, 0, 0);
            var c2 = new Color(0, 10, 0, 0);
            var diff = c1 - c2; // 5-10 = -5, wraps to 251
            Assert.Equal((byte)251, diff.Red);
        }
    }
}
