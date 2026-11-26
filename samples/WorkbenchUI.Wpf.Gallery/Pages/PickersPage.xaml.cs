using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages
{
    public partial class PickersPage : UserControl
    {
        /// <summary>Demonstrates two-way sync between RangeSlider and ColorMapLegend (ColorScale).</summary>
        private readonly ColorScale _scale = new()
        {
            ColorMap = ColorMap.Jet,
            Minimum = 100,
            Maximum = 350,
            BelowRangeColor = Colors.Magenta,
            AboveRangeColor = Colors.White,
        };

        public PickersPage()
        {
            InitializeComponent();

            RangeLegend.Scale = _scale;

            // Bind RangeSlider values two-way to ColorScale Min/Max
            ContourRange.SetBinding(RangeSlider.LowerValueProperty,
                new Binding(nameof(ColorScale.Minimum)) { Source = _scale, Mode = BindingMode.TwoWay });
            ContourRange.SetBinding(RangeSlider.UpperValueProperty,
                new Binding(nameof(ColorScale.Maximum)) { Source = _scale, Mode = BindingMode.TwoWay });
        }

        private void ViewPreset_Click(object sender, RoutedEventArgs e) =>
            DropDownResult.Text = $"View preset: {((MenuItem)sender).Header}";

        private void SolveOption_Click(object sender, RoutedEventArgs e) =>
            DropDownResult.Text = $"Run option: {((MenuItem)sender).Header}";

        private void Solve_Click(object sender, RoutedEventArgs e) =>
            DropDownResult.Text = "Run Analysis (default settings)";
    }
}
