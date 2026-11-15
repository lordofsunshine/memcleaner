using System.Diagnostics;

namespace MemCleaner.Infrastructure;

public sealed class AnimatedChannel
{
    private const double DurationMilliseconds = 520;

    private double _from;
    private double _target;
    private long _startedAt;
    private bool _running;

    public double Value { get; private set; }

    public bool IsRunning => _running;

    public void Jump(double value)
    {
        Value = value;
        _target = value;
        _running = false;
    }

    public void AnimateTo(double target, long timestamp)
    {
        if (Math.Abs(target - _target) < 0.01 && !_running)
        {
            return;
        }

        _from = Value;
        _target = target;
        _startedAt = timestamp;
        _running = true;
    }

    public bool Step(long timestamp)
    {
        if (!_running)
        {
            return false;
        }

        var elapsed = (timestamp - _startedAt) * 1000.0 / Stopwatch.Frequency;
        var progress = Math.Min(1, elapsed / DurationMilliseconds);
        var eased = 1 - Math.Pow(1 - progress, 3);
        Value = _from + (_target - _from) * eased;

        if (progress >= 1)
        {
            Value = _target;
            _running = false;
        }

        return true;
    }
}
