using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages
{
    public partial class MiscInputsPage : UserControl
    {
        public record LoadCase(string Name);

        private readonly ObservableCollection<string> _selectedComponents = [];
        private readonly ObservableCollection<LoadCase> _selectedCases = [];
        private readonly EventHandler _matrixValuesHandler;
        private readonly EventHandler _gestureHandler;
        private bool _descriptorsHooked;

        public MiscInputsPage()
        {
            InitializeComponent();
            _matrixValuesHandler = (_, _) => UpdateMatrixInfo();
            _gestureHandler = (_, _) => UpdateGestureInfo();

            // CheckComboBox: result components (count display + Select All)
            ComponentCombo.ItemsSource = new[]
            {
                "Sx", "Sy", "Sz", "Sxy", "Syz", "Szx", "Von Mises", "Principal P1", "Principal P2", "Principal P3",
            };
            ComponentCombo.SelectedItems = _selectedComponents;
            _selectedComponents.CollectionChanged += OnSelectionChanged;
            _selectedComponents.Add("Sx");
            _selectedComponents.Add("Von Mises");

            // CheckComboBox: load cases (joined names via DisplayMemberPath)
            var cases = new[]
            {
                new LoadCase("Gravity"), new LoadCase("Wind X"), new LoadCase("Wind Y"),
                new LoadCase("Seismic X"), new LoadCase("Seismic Y"), new LoadCase("Thermal"),
            };
            CaseCombo.ItemsSource = cases;
            CaseCombo.SelectedItems = _selectedCases;
            _selectedCases.Add(cases[0]);
            _selectedCases.Add(cases[3]);

            // MatrixBox: symmetric stiffness (GPa) and coordinate transform
            StiffnessMatrix.RowHeaders = new[] { "X", "Y", "Z" };
            StiffnessMatrix.ColumnHeaders = new[] { "X", "Y", "Z" };
            StiffnessMatrix.Values = new[,]
            {
                { 210.0, 80.0, 80.0 },
                { 80.0, 210.0, 80.0 },
                { 80.0, 80.0, 210.0 },
            };

            TransformMatrix.Values = new[,]
            {
                { 1.0, 0.0, 0.0 },
                { 0.0, 1.0, 0.0 },
                { 0.0, 0.0, 1.0 },
            };

            // KeyGestureBox
            RunGestureBox.Gesture = new System.Windows.Input.KeyGesture(
                System.Windows.Input.Key.F5, System.Windows.Input.ModifierKeys.Control);
            UpdateGestureInfo();

            // Wizard: control CanGoNext via the Step 2 checkbox
            SetupWizard.CurrentIndex = 0;
            UpdateWizardCanGoNext();

            UpdateComboInfo();
            UpdateMatrixInfo();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!_descriptorsHooked)
            {
                System.ComponentModel.DependencyPropertyDescriptor
                    .FromProperty(MatrixBox.ValuesProperty, typeof(MatrixBox))
                    .AddValueChanged(StiffnessMatrix, _matrixValuesHandler);
                System.ComponentModel.DependencyPropertyDescriptor
                    .FromProperty(KeyGestureBox.GestureProperty, typeof(KeyGestureBox))
                    .AddValueChanged(RunGestureBox, _gestureHandler);
                _descriptorsHooked = true;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_descriptorsHooked)
            {
                System.ComponentModel.DependencyPropertyDescriptor
                    .FromProperty(MatrixBox.ValuesProperty, typeof(MatrixBox))
                    .RemoveValueChanged(StiffnessMatrix, _matrixValuesHandler);
                System.ComponentModel.DependencyPropertyDescriptor
                    .FromProperty(KeyGestureBox.GestureProperty, typeof(KeyGestureBox))
                    .RemoveValueChanged(RunGestureBox, _gestureHandler);
                _descriptorsHooked = false;
            }
        }

        private void OnSelectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateComboInfo();

        private void UpdateComboInfo() =>
            ComboInfo.Text = $"SelectedItems: [{string.Join(", ", _selectedComponents)}]";

        private void UpdateMatrixInfo()
        {
            if (StiffnessMatrix.Values is not double[,] v)
            {
                return;
            }

            var sb = new StringBuilder("Values =");
            for (var r = 0; r < v.GetLength(0); r++)
            {
                sb.AppendLine().Append("  ");
                for (var c = 0; c < v.GetLength(1); c++)
                {
                    sb.Append($"{v[r, c],7:G4}");
                }
            }

            MatrixInfo.Text = sb.ToString();
        }

        private void UpdateGestureInfo() =>
            GestureInfo.Text = RunGestureBox.Gesture is { } g
                ? $"Gesture: {new System.Windows.Input.KeyGestureConverter().ConvertToString(g)}"
                : "Gesture: (Unassigned)";

        private void ConditionCheck_Changed(object sender, RoutedEventArgs e) => UpdateWizardCanGoNext();

        private void UpdateWizardCanGoNext()
        {
            // On Step 2 (Conditions), allow Next only when checked
            SetupWizard.CanGoNext = SetupWizard.CurrentIndex != 1 || ConditionCheck?.IsChecked == true;
        }

        private void SetupWizard_Navigating(object sender, WizardNavigatingEventArgs e)
        {
            // Demo: re-evaluate CanGoNext on each step transition (step-dependent declarative control)
            Dispatcher.BeginInvoke(UpdateWizardCanGoNext);
        }

        private void SetupWizard_Finished(object sender, RoutedEventArgs e)
        {
            WizardInfo.Text = "Finished event raised (the host dialog would close here).";
            SetupWizard.CurrentIndex = 0;
        }

        private void SetupWizard_Cancelled(object sender, RoutedEventArgs e)
        {
            WizardInfo.Text = "Cancelled event raised.";
            SetupWizard.CurrentIndex = 0;
        }
    }
}
