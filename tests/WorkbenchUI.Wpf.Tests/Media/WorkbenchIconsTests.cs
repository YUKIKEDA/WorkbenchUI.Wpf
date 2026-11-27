using System.Windows.Media;
using WorkbenchUI.Wpf.Controls;

namespace WorkbenchUI.Wpf.Tests.Media;

/// <summary>Verifies consistency of the icon dictionary.</summary>
public class WorkbenchIconsTests
{
    [Fact]
    public void Names_ContainsAtLeast60Icons()
    {
        Assert.True(WorkbenchIcons.Names.Count >= 60,
            $"Icon count is lower than expected: {WorkbenchIcons.Names.Count}");
    }

    [Fact]
    public void AllIcons_ParseToNonEmptyFrozenGeometry()
    {
        foreach (var name in WorkbenchIcons.Names)
        {
            var geometry = WorkbenchIcons.Get(name);
            Assert.True(geometry.IsFrozen, $"{name}: not frozen");
            Assert.False(geometry.Bounds.IsEmpty, $"{name}: empty Geometry");
        }
    }

    [Fact]
    public void AllIcons_FitWithin16x16Canvas()
    {
        foreach (var name in WorkbenchIcons.Names)
        {
            var bounds = WorkbenchIcons.Get(name).Bounds;
            Assert.True(bounds.Left >= 0 && bounds.Top >= 0
                && bounds.Right <= WorkbenchIcons.CanvasSize && bounds.Bottom <= WorkbenchIcons.CanvasSize,
                $"{name}: extends outside the 16x16 canvas ({bounds})");
        }
    }

    [Fact]
    public void AllIcons_HaveReasonableSize()
    {
        // Detect overly small icons (data errors); pure lines/dividers still span >= 8 in one axis
        foreach (var name in WorkbenchIcons.Names)
        {
            var bounds = WorkbenchIcons.Get(name).Bounds;
            var maxExtent = Math.Max(bounds.Width, bounds.Height);
            Assert.True(maxExtent >= 8, $"{name}: too small ({bounds})");
        }
    }

    [Fact]
    public void Get_SameInstanceIsCached()
    {
        var first = WorkbenchIcons.Get("Save");
        var second = WorkbenchIcons.Get("Save");
        Assert.Same(first, second);
        Assert.Same(first, WorkbenchIcons.Save);
    }

    [Fact]
    public void TryGet_UnknownName_ReturnsFalse()
    {
        Assert.False(WorkbenchIcons.TryGet("NoSuchIcon", out _));
        Assert.Throws<KeyNotFoundException>(() => WorkbenchIcons.Get("NoSuchIcon"));
    }

    [Fact]
    public void CreateResourceDictionary_ContainsAllIconsWithPrefixedKeys()
    {
        var dictionary = WorkbenchIcons.CreateResourceDictionary();
        var secondDictionary = WorkbenchIcons.CreateResourceDictionary();
        Assert.NotSame(dictionary, secondDictionary);
        Assert.Equal(WorkbenchIcons.Names.Count, dictionary.Count);
        foreach (var name in WorkbenchIcons.Names)
        {
            Assert.True(dictionary.Contains($"Wb.Icon.{name}"), $"Wb.Icon.{name} was not found");
            Assert.IsAssignableFrom<Geometry>(dictionary[$"Wb.Icon.{name}"]);
            Assert.Same(dictionary[$"Wb.Icon.{name}"], secondDictionary[$"Wb.Icon.{name}"]);
        }
    }
}
