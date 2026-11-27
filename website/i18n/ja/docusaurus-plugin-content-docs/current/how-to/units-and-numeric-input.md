---
title: 単位換算と数値入力
description: NumericBox、Vector3Box、MatrixBox の使い方と IUnitProvider による単位換算の実装方法。
sidebar_position: 2
---

# 単位換算と数値入力を扱う

`NumericBox` は、**内部モデル値**（SI 単位系などの基準単位）と、ユーザーに表示・編集させる **表示単位**（例: `mm` や `MPa`）を分離できます。

## `NumericBox` の基本的な使い方

`Value`（`double?`）をバインドします（`null` は未入力／空欄を表現）。確定は `Enter` またはフォーカス喪失、キャンセルは `Esc` で行われ、指数表記（`1e-3`、`2.5E+6`）にも対応しています:

```xml
<wb:NumericBox Value="{Binding Thickness}"
               Minimum="0.0001"
               Maximum="10.0"
               Increment="0.1"
               Unit="m"
               Format="G5"
               Width="150" />
```

## `IUnitProvider` を実装する

ViewModel 側の基準単位（例: `m`）を保ったまま、`mm` などで表示・単位付き入力を受け付けるには `IUnitProvider` を実装します:

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
                return false; // 赤枠とツールチップの検証エラーとして表示される
        }
    }
}
```

`NumericBox.UnitProvider` に設定します:

```xml
<wb:NumericBox Value="{Binding LengthMeters}"
               UnitProvider="{Binding LengthUnitProvider}"
               Minimum="0.0"
               Increment="1.0" />
```

- `LengthMeters = 0.5` のとき、表示は `500`（単位ラベル `mm`）になります。
- ユーザーが `25 cm` と入力して `Enter` を押すと、`TryConvertFrom` によって `0.25` へ換算され、表示は `250 mm` に正規化されます。
- 未対応の単位（例: `10 ft`）が入力された場合は検証エラーとなり、モデルは書き換わりません。

## 複数要素の入力: `Vector3Box` と `MatrixBox`

- **`Vector3Box`**: `X`, `Y`, `Z`（`double?`）、`Minimum`, `Maximum`, `Increment`, `Unit`, `UnitProvider`, `Format` を備えた 3 軸入力コントロール:

```xml
<wb:Vector3Box X="{Binding ForceX}"
               Y="{Binding ForceY}"
               Z="{Binding ForceZ}"
               Unit="N"
               Format="G4" />
```

- **`MatrixBox`**: 2 次元数値行列用のコンパクトなグリッドエディタ（`Rows`, `Columns`, `Values`, `IsSymmetric`, `IsReadOnly`, `UnitProvider`, `Format`）。
