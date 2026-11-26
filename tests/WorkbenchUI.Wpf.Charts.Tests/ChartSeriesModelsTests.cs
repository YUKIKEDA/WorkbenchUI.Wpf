using System.Windows.Media;
using WorkbenchUI.Wpf.Charts;

namespace WorkbenchUI.Wpf.Charts.Tests;

public class ChartSeriesModelsTests
{
    [Fact]
    public void ChartSeries_RaisesPropertyChanged()
    {
        var series = new ChartSeries();
        var changed = new List<string?>();
        series.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        series.Name = "Force";
        series.X = [0.0, 1.0];
        series.Y = [10.0, 20.0];
        series.Color = Colors.CornflowerBlue;
        series.LineWidth = 3.0;

        Assert.Equal(["Name", "X", "Y", "Color", "LineWidth"], changed);
    }

    [Fact]
    public void ConvergenceSeries_AppendSnapshotClear_TracksValuesAndRaisesChanged()
    {
        var series = new ConvergenceSeries("Momentum");
        var changedCount = 0;
        series.Changed += (_, _) => changedCount++;

        series.Append(1.0);
        series.Append(1e-2);
        Assert.Equal(2, series.Count);
        Assert.Equal([1.0, 1e-2], series.Snapshot());
        Assert.Equal(2, changedCount);

        series.Clear();
        Assert.Equal(0, series.Count);
        Assert.Empty(series.Snapshot());
        Assert.Equal(3, changedCount);
    }

    [Fact]
    public void FrequencyResponseSeries_RaisesPropertyChanged()
    {
        var series = new FrequencyResponseSeries();
        var changed = new List<string?>();
        series.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        series.Name = "FRF-1";
        series.Frequencies = [10.0, 100.0];
        series.Magnitudes = [1.0, 0.5];
        series.Phases = [0.0, -45.0];

        Assert.Equal(["Name", "Frequencies", "Magnitudes", "Phases"], changed);
    }
}
