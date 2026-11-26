---
title: Build Dynamic Property Grids
description: How to compose explicit PropertyItem collections, enable filtering, and add custom property editors.
sidebar_position: 3
---

# How to Build Dynamic Property Grids

`PropertyGrid` uses an **explicit `PropertyItem` collection** rather than reflection, making it natural to show/hide or swap parameters dynamically when a user switches boundary condition types, material models, or solver options.

## Composing Built-in `PropertyItem` Types

| Item Class | Editor Control | Key Properties |
| --- | --- | --- |
| `TextPropertyItem` | `TextBox` | `Value` (`string?`) |
| `BoolPropertyItem` | `CheckBox` | `Value` (`bool`) |
| `NumericPropertyItem` | `NumericBox` | `Value` (`double?`), `Minimum`, `Maximum`, `Increment`, `Unit`, `UnitProvider`, `Format` |
| `ChoicePropertyItem` | `ComboBox` | `Choices` (`IEnumerable?`), `Value` (`object?`) |
| `PathPropertyItem` | `PathBox` | `Value` (`string?`), `Mode` (`PathBoxMode`), `Filter` |
| `ColorPropertyItem` | `ColorPicker` | `Value` (`Color`), `IsAlphaEnabled` |
| `Vector3PropertyItem` | `Vector3Box` | `X`, `Y`, `Z` (`double?`), `Minimum`, `Maximum`, `Unit`, `UnitProvider` |

All items inherit from `PropertyItem`, which provides `Name`, `Category`, `Description` (shown as a row hover tooltip), and `IsReadOnly`.

```csharp
var items = new ObservableCollection<PropertyItem>
{
    new TextPropertyItem
    {
        Category = "Material",
        Name = "Name",
        Value = "Structural Steel",
        Description = "Display label for the material card.",
    },
    new NumericPropertyItem
    {
        Category = "Material",
        Name = "Young's Modulus",
        Value = 2.1e11,
        Minimum = 1.0,
        Unit = "Pa",
        Format = "E3",
    },
    new ChoicePropertyItem
    {
        Category = "Solver",
        Name = "Formulation",
        Choices = new[] { "Linear", "Nonlinear" },
        Value = "Linear",
    },
    new ColorPropertyItem
    {
        Category = "Display",
        Name = "Part Color",
        Value = Color.FromRgb(0x4E, 0xC9, 0xB0),
    },
};

MyPropertyGrid.ItemsSource = items;
```

## Configuring the Search Filter

`PropertyGrid` includes a built-in `SearchBox` that filters rows case-insensitively by `PropertyItem.Name`:

```xml
<wb:PropertyGrid ItemsSource="{Binding Properties}"
                 FilterText="{Binding PropertyFilter}"
                 ShowFilterBox="True"
                 FilterPlaceholder="Search properties..." />
```

## Adding a Custom Property Editor

1. Subclass `PropertyItem` with your typed `Value` property (using `SetField` for change notification):

```csharp
public sealed class RangePropertyItem : PropertyItem
{
    private double _lower;
    private double _upper = 1.0;

    public double Lower { get => _lower; set => SetField(ref _lower, value); }
    public double Upper { get => _upper; set => SetField(ref _upper, value); }
}
```

2. Add an implicit `DataTemplate` for your type in `Window.Resources` or `App.Resources`:

```xml
<DataTemplate DataType="{x:Type local:RangePropertyItem}">
    <wb:RangeSlider LowerValue="{Binding Lower, Mode=TwoWay}"
                    UpperValue="{Binding Upper, Mode=TwoWay}"
                    Minimum="0" Maximum="100" />
</DataTemplate>
```
