using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MemCleaner.Controls;

public sealed class RingGauge : FrameworkElement
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(RingGauge),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender, OnValueChanged));

    public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register(
        nameof(Thickness), typeof(double), typeof(RingGauge),
        new FrameworkPropertyMetadata(14d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TrackBrushProperty = DependencyProperty.Register(
        nameof(TrackBrush), typeof(Brush), typeof(RingGauge),
        new FrameworkPropertyMetadata(Frozen(0x1D, 0x26, 0x21), FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty AccentBrushProperty = DependencyProperty.Register(
        nameof(AccentBrush), typeof(Brush), typeof(RingGauge),
        new FrameworkPropertyMetadata(Frozen(0x3D, 0xDC, 0x84), FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty AccentEndBrushProperty = DependencyProperty.Register(
        nameof(AccentEndBrush), typeof(Brush), typeof(RingGauge),
        new FrameworkPropertyMetadata(Frozen(0x2B, 0xB5, 0x6B), FrameworkPropertyMetadataOptions.AffectsRender));

    private static readonly DependencyProperty AnimatedValueProperty = DependencyProperty.Register(
        nameof(AnimatedValue), typeof(double), typeof(RingGauge),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    private Pen? _trackPen;
    private Pen? _accentPen;
    private Pen? _glowPen;
    private double _cachedThickness = -1;
    private Brush? _cachedTrack;
    private Brush? _cachedAccent;
    private Brush? _cachedAccentEnd;

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Thickness
    {
        get => (double)GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    public Brush TrackBrush
    {
        get => (Brush)GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public Brush AccentBrush
    {
        get => (Brush)GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    public Brush AccentEndBrush
    {
        get => (Brush)GetValue(AccentEndBrushProperty);
        set => SetValue(AccentEndBrushProperty, value);
    }

    private double AnimatedValue
    {
        get => (double)GetValue(AnimatedValueProperty);
        set => SetValue(AnimatedValueProperty, value);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var gauge = (RingGauge)d;
        var target = Math.Clamp((double)e.NewValue, 0, 100);
        var animation = new DoubleAnimation(target, TimeSpan.FromMilliseconds(560))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        gauge.BeginAnimation(AnimatedValueProperty, animation, HandoffBehavior.SnapshotAndReplace);
    }

    protected override void OnRender(DrawingContext context)
    {
        var width = ActualWidth;
        var height = ActualHeight;

        if (width < 12 || height < 12)
        {
            return;
        }

        var size = Math.Min(width, height);
        var thickness = Math.Min(Thickness, size / 4);
        var radius = size / 2 - thickness / 2 - 1;
        var center = new Point(width / 2, height / 2);
        var value = Math.Clamp(AnimatedValue, 0, 100);
        var sweep = value * 3.6;

        EnsurePens(thickness);
        context.DrawEllipse(null, _trackPen, center, radius, radius);

        if (sweep < 0.4)
        {
            return;
        }

        if (_glowPen is not null)
        {
            context.DrawGeometry(null, _glowPen, ArcGeometry(center, radius, sweep));
        }

        context.DrawGeometry(null, _accentPen, ArcGeometry(center, radius, sweep));
    }

    private void EnsurePens(double thickness)
    {
        if (Math.Abs(_cachedThickness - thickness) < 0.1
            && ReferenceEquals(_cachedTrack, TrackBrush)
            && ReferenceEquals(_cachedAccent, AccentBrush)
            && ReferenceEquals(_cachedAccentEnd, AccentEndBrush)
            && _accentPen is not null)
        {
            return;
        }

        _cachedThickness = thickness;
        _cachedTrack = TrackBrush;
        _cachedAccent = AccentBrush;
        _cachedAccentEnd = AccentEndBrush;

        _trackPen = new Pen(TrackBrush, thickness);
        _trackPen.Freeze();

        var startColor = (AccentBrush as SolidColorBrush)?.Color ?? Color.FromRgb(0x3D, 0xDC, 0x84);
        var endColor = (AccentEndBrush as SolidColorBrush)?.Color ?? Color.FromRgb(0x2B, 0xB5, 0x6B);
        var gradient = new LinearGradientBrush(startColor, endColor, new Point(0, 0), new Point(1, 1));
        gradient.Freeze();

        _accentPen = new Pen(gradient, thickness)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };
        _accentPen.Freeze();

        var glowBrush = new SolidColorBrush(startColor) { Opacity = 0.16 };
        glowBrush.Freeze();
        _glowPen = new Pen(glowBrush, thickness + 9)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round
        };
        _glowPen.Freeze();
    }

    private static Geometry ArcGeometry(Point center, double radius, double sweep)
    {
        if (sweep >= 359.5)
        {
            var circle = new EllipseGeometry(center, radius, radius);
            circle.Freeze();
            return circle;
        }

        var start = PointOnCircle(center, radius, -90);
        var end = PointOnCircle(center, radius, -90 + sweep);
        var geometry = new StreamGeometry();

        using (var context = geometry.Open())
        {
            context.BeginFigure(start, false, false);
            context.ArcTo(end, new Size(radius, radius), 0, sweep > 180, SweepDirection.Clockwise, true, false);
        }

        geometry.Freeze();
        return geometry;
    }

    private static Point PointOnCircle(Point center, double radius, double angle)
    {
        var radians = angle * Math.PI / 180;
        return new Point(center.X + radius * Math.Cos(radians), center.Y + radius * Math.Sin(radians));
    }

    private static Brush Frozen(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
