---
title: パッケージ・API 設計
description: ゼロ依存コア、外部ライブラリの分離、スレッドセーフモデル、公開 API の絞り込み方針の解説。
sidebar_position: 2
---

# パッケージ・API 設計

## ゼロ依存コア + 外部連携の隔離

`WorkbenchUI.Wpf`（コア）は **外部 NuGet パッケージ依存ゼロ** で、WPF 標準アセンブリのみに依存します。大きな外部ライブラリ連携は別パッケージに分離しています:

- **`WorkbenchUI.Wpf.Charts`**: `ScottPlot.WPF` (MIT) を隔離
- **`WorkbenchUI.Wpf.Docking`**: `Dirkster.AvalonDock` (**Ms-PL**) を隔離し、MIT コアとライセンス境界を明確に分離
- **`WorkbenchUI.Wpf.Viewport3D`**: `Silk.NET` Direct3D 11 / D3D9 / DXGI バインディング (MIT) を隔離

4 つのアセンブリはすべて `https://schemas.workbenchui.dev/wpf` にマップされているため、利用側の XAML では拡張パッケージを追加しても `xmlns:wb` の宣言 1 行のままで済みます。

## UI から分離された高スループットモデル

計算処理やログ出力など高頻度なデータ更新に耐えるため、データモデルを WPF ビジュアルツリーから分離しています:

- **`LogBuffer` & `LogConsole`**: ワーカースレッドからはロックフリーの `ConcurrentQueue<LogEntry>` に積み、`LogConsole` 側が `DispatcherTimer` でバッチ反映することで、秒間数千行の出力でも UI スレッドをブロックしません。
- **`ConvergenceSeries` & `ConvergenceMonitor`**: 計算スレッドからスレッドセーフに `Append` でき、チャート側がスロットリングされた周期でスナップショットを描画します。
- **`ViewportMesh` & `ViewportVoxelGrid`**: 配列（`Positions`, `ScalarValues`, `Displacements`, `VectorValues`）の更新に「配列インスタンスの差し替え」規約を採用し、ジオメトリ再構築を伴わない高速な GPU 部分バッファ更新を実現しています。

## スリムな公開 API サーフェス

内部の幾何演算・色空間計算・文字列パース等のヘルパー（`ColorMath`, `AccentPalette`, `NumericText`, `FlatTreeSource`, および `ViewportCulling` 等の内部アルゴリズム群）は `internal` としてカプセル化し、`InternalsVisibleTo` を通じて単体テストから検証する構成としています。これにより利用側の IntelliSense を明快に保ち、互換性を壊さず内部最適化を行えます。
