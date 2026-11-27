# WorkbenchUI.Wpf

.NET 10 / WPF 向けの、エンジニアリング・科学技術デスクトップアプリケーション向け **高密度・無彩色 UI ライブラリ** です。

> **[English README](README.md)** | **[設計意思決定ドキュメント (Design Decisions)](docs/design-decisions.md)**

---

## 特長

- **ゼロ依存コア (`WorkbenchUI.Wpf`)**: WPF 標準アセンブリのみで構成され、外部 NuGet 依存を持ちません。
- **高密度・低彩度デザイン**: コントロール高さ 24 px・4 px スペーシンググリッド・角丸 2 px を基調とし、コンター色や 3D ビューポートの視認性を邪魔しない無彩色の Dark / Light テーマを提供します。
- **実行時テーマ・アクセント切替**: `ThemeManager.SetTheme` / `SetAccent` により、テーマバリアントおよびアクセントカラー（Hover / Pressed / Muted を HSL で自動派生）を動的に切り替えられます。
- **内蔵ベクタアイコン**: `WorkbenchIcon` / `WorkbenchIcons`（`Wb.Icon.*` リソースキー）による 16×16 ラインアイコンを標準搭載。
- **ワークベンチ・技術計算向けコントロール群**: `NumericBox`（`double?`・指数表記・`IUnitProvider`）、`PropertyGrid`、`ModelTree`（仮想化・複数選択・三状態可視性伝播）、`ColorMapLegend` / `ColorScaleEditor`、`LogConsole` / `LogBuffer`、`WorkbenchWindow`、`WorkbenchRibbon`、`DataGrid` スタイルほか多数。
- **外部ライブラリ連携の分離パッケージ**:
  - `WorkbenchUI.Wpf.Charts` (`ScottPlot.WPF` 連携)
  - `WorkbenchUI.Wpf.Docking` (`Dirkster.AvalonDock` 連携)
  - `WorkbenchUI.Wpf.Viewport3D` (`Silk.NET` Direct3D 11 連携)

## クイックスタート

1. `WorkbenchUI.Wpf`（および必要に応じて `Charts` / `Docking` / `Viewport3D`）を参照します。
2. `App.xaml` にテーマを 1 行追加します。

```xml
<Application x:Class="MyApp.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:wb="https://schemas.workbenchui.dev/wpf">
    <Application.Resources>
        <wb:WorkbenchTheme Theme="Dark" />
    </Application.Resources>
</Application>
```

3. ビルド・テスト・ギャラリー起動:

```bash
dotnet build WorkbenchUI.Wpf.slnx -c Release
dotnet test WorkbenchUI.Wpf.slnx -c Release
dotnet run --project samples/WorkbenchUI.Wpf.Gallery
```

## ドキュメント

- **ユーザードキュメントサイト (日本語 / English)**: [https://yukikeda.github.io/WorkbenchUI.Wpf/ja/](https://yukikeda.github.io/WorkbenchUI.Wpf/ja/)
- [English README](README.md)
- [設計意思決定ドキュメント (ADRs)](docs/design-decisions.md)

## ライセンス

[MIT License](LICENSE)（`WorkbenchUI.Wpf.Docking` が依存する `Dirkster.AvalonDock` は Ms-PL ライセンスです）。
