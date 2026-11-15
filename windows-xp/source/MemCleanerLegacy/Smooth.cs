using System;
using System.Diagnostics;

namespace MemCleanerLegacy
{
    internal sealed class Smooth
    {
        private const double DurationMilliseconds = 520;

        private double _from;
        private double _target;
        private long _startedAt;

        public double Value { get; private set; }

        public bool Running { get; private set; }

        public void Jump(double value)
        {
            Value = value;
            _target = value;
            Running = false;
        }

        public void To(double value)
        {
            if (Math.Abs(value - _target) < 0.01 && !Running)
            {
                return;
            }

            _from = Value;
            _target = value;
            _startedAt = Stopwatch.GetTimestamp();
            Running = true;
        }

        public bool Step()
        {
            if (!Running)
            {
                return false;
            }

            var elapsed = (Stopwatch.GetTimestamp() - _startedAt) * 1000.0 / Stopwatch.Frequency;
            var progress = Math.Min(1, elapsed / DurationMilliseconds);
            var eased = 1 - Math.Pow(1 - progress, 3);
            Value = _from + (_target - _from) * eased;

            if (progress >= 1)
            {
                Value = _target;
                Running = false;
            }

            return true;
        }
    }
}
