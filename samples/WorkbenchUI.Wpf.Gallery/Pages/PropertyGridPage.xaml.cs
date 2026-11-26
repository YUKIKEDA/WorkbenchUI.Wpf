using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Gallery.Pages
{
    public partial class PropertyGridPage : UserControl
    {
        private readonly ObservableCollection<PropertyItem> _items = [];
        private readonly ChoicePropertyItem _analysisType;
        private readonly List<PropertyItem> _dynamicItems = [];
        private readonly NumericPropertyItem _thickness;

        public PropertyGridPage()
        {
            InitializeComponent();

            _items.Add(new TextPropertyItem
            {
                Name = "Name",
                Category = "General",
                Description = "Display name of the part",
                Value = "Bracket-01",
            });
            _items.Add(new TextPropertyItem
            {
                Name = "ID",
                Category = "General",
                Description = "Internal identifier (read-only)",
                Value = "PT-000123",
                IsReadOnly = true,
            });
            _items.Add(new BoolPropertyItem
            {
                Name = "Visible",
                Category = "General",
                Description = "Visibility in the 3D view",
                Value = true,
            });

            _items.Add(new ChoicePropertyItem
            {
                Name = "Material",
                Category = "Material",
                Description = "Select from the material database",
                Choices = new[] { "Steel SS400", "Aluminum A5052", "Titanium Ti-6Al-4V" },
                Value = "Steel SS400",
            });
            _items.Add(new NumericPropertyItem
            {
                Name = "Young's Modulus",
                Category = "Material",
                Description = "Elastic modulus",
                Value = 205,
                Minimum = 0,
                Unit = "GPa",
            });
            _items.Add(new NumericPropertyItem
            {
                Name = "Poisson's Ratio",
                Category = "Material",
                Description = "Valid range: 0–0.5",
                Value = 0.3,
                Minimum = 0,
                Maximum = 0.5,
                Increment = 0.05,
            });
            _thickness = new NumericPropertyItem
            {
                Name = "Thickness",
                Category = "Material",
                Description = "Displays in mm while storing meters (unit-suffixed input like \"500 mm\" is supported)",
                Value = 0.0032,
                Minimum = 0,
                Maximum = 0.1,
                UnitProvider = new LengthUnitProvider(),
            };
            _items.Add(_thickness);

            // Path / Color / Vector3 editors
            _items.Add(new PathPropertyItem
            {
                Name = "Mesh File",
                Category = "Input Data",
                Description = "Click Browse to open a file dialog",
                Mode = PathBoxMode.OpenFile,
                Filter = "Mesh files (*.msh;*.inp)|*.msh;*.inp|All files (*.*)|*.*",
            });
            _items.Add(new ColorPropertyItem
            {
                Name = "Part Color",
                Category = "Display",
                Description = "Display color in the 3D view (alpha = opacity)",
                Value = System.Windows.Media.Color.FromArgb(0xCC, 0x00, 0x78, 0xD7),
            });
            _items.Add(new Vector3PropertyItem
            {
                Name = "Load Direction",
                Category = "Analysis Settings",
                Description = "Load direction vector",
                X = 0,
                Y = 0,
                Z = -1,
                Format = "G4",
            });

            _analysisType = new ChoicePropertyItem
            {
                Name = "Analysis Type",
                Category = "Analysis Settings",
                Description = "Changing this dynamically updates the parameters below",
                Choices = new[] { "Static", "Thermal Conduction" },
                Value = "Static",
            };
            _analysisType.PropertyChanged += OnAnalysisTypeChanged;
            _items.Add(_analysisType);

            RebuildDynamicItems();

            Grid1.ItemsSource = _items;

            _thickness.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(NumericPropertyItem.Value))
                {
                    UpdateMonitor();
                }
            };
            UpdateMonitor();
        }

        private void OnAnalysisTypeChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ChoicePropertyItem.Value))
            {
                RebuildDynamicItems();
            }
        }

        private void RebuildDynamicItems()
        {
            foreach (var item in _dynamicItems)
            {
                _items.Remove(item);
            }

            _dynamicItems.Clear();

            if (Equals(_analysisType.Value, "Static"))
            {
                _dynamicItems.Add(new NumericPropertyItem
                {
                    Name = "Load",
                    Category = "Analysis Settings",
                    Description = "Concentrated force applied at the tip",
                    Value = 1000,
                    Unit = "N",
                });
                _dynamicItems.Add(new NumericPropertyItem
                {
                    Name = "Convergence Tolerance",
                    Category = "Analysis Settings",
                    Description = "Residual norm threshold",
                    Value = 1e-6,
                    Format = "G3",
                });
            }
            else
            {
                _dynamicItems.Add(new NumericPropertyItem
                {
                    Name = "Ambient Temperature",
                    Category = "Analysis Settings",
                    Description = "Surrounding environment temperature",
                    Value = 25,
                    Unit = "℃",
                });
                _dynamicItems.Add(new NumericPropertyItem
                {
                    Name = "Heat Transfer Coefficient",
                    Category = "Analysis Settings",
                    Description = "Surface convection heat transfer coefficient",
                    Value = 10,
                    Unit = "W/m²K",
                });
            }

            foreach (var item in _dynamicItems)
            {
                _items.Add(item);
            }
        }

        private void UpdateMonitor() =>
            ValueMonitor.Text = $"Thickness base value = {_thickness.Value} m";
    }
}
