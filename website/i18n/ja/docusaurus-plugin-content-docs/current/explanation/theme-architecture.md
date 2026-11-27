---
title: テーマアーキテクチャ
description: WPF 標準 Fluent ThemeMode を使わず、高密度・無彩色な独自テーマ基盤を採用した設計上の理由。
sidebar_position: 1
---

# テーマアーキテクチャ

## なぜ WPF 標準の Fluent `ThemeMode` を使わないのか？

WPF には Fluent スタイル (`ThemeMode`) が組み込まれていますが、エンジニアリング向けワークベンチには次の課題があります:

1. **情報密度の違い**: コンシューマー向け Fluent テーマは大きな余白と高いコントロール高さ（32〜40 px）を前提としています。パラメータインスペクタ、リボン、ツールバーを大量に並べるエンジニアリング画面では、**24 px の `Wb.ControlHeight`**、**4 px スペーシンググリッド**、**2 px 角丸** の高密度基準が不可欠です。
2. **無彩色（低彩度）の徹底**: ポスト処理や可視化アプリでは、ユーザーがコンター色やグラフ色から物理量を読み取ります。Mica/Acrylic や色付きクロームは色覚の妨げになるため、構造面はすべて無彩色のニュートラルグレーで統一し、色は単一アクセントとステータス表示のみに限定しています。
3. **長期安定性**: .NET バージョンアップで変わりうる標準テンプレートの内部構造に依存せず、すべて自作テンプレートで完結させています。

## 2 層セマンティックトークン構造

`Tokens.Dark.xaml` と `Tokens.Light.xaml` は次の 2 層で構成されています:

1. **第 1 層 — セマンティック Color (`Wb.Color.*`)**: 役割ベースの `Color` プリミティブ。WPF の `Brush` を直接使えない消費者（AvalonDock テーマや ScottPlot ラスタ描画）が参照します。
2. **第 2 層 — セマンティック Brush (`Wb.Brush.*`)**: 第 1 層を参照する `SolidColorBrush`。コントロールスタイルはこちらを参照します。

`Gray800` のようなリテラル色名のパレットは廃止しました（ライトテーマで「Gray900 = ほぼ白」という矛盾が生じるため）。両テーマファイルは完全に同一のキー構造を維持しています。

## `DynamicResource` と `StaticResource` の使い分け

- **色・ブラシ (`Wb.Color.*`, `Wb.Brush.*`)**: 実行時の `ThemeManager.SetTheme` や `ThemeManager.SetAccent` に追従させるため、常に **`DynamicResource`** で参照します。
- **寸法・間隔 (`Wb.ControlHeight`, `Wb.Spacing.*`, `Wb.FontSize.*`, `Wb.CornerRadius`)**: テーマ切替で変化しないため **`StaticResource`** で参照します。

## ルックレス設計とローカライズ分離

全カスタムコントロールは `Control` 等を継承し、`Themes/Generic.xaml` にデフォルトテンプレートを持つルックレス構造（`UserControl` 不使用）です。またライブラリに `.resx` は持たせず、表示文字列の既定値は英語とし、依存関係プロパティ（例: `PropertyGrid.FilterPlaceholder`、`WorkbenchRibbon.ApplicationLabel`）からアプリ側で差し替えられる設計にしています。
