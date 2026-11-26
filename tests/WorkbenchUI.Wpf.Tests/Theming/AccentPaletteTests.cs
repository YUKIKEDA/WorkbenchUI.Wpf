using System.Windows.Media;
using WorkbenchUI.Wpf.Theming;

namespace WorkbenchUI.Wpf.Tests.Theming
{
    public class AccentPaletteTests
    {
        private static readonly Color BaseBlue = Color.FromRgb(0x00, 0x7A, 0xCC);

        [Fact]
        public void FromBase_DefaultIsBaseColor()
        {
            var palette = AccentPalette.FromBase(BaseBlue);

            Assert.Equal(BaseBlue, palette.Default);
        }

        [Fact]
        public void FromBase_LightnessOrder_IsHoverDefaultPressedMuted()
        {
            var palette = AccentPalette.FromBase(BaseBlue);

            var (_, _, hoverL) = ColorMath.ToHsl(palette.Hover);
            var (_, _, defaultL) = ColorMath.ToHsl(palette.Default);
            var (_, _, pressedL) = ColorMath.ToHsl(palette.Pressed);
            var (_, _, mutedL) = ColorMath.ToHsl(palette.Muted);

            Assert.True(hoverL > defaultL, "Hover should be lighter than Default");
            Assert.True(pressedL < defaultL, "Pressed should be darker than Default");
            Assert.True(mutedL < pressedL, "Muted should be darker than Pressed");
        }

        [Fact]
        public void FromBase_LightVariant_MutedIsLighterThanDefault()
        {
            var palette = AccentPalette.FromBase(BaseBlue, WorkbenchThemeVariant.Light);

            var (_, _, defaultL) = ColorMath.ToHsl(palette.Default);
            var (_, _, mutedL) = ColorMath.ToHsl(palette.Muted);

            Assert.True(mutedL > defaultL, "In light theme, Muted should be a lighter tint than Default");
        }

        [Fact]
        public void FromBase_DarkVariant_MatchesDefaultOverload()
        {
            var explicitDark = AccentPalette.FromBase(BaseBlue, WorkbenchThemeVariant.Dark);

            Assert.Equal(AccentPalette.FromBase(BaseBlue), explicitDark);
        }

        [Fact]
        public void FromBase_AllDerivedColorsPreserveHue()
        {
            var palette = AccentPalette.FromBase(BaseBlue);
            var (baseH, _, _) = ColorMath.ToHsl(BaseBlue);

            foreach (var color in new[] { palette.Hover, palette.Pressed, palette.Muted })
            {
                var (h, _, _) = ColorMath.ToHsl(color);
                // Allow +/-2 degrees for hue drift caused by 8-bit RGB quantization
                Assert.InRange(h, baseH - 2, baseH + 2);
            }
        }
    }
}
