---
title: テーマとアクセントの変更
description: Dark/Light テーマの設定、Windows OS 設定の取得、実行時のアクセントカラー変更方法。
sidebar_position: 1
---

# テーマとアクセントを変更する

## テーマ辞書の登録

`App.xaml` に `WorkbenchTheme` を追加し、初期バリアント（`Dark` または `Light`）を指定します:

```xml
<Application.Resources>
    <wb:WorkbenchTheme Theme="Dark" />
</Application.Resources>
```

## 実行時の Dark / Light 切替

UI スレッドから `ThemeManager.SetTheme` を呼び出します。コントロールスタイルはセマンティックブラシを `DynamicResource` で参照しているため、登録済みの全 `WorkbenchTheme` が即座に切り替わります:

```csharp
using WorkbenchUI.Wpf.Theming;

ThemeManager.SetTheme(WorkbenchThemeVariant.Light);
```

現在のテーマバリアントは `ThemeManager.CurrentTheme` で参照できます。

## 起動時に Windows のテーマ設定を反映する

`ThemeManager.GetSystemTheme()` は Windows の「アプリモード」設定（`AppsUseLightTheme`）を読み取ります:

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    ThemeManager.SetTheme(ThemeManager.GetSystemTheme());
}
```

## アクセントカラーを動的に変更する

`ThemeManager.SetAccent` を呼ぶと、全コントロール・フォーカス枠・チャート・ドッキングテーマのアクセント色が更新されます。`Hover` / `Pressed` / `Muted` の派生色は HSL 明度調整により自動計算されます:

```csharp
using System.Windows.Media;
using WorkbenchUI.Wpf.Theming;

// 緑系のアクセントを適用
ThemeManager.SetAccent(Color.FromRgb(0x2E, 0xA0, 0x43));

// 現在の上書き色を参照（テーマ既定なら null）
Color? activeAccent = ThemeManager.CurrentAccent;

// テーマ既定のアクセント (#007ACC) に戻す
ThemeManager.ResetAccent();
```

## 独自コードでテーマ変更を追従する

カスタム描画など、`DynamicResource` を使えない処理がある場合は `ThemeManager.ThemeChanged` を購読します:

```csharp
Loaded += (_, _) => ThemeManager.ThemeChanged += OnThemeChanged;
Unloaded += (_, _) => ThemeManager.ThemeChanged -= OnThemeChanged;

private void OnThemeChanged(object? sender, EventArgs e)
{
    // Application.Current から Wb.Color.* を再取得して再描画
}
```

*（`WorkbenchPlot`、`WorkbenchDockTheme`、`WorkbenchViewport` はテーマ変更を自動的に追従します。）*

## 独自 XAML でトークンを参照する際の規約

1. `Wb.Brush.*` / `Wb.Color.*` は必ず **`DynamicResource`** で参照してください。
2. `Wb.Spacing.*` / `Wb.FontSize.*` / `Wb.CornerRadius` / `Wb.ControlHeight` は **`StaticResource`** で参照してください。

```xml
<Border Background="{DynamicResource Wb.Brush.Surface.Panel}"
        BorderBrush="{DynamicResource Wb.Brush.Border.Default}"
        BorderThickness="1"
        CornerRadius="{StaticResource Wb.CornerRadius}"
        Padding="{StaticResource Wb.Padding.Panel}">
    <TextBlock Text="Inspector"
               Foreground="{DynamicResource Wb.Brush.Text.Primary}"
               FontSize="{StaticResource Wb.FontSize.M}" />
</Border>
```
