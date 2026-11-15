using System.IO;
using MemCleaner.Localization;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace MemCleaner.Services;

public sealed class TrayService : IDisposable
{
    private readonly Forms.NotifyIcon _icon;
    private readonly Forms.ToolStripMenuItem _openItem;
    private readonly Forms.ToolStripMenuItem _cleanItem;
    private readonly Forms.ToolStripMenuItem _autoItem;
    private readonly Forms.ToolStripMenuItem _settingsItem;
    private readonly Forms.ToolStripMenuItem _exitItem;
    private string _tooltip = string.Empty;

    public TrayService()
    {
        _icon = new Forms.NotifyIcon
        {
            Text = "memcleaner",
            Icon = LoadIcon(),
            Visible = true
        };

        var menu = new Forms.ContextMenuStrip
        {
            Renderer = new TrayMenuRenderer(),
            BackColor = Drawing.Color.FromArgb(16, 20, 18),
            ForeColor = Drawing.Color.FromArgb(230, 237, 232),
            ShowImageMargin = false,
            Font = new Drawing.Font("Segoe UI", 9f),
            Padding = new Forms.Padding(2, 4, 2, 4)
        };

        _openItem = new Forms.ToolStripMenuItem
        {
            Font = new Drawing.Font("Segoe UI", 9f, Drawing.FontStyle.Bold)
        };
        _openItem.Click += (_, _) => OpenRequested?.Invoke();

        _cleanItem = new Forms.ToolStripMenuItem();
        _cleanItem.Click += (_, _) => CleanRequested?.Invoke();

        _autoItem = new Forms.ToolStripMenuItem();
        _autoItem.Click += (_, _) => AutoCleanToggled?.Invoke();

        _settingsItem = new Forms.ToolStripMenuItem();
        _settingsItem.Click += (_, _) => SettingsRequested?.Invoke();

        _exitItem = new Forms.ToolStripMenuItem();
        _exitItem.Click += (_, _) => ExitRequested?.Invoke();

        menu.Items.Add(_openItem);
        menu.Items.Add(_cleanItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(_autoItem);
        menu.Items.Add(_settingsItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(_exitItem);

        RefreshLanguage();

        _icon.ContextMenuStrip = menu;
        _icon.DoubleClick += (_, _) => OpenRequested?.Invoke();
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == Forms.MouseButtons.Left)
            {
                OpenRequested?.Invoke();
            }
        };
    }

    public event Action? OpenRequested;
    public event Action? CleanRequested;
    public event Action? SettingsRequested;
    public event Action? ExitRequested;
    public event Action? AutoCleanToggled;

    public void RefreshLanguage()
    {
        _openItem.Text = Loc.T("trayOpen");
        _cleanItem.Text = Loc.T("trayClean");
        _autoItem.Text = Loc.T("trayAuto");
        _settingsItem.Text = Loc.T("traySettings");
        _exitItem.Text = Loc.T("trayExit");
    }

    public bool AutoCleanChecked
    {
        set
        {
            if (_autoItem.Checked != value)
            {
                _autoItem.Checked = value;
            }
        }
    }

    public void UpdateTooltip(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text == _tooltip)
        {
            return;
        }

        _tooltip = text;
        _icon.Text = text.Length > 120 ? text[..120] : text;
    }

    public void Notify(string message)
    {
        _icon.BalloonTipTitle = Loc.T("notificationTitle");
        _icon.BalloonTipText = message;
        _icon.ShowBalloonTip(3500);
    }

    private static Drawing.Icon LoadIcon()
    {
        try
        {
            using var stream = System.Windows.Application.GetResourceStream(
                new Uri("pack://application:,,,/Assets/app.ico"))?.Stream;

            if (stream is null)
            {
                return Drawing.SystemIcons.Application;
            }

            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            memory.Position = 0;
            return new Drawing.Icon(memory);
        }
        catch (Exception error)
        {
            LogService.Error("Не удалось загрузить значок для трея", error);
            return Drawing.SystemIcons.Application;
        }
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Icon?.Dispose();
        _icon.ContextMenuStrip?.Dispose();
        _icon.Dispose();
    }

    private sealed class TrayColors : Forms.ProfessionalColorTable
    {
        public override Drawing.Color MenuItemSelected => Drawing.Color.FromArgb(28, 36, 31);
        public override Drawing.Color MenuItemBorder => Drawing.Color.FromArgb(44, 56, 48);
        public override Drawing.Color MenuBorder => Drawing.Color.FromArgb(38, 46, 42);
        public override Drawing.Color ToolStripDropDownBackground => Drawing.Color.FromArgb(16, 20, 18);
        public override Drawing.Color ImageMarginGradientBegin => Drawing.Color.FromArgb(16, 20, 18);
        public override Drawing.Color ImageMarginGradientMiddle => Drawing.Color.FromArgb(16, 20, 18);
        public override Drawing.Color ImageMarginGradientEnd => Drawing.Color.FromArgb(16, 20, 18);
        public override Drawing.Color SeparatorDark => Drawing.Color.FromArgb(40, 48, 43);
        public override Drawing.Color SeparatorLight => Drawing.Color.FromArgb(40, 48, 43);
    }

    private sealed class TrayMenuRenderer : Forms.ToolStripProfessionalRenderer
    {
        public TrayMenuRenderer()
            : base(new TrayColors())
        {
            RoundedEdges = false;
        }

        protected override void OnRenderItemText(Forms.ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item is null || !e.Item.Enabled
                ? Drawing.Color.FromArgb(110, 122, 115)
                : Drawing.Color.FromArgb(230, 237, 232);
            base.OnRenderItemText(e);
        }

        protected override void OnRenderToolStripBackground(Forms.ToolStripRenderEventArgs e)
        {
            using var brush = new Drawing.SolidBrush(Drawing.Color.FromArgb(16, 20, 18));
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(Forms.ToolStripRenderEventArgs e)
        {
            using var pen = new Drawing.Pen(Drawing.Color.FromArgb(38, 46, 42));
            e.Graphics.DrawRectangle(pen, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }

        protected override void OnRenderSeparator(Forms.ToolStripSeparatorRenderEventArgs e)
        {
            using var pen = new Drawing.Pen(Drawing.Color.FromArgb(40, 48, 43));
            var y = e.Item.Height / 2;
            e.Graphics.DrawLine(pen, 8, y, e.Item.Width - 8, y);
        }

        protected override void OnRenderMenuItemBackground(Forms.ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected || !e.Item.Enabled)
            {
                return;
            }

            using var brush = new Drawing.SolidBrush(Drawing.Color.FromArgb(28, 36, 31));
            var bounds = new Drawing.Rectangle(4, 1, e.Item.Width - 8, e.Item.Height - 2);
            e.Graphics.FillRectangle(brush, bounds);
        }
    }
}
