using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace MemCleaner.Controls;

public sealed class Sparkline : FrameworkElement
{
    public static readonly DependencyProperty SamplesProperty = DependencyProperty.Register(
        nameof(Samples), typeof(IEnumerable<double>), typeof(Sparkline),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnSamplesChanged));

    public static readonly DependencyProperty AccentBrushProperty = DependencyProperty.Register(
        nameof(AccentBrush), typeof(Brush), typeof(Sparkline),
        new FrameworkPropertyMetadata(DefaultAccent(), FrameworkPropertyMetadataOptions.AffectsRender));

    private const double TransitionMilliseconds = 600;

    private readonly DispatcherTimer _timer;
    private double[] _display = Array.Empty<double>();
    private double[] _from = Array.Empty<double>();
    private double[] _target = Array.Empty<double>();
    private long _startedAt;

    private Pen? _linePen;
    private Brush? _fillBrush;
    private Brush? _haloBrush;
    private Brush? _dotBrush;
    private Brush? _cachedAccent;

    public Sparkline()
    {
        _timer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (_, _) => Step();
        Unloaded += (_, _) => _timer.Stop();
    }

    public IEnumerable<double>? Samples
    {
        get => (IEnumerable<double>?)GetValue(SamplesProperty);
        set => SetValue(SamplesProperty, value);
    }

    public Brush AccentBrush
    {
        get => (Brush)GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    protected override void OnRender(DrawingContext context)
    {
        var values = _display.Length >= 2 ? _display : Samples?.ToArray();

        if (values is null || values.Length < 2)
        {
            return;
        }

        var width = ActualWidth;
        var height = ActualHeight;

        if (width < 20 || height < 10)
        {
            return;
        }

        var minimum = values.Min();
        var maximum = values.Max();

        if (maximum - minimum < 6)
        {
            var middle = (maximum + minimum) / 2;
            minimum = middle - 3;
            maximum = middle + 3;
        }

        var padding = (maximum - minimum) * 0.18;
        minimum -= padding;
        maximum += padding;

        var top = 4d;
        var bottom = height - 2;
        var step = width / (values.Length - 1);
        var points = new Point[values.Length];

        for (var index = 0; index < values.Length; index++)
        {
            var ratio = (values[index] - minimum) / (maximum - minimum);
            var x = index * step;
            var y = bottom - ratio * (bottom - top);
            points[index] = new Point(x, y);
        }

        var accent = AccentBrush as SolidColorBrush ?? new SolidColorBrush(Color.FromRgb(0x3D, 0xDC, 0x84));
        var color = accent.Color;
        EnsureBrushes(accent, color);

        var area = new StreamGeometry();

        using (var geometry = area.Open())
        {
            geometry.BeginFigure(new Point(0, bottom), true, true);
            geometry.PolyLineTo(points, true, false);
            geometry.LineTo(new Point(width, bottom), true, false);
        }

        area.Freeze();
        context.DrawGeometry(_fillBrush, null, area);

        var line = new StreamGeometry();

        using (var geometry = line.Open())
        {
            geometry.BeginFigure(points[0], false, false);
            geometry.PolyLineTo(points.Skip(1).ToArray(), true, false);
        }

        line.Freeze();
        context.DrawGeometry(null, _linePen, line);

        var last = points[^1];
        context.DrawEllipse(_haloBrush, null, last, 6, 6);
        context.DrawEllipse(_dotBrush, null, last, 2.6, 2.6);
    }

    private static void OnSamplesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var sparkline = (Sparkline)d;
        var values = (e.NewValue as IEnumerable<double>)?.ToArray();

        if (values is null)
        {
            sparkline._display = Array.Empty<double>();
            sparkline._timer.Stop();
            sparkline.InvalidateVisual();
            return;
        }

        if (sparkline._display.Length != values.Length || sparkline._display.Length == 0)
        {
            sparkline._display = values;
            sparkline._from = values;
            sparkline._target = values;
            sparkline._timer.Stop();
            sparkline.InvalidateVisual();
            return;
        }

        sparkline._from = (double[])sparkline._display.Clone();
        sparkline._target = values;
        sparkline._startedAt = Stopwatch.GetTimestamp();
        sparkline._timer.Start();
    }

    private void Step()
    {
        var elapsed = Stopwatch.GetElapsedTime(_startedAt).TotalMilliseconds;
        var progress = Math.Min(1, elapsed / TransitionMilliseconds);
        var eased = 1 - Math.Pow(1 - progress, 3);

        for (var index = 0; index < _display.Length; index++)
        {
            _display[index] = _from[index] + (_target[index] - _from[index]) * eased;
        }

        InvalidateVisual();

        if (progress >= 1)
        {
            _display = (double[])_target.Clone();
            _timer.Stop();
            InvalidateVisual();
        }
    }

    private void EnsureBrushes(SolidColorBrush accent, Color color)
    {
        if (ReferenceEquals(_cachedAccent, accent) && _linePen is not null)
        {
            return;
        }

        _cachedAccent = accent;

        var fill = new LinearGradientBrush(
            Color.FromArgb(52, color.R, color.G, color.B),
            Color.FromArgb(0, color.R, color.G, color.B),
            new Point(0, 0),
            new Point(0, 1));
        fill.Freeze();
        _fillBrush = fill;

        var line = new Pen(new SolidColorBrush(color), 1.8)
        {
            LineJoin = PenLineJoin.Round,
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };
        line.Freeze();
        _linePen = line;

        var halo = new SolidColorBrush(Color.FromArgb(60, color.R, color.G, color.B));
        halo.Freeze();
        _haloBrush = halo;

        var dot = new SolidColorBrush(color);
        dot.Freeze();
        _dotBrush = dot;
    }

    private static Brush DefaultAccent()
    {
        var brush = new SolidColorBrush(Color.FromRgb(0x3D, 0xDC, 0x84));
        brush.Freeze();
        return brush;
    }
}
