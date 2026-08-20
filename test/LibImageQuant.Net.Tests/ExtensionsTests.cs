using LibImageQuant.Net.Codec;
using LibImageQuant.Net.Core;
using System;
using Xunit;

namespace LibImageQuant.Net.Tests
{
    public class ExtensionsTests
    {
        [Fact]
        public void HasTransparency_FalseWhenAllOpaque()
        {
            ReadOnlySpan<Color> palette = new[]
            {
                new Color(255, 1, 2, 3),
                new Color(255, 4, 5, 6),
            };
            Assert.False(palette.HasTransparency());
        }

        [Fact]
        public void HasTransparency_TrueWhenAnyEntryIsNotFullyOpaque()
        {
            // The non-opaque entry deliberately comes last - this is exactly the ordering
            // Coder.cs used to assume couldn't happen (and silently dropped transparency for).
            ReadOnlySpan<Color> palette = new[]
            {
                new Color(255, 1, 2, 3),
                new Color(254, 4, 5, 6),
            };
            Assert.True(palette.HasTransparency());
        }

        [Fact]
        public void HasTransparency_EmptyPalette_IsFalse()
        {
            Assert.False(ReadOnlySpan<Color>.Empty.HasTransparency());
        }
    }
}
