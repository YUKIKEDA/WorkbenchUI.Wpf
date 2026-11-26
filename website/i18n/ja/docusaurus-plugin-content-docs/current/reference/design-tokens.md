---
title: デザイントークン & 名前付きスタイル
description: WorkbenchUI.Wpf のセマンティック Color・Brush・寸法トークンおよびキー付きスタイルのリファレンス。
sidebar_position: 1
---

# デザイントークン & 名前付きスタイル

すべてのトークンキーは `Wb.<Category>.<Role>[.<State>]` の命名規約に従います。

## コア寸法・タイポグラフィトークン (`Tokens.Core.xaml`)

テーマバリアント間で不変のため、`StaticResource` で参照できます。

| キー | 型 | 値 | 用途 |
| --- | --- | --- | --- |
| `Wb.FontFamily.Default` | `FontFamily` | `Segoe UI, Yu Gothic UI` | 基本 UI フォント |
| `Wb.FontFamily.Monospace` | `FontFamily` | `Cascadia Mono, Consolas` | ログコンソール・数値桁揃え |
| `Wb.FontSize.S` | `Double` | `11` | キャプション・軸ラベル |
| `Wb.FontSize.M` | `Double` | `12` | 標準テキスト |
| `Wb.FontSize.L` | `Double` | `14` | セクション見出し・タイトル |
| `Wb.Spacing.XS` | `Double` | `2` | 極小余白 |
| `Wb.Spacing.S` | `Double` | `4` | 4 px 基本グリッド単位 |
| `Wb.Spacing.M` | `Double` | `8` | 標準間隔 |
| `Wb.Spacing.L` | `Double` | `12` | パネル・セクション余白 |
| `Wb.ControlHeight` | `Double` | `24` | 高密度コントロール標準高さ |
| `Wb.IconSize` | `Double` | `16` | `WorkbenchIcon` 既定サイズ |
| `Wb.CornerRadius` | `CornerRadius` | `2` | スクエア基調の角丸 |
| `Wb.Padding.Input` | `Thickness` | `4,2` | 入力欄パディング |
| `Wb.Padding.Panel` | `Thickness` | `8` | パネルパディング |

## セマンティックカラートークン (`Tokens.Dark.xaml` / `Tokens.Light.xaml`)

`Wb.Color.*` と `Wb.Brush.*` は必ず **`DynamicResource`** で参照してください。`Text.Muted` を除くすべての `Wb.Color.*` に対応する `Wb.Brush.*` が定義されています。

| キー | Dark | Light | 役割 |
| --- | --- | --- | --- |
| `Wb.Color.Surface.Window` | `#1E1E1E` | `#EEEEF2` | ウィンドウ・クローム背景 |
| `Wb.Color.Surface.Panel` | `#252526` | `#F5F5F7` | ツールパネル・サイドバー |
| `Wb.Color.Surface.Elevated` | `#2D2D30` | `#FAFAFC` | ツールバー・メニュー・ポップアップ |
| `Wb.Color.Surface.Input` | `#333337` | `#FFFFFF` | テキスト入力・エディタ |
| `Wb.Color.Text.Primary` | `#F1F1F1` | `#1E1E1E` | 主要テキスト |
| `Wb.Color.Text.Muted` | `#C5C5C5` | `#4B4B52` | 抑えたテキスト・チャート軸 |
| `Wb.Color.Text.Secondary` | `#969696` | `#6E6E76` | 補助テキスト |
| `Wb.Color.Text.Disabled` | `#6E6E73` | `#9B9BA3` | 無効状態テキスト |
| `Wb.Color.Text.OnAccent` | `#FFFFFF` | `#FFFFFF` | アクセント背景上のテキスト |
| `Wb.Color.Control.Background` | `#333337` | `#ECECF0` | ボタン等の既定背景 |
| `Wb.Color.Control.Hover` | `#3F3F46` | `#DADAE0` | ホバー時背景 |
| `Wb.Color.Control.Pressed` | `#55555A` | `#C8C8D0` | 押下時背景 |
| `Wb.Color.Border.Default` | `#3F3F46` | `#CCCEDB` | 標準境界線 |
| `Wb.Color.Border.Strong` | `#55555A` | `#A0A2AE` | 強調境界線 |
| `Wb.Color.State.Selected` | `#094771` | `#C7E0F4` | アクティブな選択背景 |
| `Wb.Color.State.SelectedInactive` | `#37373D` | `#E1E1E6` | 非フォーカス時の選択背景 |
| `Wb.Color.Error` | `#F14C4C` | `#C42B1C` | エラー枠・アイコン |
| `Wb.Color.Warning` | `#CCA700` | `#9D5D00` | 警告ステータス |
| `Wb.Color.Success` | `#89D185` | `#107C10` | 成功ステータス |
| `Wb.Color.Info` | `#75BEFF` | `#005FB8` | 情報ステータス |
| `Wb.Color.Danger.Default` | `#C42B1C` | `#C42B1C` | 危険操作ボタン背景 |
| `Wb.Color.Danger.Hover` | `#D13438` | `#A4262C` | 危険操作ホバー |
| `Wb.Color.Danger.Pressed` | `#A4262C` | `#8B1F17` | 危険操作押下 |
| `Wb.Color.Accent.Default` | `#007ACC` | `#007ACC` | 基本アクセント（`ThemeManager.SetAccent` で上書き可） |
| `Wb.Color.Accent.Hover` | `#1C97EA` | `#1C97EA` | アクセントホバー（HSL 自動派生） |
| `Wb.Color.Accent.Pressed` | `#005A9E` | `#005A9E` | アクセント押下（HSL 自動派生） |
| `Wb.Color.Accent.Muted` | `#094771` | `#C7E0F4` | 淡色/暗色アクセント（バリアント別派生） |

追加のブラシ専用トークン:
- `Wb.Brush.Border.Focus`（`Wb.Color.Accent.Default` に連動）
- `Wb.Brush.Surface.Alternate`（Dark: `#08FFFFFF` / Light: `#06000000`）
- `Wb.Brush.State.Hover`（Dark: `#14FFFFFF` / Light: `#0D000000`）
- `Wb.Brush.State.Pressed`（Dark: `#1FFFFFFF` / Light: `#1A000000`）

## キー付きコントロールスタイル

| スタイルキー | 対象型 | 用途 |
| --- | --- | --- |
| `Wb.Button` | `Button` | 標準ワークベンチボタン |
| `Wb.Button.Accent` | `Button` | プライマリ実行ボタン |
| `Wb.Button.Danger` | `Button` | 破壊的操作ボタン |
| `Wb.Button.Subtle` | `Button` | 控えめなフラットボタン |
| `Wb.Button.Icon` | `Button` | アイコンのみツールバーボタン（`ButtonAssist.Icon`） |
| `Wb.DropDownButton.Accent` / `.Subtle` | `DropDownButton` | ドロップダウンボタン派生 |
| `Wb.SplitButton.Accent` / `.Subtle` | `SplitButton` | スプリットボタン派生 |
| `Wb.Ribbon.Button.Large` / `.Large.Accent` / `.Small` | `Button` | リボンコマンドボタン各種 |
| `Wb.Ribbon.SplitButton.Large.Accent` | `SplitButton` | リボン用アクセント付きスプリットボタン |
| `Wb.Ribbon.Toggle.Large` / `.Small` | `ToggleButton` | リボントグルボタン各種 |
| `Wb.DataGrid.DetailsToggle` | `ToggleButton` | DataGrid 行詳細の展開トグル |
