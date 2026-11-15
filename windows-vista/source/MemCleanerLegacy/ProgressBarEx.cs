using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MemCleanerLegacy
{
    internal sealed class ProgressBarEx : Control
    {
        private double _value;

        public ProgressBarEx()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Height = 8;
            BackColor = Color.FromArgb(0x1D, 0x26, 0x21);
            ForeColor = Color.FromArgb(0x3D, 0xDC, 0x84);
        }

        public double Value
        {
            get { return _value; }
            set
            {
                var clamped = Math.Max(0, Math.Min(100, value));

                if (Math.Abs(clamped - _value) < 0.05)
                {
                    return;
                }

                _value = clamped;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(BackColor);

            var width = (int)Math.Round(Width * _value / 100.0);

            if (width <= 0)
            {
                return;
            }

            using (var brush = new SolidBrush(ForeColor))
            using (var path = Rounded(new Rectangle(0, 0, Math.Max(4, width), Height), Height / 2))
            {
                graphics.FillPath(brush, path);
            }
        }

        private static GraphicsPath Rounded(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            var diameter = radius * 2;

            if (bounds.Width <= diameter)
            {
                path.AddEllipse(bounds);
                return path;
            }

            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
