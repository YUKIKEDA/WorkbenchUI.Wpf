---
title: ModelTree の実装
description: ITreeNode 階層のバインド、複数選択、三状態可視性伝播、インライン名前変更の使い方。
sidebar_position: 4
---

# ModelTree を使う

`ModelTree` はツリー階層をフラットなリストへ射影し、内部の仮想化 `ListBox` で描画します。大規模モデルでの高速スクロール、`Ctrl` / `Shift` による複数選択、三状態の表示/非表示トグル、インライン名前変更に対応しています。

## `TreeNode` を使ったシンプルな構成

`ITreeNode` と `IRenamableNode` を実装済みの `TreeNode` をそのまま利用できます:

```csharp
var root = new TreeNode
{
    Name = "Model",
    IsExpanded = true,
    Icon = WorkbenchIcons.Layers,
};
root.Children.Add(new TreeNode { Name = "Part A", Icon = WorkbenchIcons.Cube });
root.Children.Add(new TreeNode { Name = "Part B", Icon = WorkbenchIcons.Cube });

MyModelTree.ItemsSource = new ObservableCollection<ITreeNode> { root };
```

XAML:

```xml
<wb:ModelTree x:Name="MyModelTree"
              SelectionMode="Extended"
              ShowVisibilityToggles="True"
              SelectionChanged="OnModelTreeSelectionChanged" />
```

## 既存 ViewModel に `ITreeNode` を実装する

既存の ViewModel 階層を接続する場合は、`ITreeNode`（名前変更も使うなら `IRenamableNode`）を実装します:

- 子の増減へ追従させるため、`Children` からは同じ `ObservableCollection<ITreeNode>` インスタンスを返してください。
- `Name`、`IsExpanded`、`IsVisible`、`IsSelected` に対して `PropertyChanged` を通知してください。

## 三状態の可視性伝播

行の目アイコンをクリックする（または `ToggleVisibility` / `SetVisibility` を呼ぶ）と、次のように伝播します:

- 親ノードを切り替えると、すべての子孫ノードに `true` / `false` が反映されます。
- 子ノードに混在状態が生じると、親ノードの `IsVisible` は自動的に `null`（混在）になります。

## 複数選択とインライン名前変更

```csharp
// 選択中ノードの取得
IReadOnlyList<ITreeNode> selected = MyModelTree.GetSelectedNodes();

// プログラムから名前変更モードを開始（IRenamableNode 実装が必要）
if (selected.FirstOrDefault() is { } first)
{
    MyModelTree.BeginRename(first);
}
```
