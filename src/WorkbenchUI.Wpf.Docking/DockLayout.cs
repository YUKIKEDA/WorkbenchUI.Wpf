using System.IO;
using System.Xml;
using AvalonDock;
using AvalonDock.Layout.Serialization;

namespace WorkbenchUI.Wpf.Docking;

/// <summary>
/// Helpers for saving and restoring AvalonDock layouts.
/// A thin wrapper around AvalonDock's <see cref="XmlLayoutSerializer"/> that delegates
/// content reattachment during restoration to a <c>ContentId</c>-based resolver.
/// <list type="bullet">
/// <item>Any <c>ContentId</c> for which the resolver returns <see langword="null"/> is discarded from the layout
/// (preventing obsolete tool windows from being restored as leftover panes).</item>
/// <item>New tool windows added after a layout was saved will not exist in the saved XML,
/// so ensure they are represented in the default layout definition.</item>
/// </list>
/// </summary>
public static class DockLayout
{
    /// <summary>Saves the current layout to a file, creating parent directories if needed.</summary>
    /// <param name="manager">The target <see cref="DockingManager"/>.</param>
    /// <param name="path">Destination file path for the layout XML.</param>
    public static void Save(DockingManager manager, string path)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var writer = new StreamWriter(path);
        new XmlLayoutSerializer(manager).Serialize(writer);
    }

    /// <summary>Serializes the current layout as an XML string (for embedding in application settings).</summary>
    /// <param name="manager">The target <see cref="DockingManager"/>.</param>
    /// <returns>The serialized layout XML string.</returns>
    public static string SaveToString(DockingManager manager)
    {
        ArgumentNullException.ThrowIfNull(manager);

        using var writer = new StringWriter();
        new XmlLayoutSerializer(manager).Serialize(writer);
        return writer.ToString();
    }

    /// <summary>
    /// Restores a layout from a file.
    /// </summary>
    /// <param name="manager">The target <see cref="DockingManager"/>.</param>
    /// <param name="path">Path to the layout XML file. If the file does not exist, nothing happens and <see langword="false"/> is returned.</param>
    /// <param name="resolveContent">
    /// Resolver that maps a <c>ContentId</c> to pane content (such as a control). Returning <see langword="null"/> discards the item.
    /// </param>
    /// <returns><see langword="true"/> if the layout was restored; <see langword="false"/> if the file does not exist.</returns>
    public static bool Load(DockingManager manager, string path, Func<string, object?> resolveContent)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(resolveContent);

        if (!File.Exists(path))
        {
            return false;
        }

        using var reader = new StreamReader(path);
        Deserialize(manager, reader, resolveContent);
        return true;
    }

    /// <summary>Restores a layout from an XML string produced by <see cref="SaveToString"/>.</summary>
    /// <param name="manager">The target <see cref="DockingManager"/>.</param>
    /// <param name="xml">The serialized layout XML string.</param>
    /// <param name="resolveContent">
    /// Resolver that maps a <c>ContentId</c> to pane content (such as a control). Returning <see langword="null"/> discards the item.
    /// </param>
    public static void LoadFromString(DockingManager manager, string xml, Func<string, object?> resolveContent)
    {
        ArgumentNullException.ThrowIfNull(manager);
        ArgumentException.ThrowIfNullOrWhiteSpace(xml);
        ArgumentNullException.ThrowIfNull(resolveContent);

        using var reader = new StringReader(xml);
        Deserialize(manager, reader, resolveContent);
    }

    private static void Deserialize(DockingManager manager, TextReader reader, Func<string, object?> resolveContent)
    {
        var serializer = new XmlLayoutSerializer(manager);
        serializer.LayoutSerializationCallback += (_, e) =>
        {
            // Skip items without a ContentId or unknown to the resolver.
            var content = string.IsNullOrEmpty(e.Model.ContentId)
                ? null
                : resolveContent(e.Model.ContentId);

            if (content is null)
            {
                e.Cancel = true;
            }
            else
            {
                e.Content = content;
            }
        };

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
        };
        using var xmlReader = XmlReader.Create(reader, settings);
        serializer.Deserialize(xmlReader);
    }
}
