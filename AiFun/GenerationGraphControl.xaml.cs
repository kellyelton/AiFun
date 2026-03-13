using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AiFun
{
    public partial class GenerationGraphControl : UserControl
    {
        private ObservableCollection<GenerationStats> _history;
        private readonly Dictionary<string, bool> _seriesVisible = new()
        {
            ["BestSurvival"] = true,
            ["AvgSurvival"] = true,
            ["BestDistance"] = true,
            ["AvgVision"] = true,
            ["TotalBabies"] = true,
            ["FoodEaten"] = true
        };

        private static readonly Dictionary<string, Color> SeriesColors = new()
        {
            ["BestSurvival"] = Color.FromRgb(79, 195, 247),
            ["AvgSurvival"] = Color.FromRgb(129, 199, 132),
            ["BestDistance"] = Color.FromRgb(255, 183, 77),
            ["AvgVision"] = Color.FromRgb(206, 147, 216),
            ["TotalBabies"] = Color.FromRgb(240, 98, 146),
            ["FoodEaten"] = Color.FromRgb(139, 195, 74)
        };

        public GenerationGraphControl()
        {
            InitializeComponent();
        }

        public void BindToHistory(ObservableCollection<GenerationStats> history)
        {
            if (_history != null)
                _history.CollectionChanged -= OnHistoryChanged;

            _history = history;
            _history.CollectionChanged += OnHistoryChanged;
            Redraw();
        }

        private void OnHistoryChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Redraw();
        }

        private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Redraw();
        }

        private void Legend_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement el && el.Tag is string key && _seriesVisible.ContainsKey(key))
            {
                _seriesVisible[key] = !_seriesVisible[key];
                UpdateLegendOpacity();
                Redraw();
            }
        }

        private void UpdateLegendOpacity()
        {
            LegendBestSurvival.Opacity = _seriesVisible["BestSurvival"] ? 1.0 : 0.3;
            LegendAvgSurvival.Opacity = _seriesVisible["AvgSurvival"] ? 1.0 : 0.3;
            LegendBestDistance.Opacity = _seriesVisible["BestDistance"] ? 1.0 : 0.3;
            LegendAvgVision.Opacity = _seriesVisible["AvgVision"] ? 1.0 : 0.3;
            LegendTotalBabies.Opacity = _seriesVisible["TotalBabies"] ? 1.0 : 0.3;
            LegendFoodEaten.Opacity = _seriesVisible["FoodEaten"] ? 1.0 : 0.3;
        }

        private void Redraw()
        {
            ChartCanvas.Children.Clear();
            XAxisCanvas.Children.Clear();

            if (_history == null || _history.Count == 0)
            {
                NoDataText.Visibility = Visibility.Visible;
                return;
            }
            NoDataText.Visibility = Visibility.Collapsed;

            var w = ChartCanvas.ActualWidth;
            var h = ChartCanvas.ActualHeight;
            if (w <= 0 || h <= 0) return;

            var stats = _history.ToList();
            var count = stats.Count;

            // Draw grid lines (just horizontal reference lines, no numeric labels)
            DrawGridLines(w, h, stats);

            // Each series is independently scaled to its own max,
            // so trends are visually comparable regardless of magnitude.
            // The latest value is shown at the end of each line.
            var seriesDefs = new (string key, Func<GenerationStats, double> selector, double thickness)[]
            {
                ("BestSurvival", s => s.BestSurvivalTime, 2.0),
                ("AvgSurvival", s => s.AvgSurvivalTime, 1.5),
                ("BestDistance", s => s.BestDistance, 2.0),
                ("AvgVision", s => s.AvgVisionDistance, 1.5),
                ("TotalBabies", s => (double)s.TotalBabies, 1.5),
                ("FoodEaten", s => s.TotalFoodEaten, 1.5),
            };

            // Collect end label positions, then resolve overlaps
            var endLabels = new List<(double y, double value, Color color, int trend)>();

            foreach (var (key, selector, thickness) in seriesDefs)
            {
                if (!_seriesVisible[key]) continue;

                var values = stats.Select(selector).ToList();
                var seriesMax = values.Max();
                if (seriesMax <= 0) seriesMax = 1;
                seriesMax *= 1.1; // headroom

                DrawSeries(stats, selector, SeriesColors[key], w, h, 0, seriesMax, thickness);

                var lastValue = values[values.Count - 1];
                var y = h - ((lastValue / seriesMax) * h);
                var trend = ComputeTrend(values);
                endLabels.Add((y, lastValue, SeriesColors[key], trend));
            }

            // Resolve overlapping labels by pushing them apart
            ResolveEndLabelOverlaps(endLabels, h);

            foreach (var (y, value, color, trend) in endLabels)
                DrawEndLabel(value, color, w, y, trend);
        }

        private void DrawGridLines(double w, double h, List<GenerationStats> stats)
        {
            // Horizontal reference lines (no numeric labels since each series has its own scale)
            var gridLineCount = 4;
            for (int i = 0; i <= gridLineCount; i++)
            {
                var fraction = (double)i / gridLineCount;
                var y = h - (fraction * h);

                var line = new Line
                {
                    X1 = 0, Y1 = y, X2 = w, Y2 = y,
                    Stroke = new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)),
                    StrokeThickness = 1
                };
                ChartCanvas.Children.Add(line);
            }

            // X-axis labels (generation numbers)
            var count = stats.Count;
            var labelStep = Math.Max(1, count / 5);
            for (int i = 0; i < count; i += labelStep)
            {
                var x = count == 1 ? w / 2 : (double)i / (count - 1) * w;
                var label = new TextBlock
                {
                    Text = stats[i].Generation.ToString(),
                    Foreground = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
                    FontSize = 9
                };
                Canvas.SetLeft(label, x - 6);
                Canvas.SetTop(label, 0);
                XAxisCanvas.Children.Add(label);
            }

            // Always show last generation label if not already shown
            if (count > 1 && (count - 1) % labelStep != 0)
            {
                var x = w;
                var label = new TextBlock
                {
                    Text = stats[count - 1].Generation.ToString(),
                    Foreground = new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
                    FontSize = 9
                };
                Canvas.SetLeft(label, x - 12);
                Canvas.SetTop(label, 0);
                XAxisCanvas.Children.Add(label);
            }
        }

        private void DrawSeries(List<GenerationStats> stats, Func<GenerationStats, double> selector,
            Color color, double w, double h, double yMin, double yMax, double thickness)
        {
            var count = stats.Count;
            if (count == 0) return;

            var points = new PointCollection();
            for (int i = 0; i < count; i++)
            {
                var x = count == 1 ? w / 2 : (double)i / (count - 1) * w;
                var value = selector(stats[i]);
                var y = h - ((value - yMin) / (yMax - yMin) * h);
                points.Add(new Point(x, y));
            }

            var polyline = new Polyline
            {
                Points = points,
                Stroke = new SolidColorBrush(color),
                StrokeThickness = thickness,
                StrokeLineJoin = PenLineJoin.Round,
                IsHitTestVisible = false
            };
            ChartCanvas.Children.Add(polyline);

            // Draw dots for each data point (small, if not too many)
            if (count <= 50)
            {
                foreach (var pt in points)
                {
                    var dot = new Ellipse
                    {
                        Width = 4, Height = 4,
                        Fill = new SolidColorBrush(color),
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(dot, pt.X - 2);
                    Canvas.SetTop(dot, pt.Y - 2);
                    ChartCanvas.Children.Add(dot);
                }
            }
        }

        private void ResolveEndLabelOverlaps(List<(double y, double value, Color color, int trend)> labels, double chartHeight)
        {
            const double labelHeight = 14.0;

            // Sort by Y position
            labels.Sort((a, b) => a.y.CompareTo(b.y));

            // Push overlapping labels apart
            for (int i = 1; i < labels.Count; i++)
            {
                var gap = labels[i].y - labels[i - 1].y;
                if (gap < labelHeight)
                {
                    var push = (labelHeight - gap) / 2.0;
                    labels[i - 1] = (labels[i - 1].y - push, labels[i - 1].value, labels[i - 1].color, labels[i - 1].trend);
                    labels[i] = (labels[i].y + push, labels[i].value, labels[i].color, labels[i].trend);

                    // Ripple upward if we pushed the previous one into another
                    for (int j = i - 1; j > 0; j--)
                    {
                        var gapAbove = labels[j].y - labels[j - 1].y;
                        if (gapAbove < labelHeight)
                        {
                            labels[j - 1] = (labels[j].y - labelHeight, labels[j - 1].value, labels[j - 1].color, labels[j - 1].trend);
                        }
                        else break;
                    }
                }
            }

            // Clamp to chart bounds
            for (int i = 0; i < labels.Count; i++)
            {
                var y = Math.Max(0, Math.Min(chartHeight - labelHeight, labels[i].y));
                labels[i] = (y, labels[i].value, labels[i].color, labels[i].trend);
            }
        }

        private static int ComputeTrend(List<double> values)
        {
            if (values.Count < 2) return 0;
            var lookback = Math.Min(10, values.Count - 1);
            var previous = values.Skip(values.Count - 1 - lookback).Take(lookback).Average();
            var current = values[values.Count - 1];
            var threshold = Math.Max(previous * 0.01, 0.001); // 1% change threshold
            if (current > previous + threshold) return 1;  // trending up
            if (current < previous - threshold) return -1; // trending down
            return 0; // flat
        }

        private void DrawEndLabel(double value, Color color, double w, double y, int trend)
        {
            var trendText = trend > 0 ? " \u25B2" : trend < 0 ? " \u25BC" : "";
            var trendColor = trend > 0 ? Color.FromRgb(76, 175, 80) : Color.FromRgb(244, 67, 54);

            var panel = new StackPanel { Orientation = Orientation.Horizontal };

            panel.Children.Add(new TextBlock
            {
                Text = FormatValue(value),
                Foreground = new SolidColorBrush(color),
                FontSize = 9,
                FontWeight = System.Windows.FontWeights.SemiBold
            });

            if (trend != 0)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = trendText,
                    Foreground = new SolidColorBrush(trendColor),
                    FontSize = 8,
                    FontWeight = System.Windows.FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            Canvas.SetLeft(panel, w + 3);
            Canvas.SetTop(panel, y - 7);
            ChartCanvas.Children.Add(panel);
        }

        private static string FormatValue(double value)
        {
            if (value >= 10000) return $"{value / 1000:F0}k";
            if (value >= 1000) return $"{value / 1000:F1}k";
            if (value >= 100) return $"{value:F0}";
            if (value >= 10) return $"{value:F1}";
            return $"{value:F2}";
        }
    }
}
