---
title: 設計意思決定 (ADRs)
description: WorkbenchUI.Wpf のアーキテクチャ・命名・パッケージング・ドキュメントに関する意思決定記録。
sidebar_position: 3
---

# アーキテクチャ & 設計意思決定記録 (ADRs)

内部プロトタイプ（`WpfCustomUI`）を整理・洗練し、公開 OSS ライブラリ **WorkbenchUI.Wpf** として再定義した際の設計意思決定をまとめた記録です（英語正本: `docs/design-decisions.md`）。

---

## ADR-01: ポジショニングとモジュラー構成
- **決定**: エンジニアリング／科学技術デスクトップアプリ向けの高密度・無彩色ワークベンチ UI ライブラリとして位置付け、コア `WorkbenchUI.Wpf` は **ゼロ依存** を維持し、`Charts` / `Docking` / `Viewport3D` に外部依存を隔離する。サンプルは `WorkbenchUI.Wpf.Gallery` のみを収録し、業務サンプルアプリ（`CaeStudio` / `CastingStudio`）は除外する。

## ADR-02: ターゲットフレームワーク
- **決定**: **`net10.0-windows`** 単一ターゲットとする（.NET 8 LTS は 2026 年 11 月時点でサポート終了しており、.NET 10 が現行 LTS であるため）。

## ADR-03: 命名・リブランド・XAML 名前空間
- **パッケージ名**: `WorkbenchUI.Wpf`, `WorkbenchUI.Wpf.Charts`, `WorkbenchUI.Wpf.Docking`, `WorkbenchUI.Wpf.Viewport3D`
- **接頭辞付き型名**: 旧 `Wcu*` 14 種は略語を避け一貫性を保つため **`Workbench*`** へ統一（`WorkbenchTheme`, `WorkbenchWindow`, `WorkbenchRibbon`, `WorkbenchIcon`, `WorkbenchPlot`, `WorkbenchDockTheme`, `WorkbenchViewport` 等）。接頭辞のないドメインコントロール（`NumericBox`, `ModelTree`, `PropertyGrid` 等）はそのまま維持。
- **リソースキー**: 旧 `Wcu.*` → **`Wb.*`**（XAML での簡潔さと衝突回避を両立）。
- **XAML URI**: **`https://schemas.workbenchui.dev/wpf`**（推奨プレフィックス `wb`）。

## ADR-04: テーマ基盤アーキテクチャ
- **決定**: WPF 標準 Fluent `ThemeMode` には依存せず、2 層セマンティックトークン（`Wb.Color.*` → `Wb.Brush.*`）と `DynamicResource` 規約による自作 Dark / Light テーマを採用。`ThemeManager` に `CurrentTheme` / `CurrentAccent` を追加。

## ADR-05: 公開 API サーフェスと可視性ポリシー
- **決定**: `ColorMath`, `AccentPalette`, `NumericText`, `FlatTreeSource`, および `Viewport3D` の内部アルゴリズム群（`ViewportCulling`, `ViewportDeformation`, `ViewportGeometry`, `ViewportSection`, `ViewportVoxelLod`, `ViewportVoxelPicking`, `ViewportVoxelSlice`, `ViewportVoxelSurface`）を **`internal`** 化し、`InternalsVisibleTo` でテストする。

## ADR-06: ライセンスとサードパーティ注記
- **決定**: オリジナルコードは **MIT ライセンス**。`WorkbenchUI.Wpf.Docking` が依存する `Dirkster.AvalonDock` は **Ms-PL** のため、同パッケージの README およびドキュメントで明示する。

## ADR-07: リポジトリ構成と OSS 基盤
- **決定**: クリーンな履歴で移行し、Central Package Management (`Directory.Packages.props`)、.NET 10 SDK 組込 SourceLink、`.snupkg`、英語 XML ドキュメント（`GenerateDocumentationFile=true`）、全 4 パッケージの単体テスト、GitHub Actions CI を整備する。

## ADR-08: ユーザードキュメント (Diátaxis + Docusaurus 3 日英対応)
- **決定**: **Diátaxis** フレームワーク（Tutorials / How-to Guides / Reference / Explanation）に沿って日英両対応のドキュメントを構築し、`website/` の **Docusaurus 3** サイトおよび `.github/workflows/docs.yml` による GitHub Pages 自動公開を整備する。
