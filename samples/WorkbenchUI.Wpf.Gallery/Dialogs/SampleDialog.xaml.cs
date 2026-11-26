using System.Windows;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Gallery.Dialogs
{
    public partial class SampleDialog : WorkbenchDialogWindow
    {
        public SampleDialog()
        {
            InitializeComponent();
        }

        public string MaterialName => NameBox.Text;

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
