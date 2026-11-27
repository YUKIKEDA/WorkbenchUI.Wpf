using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using WorkbenchUI.Wpf.Charts;

namespace WorkbenchUI.Wpf.Charts.Tests;

public class ChartSeriesModelsTests
{
    private sealed class TrackingChartSeries : ChartSeries, INotifyPropertyChanged
    {
        public int SubscriberCount { get; private set; }

        event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
        {
            add
            {
                PropertyChanged += value;
                SubscriberCount++;
            }
            remove
            {
                PropertyChanged -= value;
                SubscriberCount--;
            }
        }
    }

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

    [Fact]
    public void HistoryChart_MismatchedArrayLengths_DoesNotThrow()
    {
        StaTestRunner.Run(() =>
        {
            var chart = new HistoryChart
            {
                SeriesSource = new[]
                {
                    new ChartSeries { X = [1.0, 2.0, 3.0], Y = [10.0, 20.0] },
                },
            };

            chart.BeginInit();
            chart.EndInit();
            chart.Measure(new Size(400, 300));
            chart.Arrange(new Rect(0, 0, 400, 300));
            chart.UpdateLayout();
            var plot = Assert.IsType<WorkbenchPlot>(chart.Template.FindName("PART_Plot", chart));
            Assert.NotEmpty(plot.Plot.GetPlottables());
        });
    }

    [Fact]
    public void FrequencyResponsePlot_MismatchedArrayLengths_DoesNotThrow()
    {
        StaTestRunner.Run(() =>
        {
            var chart = new FrequencyResponsePlot
            {
                SeriesSource = new[]
                {
                    new FrequencyResponseSeries
                    {
                        Frequencies = [10.0, 100.0, 1000.0],
                        Magnitudes = [1.0, 0.5],
                        Phases = [0.0],
                    },
                },
            };

            chart.BeginInit();
            chart.EndInit();
            chart.Measure(new Size(400, 300));
            chart.Arrange(new Rect(0, 0, 400, 300));
            chart.UpdateLayout();
            var magPlot = Assert.IsType<WorkbenchPlot>(chart.Template.FindName("PART_MagnitudePlot", chart));
            var phasePlot = Assert.IsType<WorkbenchPlot>(chart.Template.FindName("PART_PhasePlot", chart));
            Assert.NotEmpty(magPlot.Plot.GetPlottables());
            Assert.NotEmpty(phasePlot.Plot.GetPlottables());
        });
    }

    [Fact]
    public void HistoryChart_RemoveSeries_DetachesPropertyChangedHandler()
    {
        StaTestRunner.Run(() =>
        {
            var series = new TrackingChartSeries { X = [0.0, 1.0], Y = [1.0, 2.0] };
            var collection = new ObservableCollection<ChartSeries> { series };
            var chart = new HistoryChart { SeriesSource = collection };

            Assert.Equal(1, series.SubscriberCount);

            collection.Remove(series);
            Assert.Equal(0, series.SubscriberCount);

            collection.Add(series);
            Assert.Equal(1, series.SubscriberCount);

            collection.Clear();
            Assert.Equal(0, series.SubscriberCount);

            _ = chart;
        });
    }
}
