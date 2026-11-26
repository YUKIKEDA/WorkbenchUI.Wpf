using System.Windows.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages
{
    public partial class InputsPage : UserControl
    {
        public InputsPage()
        {
            InitializeComponent();
            DataContext = new ValidationDemoModel();
        }
    }

    /// <summary>Demo model for validation error display (red border + tooltip).</summary>
    public class ValidationDemoModel
    {
        private double _thickness = 3.2;

        public double Thickness
        {
            get => _thickness;
            set
            {
                if (value is < 0 or > 100)
                {
                    throw new ArgumentException("Value must be between 0 and 100.");
                }

                _thickness = value;
            }
        }
    }
}
