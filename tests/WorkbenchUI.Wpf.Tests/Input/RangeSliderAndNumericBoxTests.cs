using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Tests.Input;

public class RangeSliderAndNumericBoxTests
{
    [Fact]
    public void RangeSlider_SetMinimumBeforeMaximum_DoesNotThrow()
    {
        StaTestRunner.Run(() =>
        {
            var slider = new RangeSlider
            {
                Minimum = 10.0,
                Maximum = 100.0,
                LowerValue = 20.0,
                UpperValue = 80.0,
            };

            Assert.Equal(10.0, slider.Minimum);
            Assert.Equal(100.0, slider.Maximum);
            Assert.Equal(20.0, slider.LowerValue);
            Assert.Equal(80.0, slider.UpperValue);
        });
    }

    [Fact]
    public void RangeSlider_SetNegativeMaximumBeforeMinimum_DoesNotThrow()
    {
        StaTestRunner.Run(() =>
        {
            var slider = new RangeSlider
            {
                Maximum = -10.0,
                Minimum = -100.0,
                LowerValue = -80.0,
                UpperValue = -20.0,
            };

            Assert.Equal(-100.0, slider.Minimum);
            Assert.Equal(-10.0, slider.Maximum);
            Assert.Equal(-80.0, slider.LowerValue);
            Assert.Equal(-20.0, slider.UpperValue);
        });
    }

    [Fact]
    public void NumericBox_UnmodifiedLostFocus_PreservesFullPrecision()
    {
        StaTestRunner.Run(() =>
        {
            const double exactValue = 1.23456789;
            var box = new NumericBox
            {
                Format = "F2",
                Value = exactValue,
            };

            box.BeginInit();
            box.EndInit();
            box.Measure(new Size(200, 40));
            box.Arrange(new Rect(0, 0, 200, 40));
            box.UpdateLayout();
            var textBox = Assert.IsType<TextBox>(box.Template.FindName("PART_TextBox", box));
            textBox.RaiseEvent(new KeyboardFocusChangedEventArgs(Keyboard.PrimaryDevice, 0, textBox, box)
            {
                RoutedEvent = UIElement.LostKeyboardFocusEvent,
            });

            Assert.Equal(exactValue, box.Value);
        });
    }

    [Fact]
    public void NumericBox_ReadOnly_IgnoresCommit()
    {
        StaTestRunner.Run(() =>
        {
            var box = new NumericBox
            {
                IsReadOnly = true,
                Value = 5.0,
            };

            box.BeginInit();
            box.EndInit();
            box.Measure(new Size(200, 40));
            box.Arrange(new Rect(0, 0, 200, 40));
            box.UpdateLayout();
            var textBox = Assert.IsType<TextBox>(box.Template.FindName("PART_TextBox", box));
            textBox.Text = "99.0";
            textBox.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(textBox) ?? new DummyPresentationSource(), 0, Key.Enter)
            {
                RoutedEvent = UIElement.PreviewKeyDownEvent,
            });

            Assert.Equal(5.0, box.Value);
        });
    }

    private sealed class DummyPresentationSource : PresentationSource
    {
        protected override System.Windows.Media.CompositionTarget GetCompositionTargetCore() => null!;
        public override System.Windows.Media.Visual RootVisual { get; set; } = null!;
        public override bool IsDisposed => false;
    }
}
