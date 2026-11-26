---
title: PropertyGrid の活用
description: 明示的 PropertyItem コレクションの構築、フィルタ機能、カスタムエディタの追加方法。
sidebar_position: 3
---

# 動的な PropertyGrid を構築する

`PropertyGrid` はリフレクションではなく **明示的な `PropertyItem` コレクション** を受け取る設計です。境界条件や材料モデルの選択に応じてパラメータ項目を動的に切り替えるワークベンチ UI に適しています。

## 組み込み `PropertyItem` 型一覧

| 項目クラス | エディタ | 主なプロパティ |
| --- | --- | --- |
| `TextPropertyItem` | `TextBox` | `Value` (`string?`) |
| `BoolPropertyItem` | `CheckBox` | `Value` (`bool`) |
| `NumericPropertyItem` | `NumericBox` | `Value` (`double?`), `Minimum`, `Maximum`, `Increment`, `Unit`, `UnitProvider`, `Format` |
| `ChoicePropertyItem` | `ComboBox` | `Choices` (`IEnumerable?`), `Value` (`object?`) |
| `PathPropertyItem` | `PathBox` | `Value` (`string?`), `Mode` (`PathBoxMode`), `Filter` |
| `ColorPropertyItem` | `ColorPicker` | `Value` (`Color`), `IsAlphaEnabled` |
| `Vector3PropertyItem` | `Vector3Box` | `X`, `Y`, `Z` (`double?`), `Minimum`, `Maximum`, `Unit`, `UnitProvider` |

すべての項目は `PropertyItem` を継承し、`Name`、`Category`、`Description`（行ホバー時のツールチップ表示）、`IsReadOnly` を持ちます。

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

## 検索フィルタの設定

`PropertyGrid` には `SearchBox` が内蔵されており、`PropertyItem.Name` に対する大文字小文字を区別しない部分一致フィルタを提供します:

```xml
<wb:PropertyGrid ItemsSource="{Binding Properties}"
                 FilterText="{Binding PropertyFilter}"
                 ShowFilterBox="True"
                 FilterPlaceholder="Search properties..." />
```

## カスタムエディタの追加

1. `PropertyItem` を継承し、型付きプロパティを追加します（通知には `SetField` を使用）:

```csharp
public sealed class RangePropertyItem : PropertyItem
{
    private double _lower;
    private double _upper = 1.0;

    public double Lower { get => _lower; set => SetField(ref _lower, value); }
    public double Upper { get => _upper; set => SetField(ref _upper, value); }
}
```

2. `Window.Resources` または `App.Resources` に暗黙 `DataTemplate` を登録します:

```xml
<DataTemplate DataType="{x:Type local:RangePropertyItem}">
    <wb:RangeSlider LowerValue="{Binding Lower, Mode=TwoWay}"
                    UpperValue="{Binding Upper, Mode=TwoWay}"
                    Minimum="0" Maximum="100" />
</DataTemplate>
```
