---
title: Work with Units & Numeric Inputs
description: How to use NumericBox, Vector3Box, MatrixBox, and implement IUnitProvider for unit conversion.
sidebar_position: 2
---

# How to Work with Units & Numeric Inputs

`NumericBox` separates the **internal model value** (stored in base units, such as SI meters or Pascals) from the **display value** shown to the user (such as `mm` or `MPa`).

## Basic `NumericBox` Usage

Bind `Value` (`double?`), where `null` represents an unset/blank state. Input commits on `Enter` or focus loss and reverts on `Esc`, and scientific notation (`1e-3`, `2.5E+6`) is supported out of the box:

```xml
<wb:NumericBox Value="{Binding Thickness}"
               Minimum="0.0001"
               Maximum="10.0"
               Increment="0.1"
               Unit="m"
               Format="G5"
               Width="150" />
```

## Implementing `IUnitProvider`

To display and edit in another unit while keeping your ViewModel in SI base units, implement `IUnitProvider`:

```csharp
using WorkbenchUI.Wpf.Controls;

public sealed class LengthMillimeterProvider : IUnitProvider
{
    public string DisplayUnit => "mm";

    public double ToDisplay(double baseValue) => baseValue * 1000.0;

    public double FromDisplay(double displayValue) => displayValue / 1000.0;

    public bool TryConvertFrom(double value, string unitSymbol, out double baseValue)
    {
        switch (unitSymbol.Trim().ToLowerInvariant())
        {
            case "m":
                baseValue = value;
                return true;
            case "cm":
                baseValue = value * 0.01;
                return true;
            case "mm":
                baseValue = value * 0.001;
                return true;
            default:
                baseValue = 0;
                return false; // Shows validation error border & tooltip
        }
    }
}
```

Assign the provider to `NumericBox.UnitProvider`:

```xml
<wb:NumericBox Value="{Binding LengthMeters}"
               UnitProvider="{Binding LengthUnitProvider}"
               Minimum="0.0"
               Increment="1.0" />
```

- When `LengthMeters = 0.5`, the box displays `500` with unit label `mm`.
- If the user types `25 cm` and presses `Enter`, `TryConvertFrom` converts it to `0.25` in `LengthMeters`, and the display updates to `250 mm`.
- If the user enters an unknown suffix like `10 ft`, `NumericBox` displays a red border validation error without modifying `LengthMeters`.

## Multi-Component Inputs: `Vector3Box` and `MatrixBox`

- **`Vector3Box`** exposes `X`, `Y`, `Z` (`double?`), `Minimum`, `Maximum`, `Increment`, `Unit`, `UnitProvider`, and `Format`:

```xml
<wb:Vector3Box X="{Binding ForceX}"
               Y="{Binding ForceY}"
               Z="{Binding ForceZ}"
               Unit="N"
               Format="G4" />
```

- **`MatrixBox`** provides a compact grid editor for 2D numeric matrices (`Rows`, `Columns`, `Values`, `IsSymmetric`, `IsReadOnly`, `UnitProvider`, `Format`).
