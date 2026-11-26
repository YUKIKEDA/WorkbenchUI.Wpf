---
title: はじめに
description: WorkbenchUI.Wpf の概要、前提環境、パッケージ構成の案内。
sidebar_position: 1
---

# WorkbenchUI.Wpf へようこそ

**WorkbenchUI.Wpf** は、**.NET 10 (`net10.0-windows`)** をターゲットとした、エンジニアリング・科学技術デスクトップアプリケーション向けの高密度・無彩色 WPF UI ライブラリです。

ニュートラルグレーの配色、24 px のコンパクトなコントロール寸法、および動的に切り替え可能な Dark / Light テーマにより、シミュレーションのコンター色やグラフ、3D ビューポートの視認性を妨げない洗練されたワークベンチ UI を構築できます。

## 前提環境

- **OS**: Windows 10 / 11（WPF および 3D ビューポートの Direct3D 11 のため）
- **SDK**: [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0-windows`)

## パッケージ構成

| パッケージ | 役割 | 外部依存 |
| --- | --- | --- |
| `WorkbenchUI.Wpf` | テーマ基盤 (`Dark`/`Light`)、セマンティックトークン、68 種のベクタアイコン、基本/ワークベンチコントロール | なし（WPF 標準のみ） |
| `WorkbenchUI.Wpf.Charts` | ScottPlot 5 テーマ同期と技術計算向け複合チャート | `ScottPlot.WPF` (MIT) |
| `WorkbenchUI.Wpf.Docking` | AvalonDock トークン連動テーマと XML レイアウト永続化 | `Dirkster.AvalonDock` (Ms-PL) |
| `WorkbenchUI.Wpf.Viewport3D` | Direct3D 11 ベースの 3D ビューポート（メッシュ、疎ボクセル、コンター、変形、断面、プローブ） | `Silk.NET` Direct3D / DXGI (MIT) |

4 つのパッケージはすべて単一の XAML 名前空間 URI に集約されています:

```xml
xmlns:wb="https://schemas.workbenchui.dev/wpf"
```

## ドキュメント

- **チュートリアル** — [最初の Workbench アプリを作る](tutorials/first-workbench-app.md) では、空の WPF プロジェクトからテーマ付きワークベンチ画面をステップバイステップで構築します。
- **ハウツーガイド** — [テーマとアクセントの変更](how-to/theming-and-accent.md)、[単位換算と数値入力](how-to/units-and-numeric-input.md)、[PropertyGrid の活用](how-to/property-grid.md)、[ModelTree の実装](how-to/model-tree.md)、[Docking と Charts の統合](how-to/docking-and-charts.md)、[3D ビューポートの描画](how-to/viewport-3d.md)。
- **リファレンス** — [デザイントークン一覧](reference/design-tokens.md)、[コントロールカタログ](reference/controls-catalog.md)、[内蔵アイコン一覧](reference/icons.md)、[拡張パッケージ API](reference/extension-packages.md)。
- **解説** — [テーマアーキテクチャ](explanation/theme-architecture.md)、[パッケージ・API 設計](explanation/package-architecture.md)。
