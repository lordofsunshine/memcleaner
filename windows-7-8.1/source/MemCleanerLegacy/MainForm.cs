using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using WinTimer = System.Windows.Forms.Timer;

namespace MemCleanerLegacy
{
    internal sealed class MainForm : Form
    {
        private static readonly Color Background = Color.FromArgb(0x0F, 0x14, 0x11);
        private static readonly Color Card = Color.FromArgb(0x15, 0x1C, 0x18);
        private static readonly Color Border = Color.FromArgb(0x24, 0x30, 0x2A);
        private static readonly Color TextColor = Color.FromArgb(0xE6, 0xED, 0xE8);
        private static readonly Color Muted = Color.FromArgb(0x94, 0xA3, 0x9B);
        private static readonly Color Dim = Color.FromArgb(0x6E, 0x7C, 0x74);
        private static readonly Color Accent = Color.FromArgb(0x3D, 0xDC, 0x84);
        private static readonly Color AccentSoft = Color.FromArgb(0x1B, 0x2E, 0x24);
        private static readonly Color Warning = Color.FromArgb(0xE2, 0xB4, 0x5F);

        private readonly LegacySettings _settings;
        private readonly bool _startHidden;
        private readonly EventWaitHandle _activateEvent;
        private readonly List<HistoryEntry> _history;
        private readonly List<AreaRow> _areaRows = new List<AreaRow>();
        private readonly List<LanguageOption> _languages = new List<LanguageOption>();
        private readonly Smooth _used = new Smooth();
        private readonly Smooth _free = new Smooth();
        private readonly Smooth _commit = new Smooth();
        private readonly Smooth _cache = new Smooth();
        private readonly Smooth _percent = new Smooth();
        private readonly WinTimer _monitorTimer = new WinTimer();
        private readonly WinTimer _animationTimer = new WinTimer();
        private readonly WinTimer _autoTimer = new WinTimer();

        private NotifyIcon _tray;
        private ToolStripMenuItem _trayOpenItem;
        private ToolStripMenuItem _trayExitItem;
        private bool _busy;
        private bool _updating;
        private bool _visible = true;
        private bool _exitRequested;
        private bool _thresholdArmed = true;
        private Thread _activationThread;
        private DateTime _lastClean = DateTime.Now;
        private ulong _totalBytes;
        private ulong _commitLimit;

        private Button _navOverview;
        private Button _navSettings;
        private Panel _overviewPanel;
        private Panel _settingsPanel;
        private Label _taglineLabel;
        private Label _versionLabel;
        private Label _percentLabel;
        private Label _percentCaption;
        private Label _totalLabel;
        private ProgressBarEx _bar;
        private Label _usedCaption;
        private Label _usedValue;
        private Label _freeCaption;
        private Label _freeValue;
        private Label _cacheCaption;
        private Label _cacheValue;
        private Label _commitCaption;
        private Label _commitValue;
        private Button _cleanButton;
        private Label _resultLabel;
        private Label _historyTitle;
        private Label _historyCaption;
        private Label _historyEmpty;
        private ListBox _historyList;

        private Label _languageTitle;
        private Label _languageDesc;
        private ComboBox _languageCombo;
        private Label _autoTitle;
        private Label _autoHint;
        private CheckBox _autoCheck;
        private CheckBox _intervalCheck;
        private NumericUpDown _intervalValue;
        private Label _intervalUnit;
        private CheckBox _thresholdCheck;
        private NumericUpDown _thresholdValue;
        private Label _percentUnit;
        private Label _cooldownLabel;
        private NumericUpDown _cooldownValue;
        private Label _cooldownUnit;
        private Label _areasTitle;
        private Label _areasNote;
        private Label _exceptionsTitle;
        private Label _exceptionsDesc;
        private Label _exceptionsHint;
        private TextBox _exceptionsBox;
        private Label _behaviorTitle;
        private CheckBox _trayCheck;
        private Label _trayDesc;
        private CheckBox _notifyCheck;
        private Label _notifyDesc;
        private CheckBox _startupCheck;
        private Label _startupDesc;
        private Label _startupMessage;
        private Label _saveStatus;
        private Label _capabilityNote;

        public MainForm(LegacySettings settings, bool startHidden, EventWaitHandle activateEvent)
        {
            _settings = settings;
            _startHidden = startHidden;
            _activateEvent = activateEvent;
            _history = HistoryStore.Load();

            BuildUi();
            CreateTray();
            ApplySettingsToUi();
            ApplyTexts();
            RebuildHistory();
            _settings.Save();

            _monitorTimer.Interval = 1500;
            _monitorTimer.Tick += (sender, e) => RefreshValues(false);
            _animationTimer.Interval = 16;
            _animationTimer.Tick += (sender, e) => StepAnimation();
            _autoTimer.Interval = 5000;
            _autoTimer.Tick += (sender, e) => CheckAutoClean();

            Shown += OnShown;
            RefreshValues(true);
            _monitorTimer.Start();
            _autoTimer.Start();
            StartActivationListener();
        }

        private static string UiFontName
        {
            get
            {
                using (var probe = new Font("Segoe UI", 9f))
                {
                    return probe.Name;
                }
            }
        }

        private static Font Ui(float size, FontStyle style)
        {
            return new Font(UiFontName, size, style);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            var area = Screen.FromControl(this).WorkingArea;
            var maxWidth = Math.Max(640, area.Width - 32);
            var maxHeight = Math.Max(520, area.Height - 32);

            if (Width > maxWidth || Height > maxHeight)
            {
                Width = Math.Min(Width, maxWidth);
                Height = Math.Min(Height, maxHeight);
                Left = area.Left + (area.Width - Width) / 2;
                Top = area.Top + (area.Height - Height) / 2;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            if (WindowState == FormWindowState.Minimized && _settings.MinimizeToTray)
            {
                HideToTray();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && _settings.MinimizeToTray && !_exitRequested)
            {
                e.Cancel = true;
                HideToTray();
                return;
            }

            _exitRequested = true;

            if (_tray != null)
            {
                _tray.Visible = false;
                _tray.Dispose();
                _tray = null;
            }

            _settings.Save();
            _monitorTimer.Stop();
            _animationTimer.Stop();
            _autoTimer.Stop();
            _activationThread.Join(600);
            base.OnFormClosing(e);
        }

        private void OnShown(object sender, EventArgs e)
        {
            if (_startHidden && _settings.MinimizeToTray)
            {
                HideToTray();
            }
        }

        private void HideToTray()
        {
            _visible = false;
            Hide();
            ShowInTaskbar = false;
        }

        private void ShowFromTray()
        {
            if (_exitRequested || IsDisposed)
            {
                return;
            }

            Show();
            ShowInTaskbar = true;
            WindowState = FormWindowState.Normal;
            _visible = true;
            Activate();
            RefreshValues(false);
        }

        private void BuildUi()
        {
            SuspendLayout();

            Text = "memcleaner";
            BackColor = Background;
            ForeColor = TextColor;
            Font = Ui(9f, FontStyle.Regular);
            ClientSize = new Size(880, 640);
            MinimumSize = new Size(640, 520);
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;

            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch (Exception)
            {
            }

            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 82,
                ColumnCount = 2,
                BackColor = Background,
                Padding = new Padding(20, 12, 22, 6)
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var brand = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0)
            };

            var logo = new PictureBox
            {
                Size = new Size(42, 42),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Image = LoadLogo(),
                Margin = new Padding(0, 2, 12, 0)
            };

            var titles = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 0)
            };

            var name = new Label
            {
                Text = "memcleaner",
                AutoSize = true,
                Font = Ui(15f, FontStyle.Bold),
                ForeColor = TextColor,
                Margin = new Padding(0)
            };

            _taglineLabel = new Label
            {
                AutoSize = true,
                Font = Ui(8.5f, FontStyle.Regular),
                ForeColor = Muted,
                Margin = new Padding(1, 3, 0, 0)
            };

            titles.Controls.Add(name);
            titles.Controls.Add(_taglineLabel);
            brand.Controls.Add(logo);
            brand.Controls.Add(titles);

            _versionLabel = new Label
            {
                AutoSize = true,
                Font = Ui(8.5f, FontStyle.Regular),
                ForeColor = Dim,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0, 24, 0, 0)
            };

            header.Controls.Add(brand, 0, 0);
            header.Controls.Add(_versionLabel, 1, 0);

            _navOverview = CreateNavButton();
            _navSettings = CreateNavButton();
            _navOverview.Click += (sender, e) => ShowPage(false);
            _navSettings.Click += (sender, e) => ShowPage(true);

            var nav = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Background,
                Padding = new Padding(20, 6, 20, 4),
                WrapContents = false
            };
            nav.Controls.Add(_navOverview);
            nav.Controls.Add(_navSettings);

            _overviewPanel = BuildOverview();
            _settingsPanel = BuildSettings();
            _settingsPanel.Visible = false;

            var content = new Panel { Dock = DockStyle.Fill, BackColor = Background };
            content.Controls.Add(_settingsPanel);
            content.Controls.Add(_overviewPanel);

            Controls.Add(content);
            Controls.Add(nav);
            Controls.Add(header);

            ResumeLayout();
            ShowPage(false);
        }

        private Button CreateNavButton()
        {
            var button = new Button
            {
                Width = 112,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                Font = Ui(9f, FontStyle.Regular),
                BackColor = Card,
                ForeColor = Muted,
                Margin = new Padding(0, 0, 6, 0),
                TabStop = false
            };

            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Border;
            return button;
        }

        private Panel BuildOverview()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Background,
                Padding = new Padding(20, 10, 20, 16),
                ColumnCount = 1,
                RowCount = 4
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 174));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var card = new Panel { Dock = DockStyle.Fill, BackColor = Card, Margin = new Padding(0) };

            _percentLabel = new Label
            {
                AutoSize = true,
                Font = Ui(32f, FontStyle.Bold),
                ForeColor = TextColor,
                Location = new Point(20, 12)
            };

            _percentCaption = new Label
            {
                AutoSize = true,
                Font = Ui(8.5f, FontStyle.Regular),
                ForeColor = Muted,
                Location = new Point(24, 70)
            };

            _totalLabel = new Label
            {
                AutoSize = true,
                Font = Ui(8.5f, FontStyle.Regular),
                ForeColor = Dim,
                Location = new Point(24, 90)
            };

            _bar = new ProgressBarEx
            {
                Location = new Point(24, 126),
                Height = 8,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            card.Controls.Add(_percentLabel);
            card.Controls.Add(_percentCaption);
            card.Controls.Add(_totalLabel);
            card.Controls.Add(_bar);
            card.Resize += (sender, e) => _bar.Width = Math.Max(40, card.ClientSize.Width - 48);

            root.Controls.Add(card, 0, 0);

            var stats = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Background,
                ColumnCount = 4,
                Margin = new Padding(0, 10, 0, 0)
            };

            for (var index = 0; index < 4; index++)
            {
                stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
            }

            Label caption;
            Label value;
            stats.Controls.Add(CreateStat(out caption, out value), 0, 0);
            _usedCaption = caption;
            _usedValue = value;

            stats.Controls.Add(CreateStat(out caption, out value), 1, 0);
            _freeCaption = caption;
            _freeValue = value;

            stats.Controls.Add(CreateStat(out caption, out value), 2, 0);
            _cacheCaption = caption;
            _cacheValue = value;

            stats.Controls.Add(CreateStat(out caption, out value), 3, 0);
            _commitCaption = caption;
            _commitValue = value;

            root.Controls.Add(stats, 0, 1);

            var actions = new Panel { Dock = DockStyle.Fill, BackColor = Background, Margin = new Padding(0, 8, 0, 0) };

            _cleanButton = new Button
            {
                Location = new Point(0, 2),
                Size = new Size(196, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = Accent,
                ForeColor = Color.FromArgb(0x07, 0x17, 0x0E),
                Font = Ui(9.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };
            _cleanButton.FlatAppearance.BorderSize = 0;
            _cleanButton.Click += (sender, e) => BeginClean("manual");

            _resultLabel = new Label
            {
                Location = new Point(212, 4),
                Height = 40,
                AutoSize = false,
                Font = Ui(9f, FontStyle.Regular),
                ForeColor = Muted,
                TextAlign = ContentAlignment.MiddleLeft,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _resultLabel.Text = string.Empty;

            actions.Controls.Add(_cleanButton);
            actions.Controls.Add(_resultLabel);
            actions.Resize += (sender, e) =>
            {
                _resultLabel.Width = Math.Max(80, actions.ClientSize.Width - 216);
            };

            root.Controls.Add(actions, 0, 2);

            var historyCard = new Panel { Dock = DockStyle.Fill, BackColor = Card, Margin = new Padding(0) };
            var historyLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(20, 14, 20, 12)
            };
            historyLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            historyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var historyHeader = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) };
            _historyTitle = new Label
            {
                AutoSize = true,
                Font = Ui(10.5f, FontStyle.Bold),
                ForeColor = TextColor,
                Location = new Point(0, 2)
            };
            _historyCaption = new Label
            {
                AutoSize = true,
                Font = Ui(8.5f, FontStyle.Regular),
                ForeColor = Dim,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(historyHeader.Width - 100, 4)
            };
            historyHeader.Resize += (sender, e) =>
            {
                _historyCaption.Left = Math.Max(0, historyHeader.ClientSize.Width - _historyCaption.Width);
            };
            historyHeader.Controls.Add(_historyTitle);
            historyHeader.Controls.Add(_historyCaption);
            historyLayout.Controls.Add(historyHeader, 0, 0);

            var historyBody = new Panel { Dock = DockStyle.Fill, BackColor = Card, Margin = new Padding(0, 4, 0, 0) };
            _historyList = new ListBox
            {
                Dock = DockStyle.Fill,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 28,
                BorderStyle = BorderStyle.None,
                BackColor = Card,
                ForeColor = TextColor,
                Font = Ui(9f, FontStyle.Regular),
                IntegralHeight = false
            };
            _historyList.DrawItem += DrawHistoryItem;
            _historyEmpty = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft,
                Font = Ui(8.5f, FontStyle.Regular),
                ForeColor = Dim,
                Padding = new Padding(0, 6, 0, 0)
            };
            historyBody.Controls.Add(_historyList);
            historyBody.Controls.Add(_historyEmpty);
            historyLayout.Controls.Add(historyBody, 0, 1);

            historyCard.Controls.Add(historyLayout);
            root.Controls.Add(historyCard, 0, 3);

            return root;
        }

        private static Panel CreateStat(out Label caption, out Label value)
        {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Card, Margin = new Padding(0, 0, 8, 0) };
            caption = new Label
            {
                AutoSize = true,
                Font = Ui(8.5f, FontStyle.Regular),
                ForeColor = Muted,
                Location = new Point(14, 10)
            };
            value = new Label
            {
                AutoSize = true,
                Font = Ui(13f, FontStyle.Bold),
                ForeColor = TextColor,
                Location = new Point(14, 30)
            };
            panel.Controls.Add(caption);
            panel.Controls.Add(value);
            return panel;
        }

        private Panel BuildSettings()
        {
            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Background,
                AutoScroll = true,
                Padding = new Padding(20, 10, 20, 10)
            };

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                BackColor = Background
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            BuildLanguageSection(table);
            BuildAutoSection(table);
            BuildAreasSection(table);
            BuildExceptionsSection(table);
            BuildBehaviorSection(table);

            _capabilityNote = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(720, 0),
                Font = Ui(8.5f, FontStyle.Regular),
                ForeColor = Dim,
                Margin = new Padding(2, 4, 0, 20)
            };
            table.Controls.Add(_capabilityNote);

            scroll.Controls.Add(table);
            scroll.Resize += (sender, e) => table.Width = Math.Max(320, scroll.ClientSize.Width - 24);
            return scroll;
        }

        private void BuildLanguageSection(TableLayoutPanel parent)
        {
            var section = CreateSection(out _languageTitle);
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                Margin = new Padding(0, 6, 0, 0)
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _languageDesc = CreateCaption();
            _languageDesc.MaximumSize = new Size(700, 0);

            _languageCombo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0x10, 0x16, 0x13),
                ForeColor = TextColor,
                Width = 190,
                Margin = new Padding(0, 10, 0, 2)
            };
            _languageCombo.SelectedIndexChanged += OnLanguageChanged;

            body.Controls.Add(_languageDesc);
            body.Controls.Add(_languageCombo);
            section.Controls.Add(body);
            parent.Controls.Add(section);
        }

        private void BuildAutoSection(TableLayoutPanel parent)
        {
            var section = CreateSection(out _autoTitle);
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 2,
                Margin = new Padding(0, 8, 0, 0)
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _autoCheck = CreateCheck();
            _autoCheck.CheckedChanged += (sender, e) =>
            {
                if (_updating)
                {
                    return;
                }

                _settings.AutoEnabled = _autoCheck.Checked;
                SaveSettings();
            };
            body.Controls.Add(_autoCheck, 0, 0);
            body.SetColumnSpan(_autoCheck, 2);

            _intervalCheck = CreateCheck();
            _intervalCheck.CheckedChanged += (sender, e) =>
            {
                if (_updating)
                {
                    return;
                }

                _settings.UseInterval = _intervalCheck.Checked;
                SaveSettings();
            };
            _intervalValue = CreateNumber(1, 180);
            _intervalValue.ValueChanged += (sender, e) =>
            {
                if (_updating)
                {
                    return;
                }

                _settings.IntervalMinutes = (int)_intervalValue.Value;
                SaveSettings();
            };
            _intervalUnit = CreateUnit();

            body.Controls.Add(_intervalCheck, 0, 1);
            body.Controls.Add(CreateControlRow(_intervalValue, _intervalUnit), 1, 1);

            _thresholdCheck = CreateCheck();
            _thresholdCheck.CheckedChanged += (sender, e) =>
            {
                if (_updating)
                {
                    return;
                }

                _settings.UseThreshold = _thresholdCheck.Checked;
                SaveSettings();
            };
            _thresholdValue = CreateNumber(40, 99);
            _thresholdValue.ValueChanged += (sender, e) =>
            {
                if (_updating)
                {
                    return;
                }

                _settings.ThresholdPercent = (int)_thresholdValue.Value;
                _lastClean = DateTime.MinValue;
                SaveSettings();
            };
            _percentUnit = CreateUnit();

            body.Controls.Add(_thresholdCheck, 0, 2);
            body.Controls.Add(CreateControlRow(_thresholdValue, _percentUnit), 1, 2);

            _cooldownLabel = CreateRowLabel();
            _cooldownValue = CreateNumber(1, 120);
            _cooldownValue.ValueChanged += (sender, e) =>
            {
                if (_updating)
                {
                    return;
                }

                _settings.CooldownMinutes = (int)_cooldownValue.Value;
                SaveSettings();
            };
            _cooldownUnit = CreateUnit();

            body.Controls.Add(_cooldownLabel, 0, 3);
            body.Controls.Add(CreateControlRow(_cooldownValue, _cooldownUnit), 1, 3);

            _autoHint = CreateCaption();
            _autoHint.MaximumSize = new Size(700, 0);

            section.Controls.Add(body);
            section.Controls.Add(_autoHint);
            parent.Controls.Add(section);
        }

        private void BuildAreasSection(TableLayoutPanel parent)
        {
            var section = CreateSection(out _areasTitle);
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                Margin = new Padding(0, 8, 0, 0)
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddAreaRow(body, CleanAreas.WorkingSets, "areaWorkingSetsLabel", "areaWorkingSetsDesc");
            AddAreaRow(body, CleanAreas.Standby, "areaStandbyLabel", "areaStandbyDesc");
            AddAreaRow(body, CleanAreas.Modified, "areaModifiedLabel", "areaModifiedDesc");
            AddAreaRow(body, CleanAreas.FileCache, "areaFileCacheLabel", "areaFileCacheDesc");

            _areasNote = CreateCaption();
            _areasNote.MaximumSize = new Size(700, 0);
            _areasNote.Margin = new Padding(0, 8, 0, 0);

            section.Controls.Add(body);
            section.Controls.Add(_areasNote);
            parent.Controls.Add(section);
        }

        private void AddAreaRow(TableLayoutPanel table, CleanAreas area, string labelKey, string descKey)
        {
            var supported = MemoryEngine.SupportedAreas.Has(area);

            var check = CreateCheck();
            check.Enabled = supported;
            check.CheckedChanged += (sender, e) =>
            {
                if (_updating)
                {
                    return;
                }

                if (check.Checked)
                {
                    _settings.Areas |= area;
                }
                else
                {
                    _settings.Areas &= ~area;
                }

                SaveSettings();
            };

            var desc = CreateCaption();
            desc.Margin = new Padding(20, 1, 0, 10);
            desc.MaximumSize = new Size(680, 0);

            table.Controls.Add(check);
            table.Controls.Add(desc);
            _areaRows.Add(new AreaRow
            {
                Area = area,
                Check = check,
                Description = desc,
                LabelKey = labelKey,
                DescriptionKey = descKey,
                Supported = supported
            });
        }

        private void BuildExceptionsSection(TableLayoutPanel parent)
        {
            var section = CreateSection(out _exceptionsTitle);
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                Margin = new Padding(0, 8, 0, 0)
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _exceptionsDesc = CreateCaption();
            _exceptionsDesc.MaximumSize = new Size(700, 0);

            _exceptionsBox = new TextBox
            {
                Multiline = true,
                Height = 62,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Color.FromArgb(0x10, 0x16, 0x13),
                ForeColor = TextColor,
                BorderStyle = BorderStyle.FixedSingle,
                Font = Ui(9f, FontStyle.Regular),
                Margin = new Padding(0, 8, 0, 0)
            };
            _exceptionsBox.TextChanged += (sender, e) =>
            {
                if (_updating)
                {
                    return;
                }

                _settings.Excluded = _exceptionsBox.Text;
                SaveSettings();
            };

            _exceptionsHint = CreateCaption();
            _exceptionsHint.MaximumSize = new Size(700, 0);

            body.Controls.Add(_exceptionsDesc);
            body.Controls.Add(_exceptionsBox);
            body.Controls.Add(_exceptionsHint);
            section.Controls.Add(body);
            parent.Controls.Add(section);
        }

        private void BuildBehaviorSection(TableLayoutPanel parent)
        {
            var section = CreateSection(out _behaviorTitle);
            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                Margin = new Padding(0, 8, 0, 0)
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            _trayCheck = CreateCheck();
            _trayCheck.CheckedChanged += (sender, e) =>
            {
                if (_updating)
                {
                    return;
                }

                _settings.MinimizeToTray = _trayCheck.Checked;
                SaveSettings();
            };
            _trayDesc = CreateCaption();
            _trayDesc.Margin = new Padding(20, 1, 0, 10);
            _trayDesc.MaximumSize = new Size(680, 0);

            _notifyCheck = CreateCheck();
            _notifyCheck.CheckedChanged += (sender, e) =>
            {
                if (_updating)
                {
                    return;
                }

                _settings.NotifyOnClean = _notifyCheck.Checked;
                SaveSettings();
            };
            _notifyDesc = CreateCaption();
            _notifyDesc.Margin = new Padding(20, 1, 0, 10);
            _notifyDesc.MaximumSize = new Size(680, 0);

            _startupCheck = CreateCheck();
            _startupCheck.CheckedChanged += OnStartupChanged;
            _startupDesc = CreateCaption();
            _startupDesc.Margin = new Padding(20, 1, 0, 4);
            _startupDesc.MaximumSize = new Size(680, 0);

            _startupMessage = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(680, 0),
                Font = Ui(8.5f, FontStyle.Regular),
                ForeColor = Warning,
                Margin = new Padding(20, 0, 0, 8),
                Visible = false
            };

            body.Controls.Add(_trayCheck);
            body.Controls.Add(_trayDesc);
            body.Controls.Add(_notifyCheck);
            body.Controls.Add(_notifyDesc);
            body.Controls.Add(_startupCheck);
            body.Controls.Add(_startupDesc);
            body.Controls.Add(_startupMessage);
            section.Controls.Add(body);
            parent.Controls.Add(section);
        }

        private Panel CreateSection(out Label title)
        {
            var section = new Panel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Card,
                Padding = new Padding(20, 14, 20, 16),
                Margin = new Padding(0, 0, 0, 12)
            };

            title = new Label
            {
                AutoSize = true,
                Font = Ui(10.5f, FontStyle.Bold),
                ForeColor = TextColor,
                Dock = DockStyle.Top
            };

            section.Controls.Add(title);
            return section;
        }

        private static CheckBox CreateCheck()
        {
            return new CheckBox
            {
                AutoSize = true,
                Font = Ui(9f, FontStyle.Regular),
                ForeColor = TextColor,
                Margin = new Padding(0, 3, 0, 3)
            };
        }

        private static Label CreateRowLabel()
        {
            return new Label
            {
                AutoSize = true,
                Font = Ui(9f, FontStyle.Regular),
                ForeColor = Muted,
                Margin = new Padding(0, 6, 10, 3)
            };
        }

        private static Label CreateCaption()
        {
            return new Label
            {
                AutoSize = true,
                Font = Ui(8.5f, FontStyle.Regular),
                ForeColor = Muted,
                Margin = new Padding(0, 4, 0, 2)
            };
        }

        private static Label CreateUnit()
        {
            return new Label
            {
                AutoSize = true,
                Font = Ui(8.5f, FontStyle.Regular),
                ForeColor = Dim,
                Margin = new Padding(6, 7, 0, 0)
            };
        }

        private static NumericUpDown CreateNumber(int minimum, int maximum)
        {
            return new NumericUpDown
            {
                Minimum = minimum,
                Maximum = maximum,
                Width = 70,
                BackColor = Color.FromArgb(0x10, 0x16, 0x13),
                ForeColor = TextColor,
                BorderStyle = BorderStyle.FixedSingle,
                Font = Ui(9f, FontStyle.Regular),
                Margin = new Padding(0, 1, 0, 1)
            };
        }

        private static FlowLayoutPanel CreateControlRow(NumericUpDown number, Label unit)
        {
            var row = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Margin = new Padding(0, 2, 0, 2)
            };
            row.Controls.Add(number);
            row.Controls.Add(unit);
            return row;
        }

        private static Image LoadLogo()
        {
            try
            {
                var icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                return icon == null ? null : icon.ToBitmap();
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void CreateTray()
        {
            _tray = new NotifyIcon
            {
                Visible = true,
                Text = "memcleaner",
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
            };

            var menu = new ContextMenuStrip();
            _trayOpenItem = new ToolStripMenuItem();
            _trayOpenItem.Click += (sender, e) => ShowFromTray();
            _trayExitItem = new ToolStripMenuItem();
            _trayExitItem.Click += (sender, e) => CloseApplication();
            menu.Items.Add(_trayOpenItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_trayExitItem);

            _tray.ContextMenuStrip = menu;
            _tray.DoubleClick += (sender, e) => ShowFromTray();
        }

        private void CloseApplication()
        {
            _exitRequested = true;

            if (_tray != null)
            {
                _tray.Visible = false;
            }

            Close();
        }

        private void StartActivationListener()
        {
            _activationThread = new Thread(delegate()
            {
                while (!_exitRequested)
                {
                    try
                    {
                        if (!_activateEvent.WaitOne(500))
                        {
                            continue;
                        }

                        if (_exitRequested || IsDisposed)
                        {
                            break;
                        }

                        BeginInvoke(new Action(ShowFromTray));
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
            });

            _activationThread.IsBackground = true;
            _activationThread.Start();
        }

        private void ShowPage(bool settings)
        {
            _overviewPanel.Visible = !settings;
            _settingsPanel.Visible = settings;

            _navOverview.BackColor = settings ? Card : AccentSoft;
            _navOverview.ForeColor = settings ? Muted : Accent;
            _navSettings.BackColor = settings ? AccentSoft : Card;
            _navSettings.ForeColor = settings ? Accent : Muted;
        }

        private void ApplyTexts()
        {
            _taglineLabel.Text = L.T("appTagline");
            _versionLabel.Text = L.F("versionLabel", GetVersion()) + " · lordofsunshine";
            _navOverview.Text = L.T("navOverview");
            _navSettings.Text = L.T("navSettings");

            _percentCaption.Text = L.T("metricUsed");
            _usedCaption.Text = L.T("metricUsed");
            _freeCaption.Text = L.T("metricFree");
            _cacheCaption.Text = L.T("metricFileCache");
            _commitCaption.Text = L.T("metricAllocated");

            _cleanButton.Text = L.T(_busy ? "cleanButtonBusy" : "cleanButton");
            _historyTitle.Text = L.T("historyTitle");
            _historyCaption.Text = L.T("historyCaption");
            _historyEmpty.Text = L.T("historyEmpty");

            _languageTitle.Text = L.T("sectionLanguage");
            _languageDesc.Text = L.T("languageDesc");

            _autoTitle.Text = L.T("sectionAuto");
            _autoCheck.Text = L.T("autoEnabled");
            _intervalCheck.Text = L.T("autoInterval");
            _thresholdCheck.Text = L.T("autoThreshold");
            _cooldownLabel.Text = L.T("autoCooldown");
            _intervalUnit.Text = L.T("unitMinShort");
            _cooldownUnit.Text = L.T("unitMinShort");
            _percentUnit.Text = "%";
            _autoHint.Text = L.T("autoHint");

            _areasTitle.Text = L.T("sectionAreas");

            foreach (var row in _areaRows)
            {
                row.Check.Text = L.T(row.LabelKey) + (row.Supported ? string.Empty : " · " + L.T("unavailable"));
                row.Description.Text = L.T(row.DescriptionKey);
            }

            _areasNote.Text = MemoryEngine.SupportedAreas == (CleanAreas.WorkingSets | CleanAreas.Standby | CleanAreas.Modified | CleanAreas.FileCache)
                ? string.Empty
                : L.T("areasUnavailable");

            _exceptionsTitle.Text = L.T("sectionExceptions");
            _exceptionsDesc.Text = L.T("exceptionsDesc");
            _exceptionsHint.Text = L.T("exceptionsHint");

            _behaviorTitle.Text = L.T("sectionBehavior");
            _trayCheck.Text = L.T("trayTitle");
            _trayDesc.Text = L.T("trayDesc");
            _notifyCheck.Text = L.T("notificationsTitle");
            _notifyDesc.Text = L.T("notificationsDesc");
            _startupCheck.Text = L.T("startupTitle");
            _startupDesc.Text = L.T("startupDesc");

            _capabilityNote.Text = L.T("capabilityNote");

            _trayOpenItem.Text = L.T("trayOpen");
            _trayExitItem.Text = L.T("trayExit");

            RebuildLanguageItems();
        }

        private void RebuildLanguageItems()
        {
            var previous = _updating;
            _updating = true;

            _languages.Clear();
            _languages.Add(new LanguageOption("system", L.T("languageSystem")));
            _languages.Add(new LanguageOption("en", L.T("languageEnglish")));
            _languages.Add(new LanguageOption("ru", L.T("languageRussian")));

            _languageCombo.Items.Clear();

            foreach (var option in _languages)
            {
                _languageCombo.Items.Add(option);
            }

            _languageCombo.SelectedIndex = Math.Max(0, _languages.FindIndex(option => option.Setting == _settings.Language));
            _updating = previous;
        }

        private void ApplySettingsToUi()
        {
            _updating = true;

            _autoCheck.Checked = _settings.AutoEnabled;
            _intervalCheck.Checked = _settings.UseInterval;
            _thresholdCheck.Checked = _settings.UseThreshold;
            _intervalValue.Value = _settings.IntervalMinutes;
            _thresholdValue.Value = _settings.ThresholdPercent;
            _cooldownValue.Value = _settings.CooldownMinutes;
            _exceptionsBox.Text = _settings.Excluded;
            _trayCheck.Checked = _settings.MinimizeToTray;
            _notifyCheck.Checked = _settings.NotifyOnClean;
            _startupCheck.Checked = _settings.StartWithWindows;

            foreach (var row in _areaRows)
            {
                row.Check.Checked = _settings.Areas.Has(row.Area);
            }

            _saveStatus = null;
            _updating = false;
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (_updating)
            {
                return;
            }

            var option = _languageCombo.SelectedItem as LanguageOption;

            if (option == null || option.Setting == _settings.Language)
            {
                return;
            }

            _settings.Language = option.Setting;
            L.Apply(option.Setting);
            SaveSettings();
            ApplySettingsToUi();
            ApplyTexts();
            ApplyValues();
            RebuildHistory();
        }

        private void OnStartupChanged(object sender, EventArgs e)
        {
            if (_updating)
            {
                return;
            }

            _settings.StartWithWindows = _startupCheck.Checked;
            var wanted = _settings.StartWithWindows;
            SaveSettings();

            _startupMessage.Text = L.T("autostartWorking");
            _startupMessage.ForeColor = Muted;
            _startupMessage.Visible = true;

            ThreadPool.QueueUserWorkItem(delegate
            {
                var message = Autostart.Apply(wanted, Application.ExecutablePath);

                try
                {
                    BeginInvoke(new Action(delegate
                    {
                        if (message.Length > 0)
                        {
                            _settings.StartWithWindows = !wanted;
                            _updating = true;
                            _startupCheck.Checked = _settings.StartWithWindows;
                            _updating = false;
                            _startupMessage.ForeColor = Warning;
                            _startupMessage.Text = message;
                            _startupMessage.Visible = true;
                            SaveSettings();
                        }
                        else
                        {
                            _startupMessage.Visible = false;
                        }
                    }));
                }
                catch (Exception)
                {
                }
            });
        }

        private void SaveSettings()
        {
            var saved = _settings.Save();

            if (_saveStatus != null)
            {
                if (_settings.Areas == CleanAreas.None)
                {
                    _saveStatus.Text = L.T("validationAreas");
                    _saveStatus.ForeColor = Warning;
                }
                else if (!saved)
                {
                    _saveStatus.Text = L.T("saveFailed");
                    _saveStatus.ForeColor = Warning;
                }
                else
                {
                    _saveStatus.Text = L.T("saveStatus");
                    _saveStatus.ForeColor = Dim;
                }
            }

        }

        private void RefreshValues(bool immediate)
        {
            try
            {
                var snapshot = MemoryEngine.Read();
                UpdateTooltip(snapshot);

                if (!_visible)
                {
                    return;
                }

                _totalBytes = snapshot.TotalBytes;
                _commitLimit = snapshot.CommitLimitBytes;

                if (immediate)
                {
                    _used.Jump(snapshot.TotalBytes - snapshot.FreeBytes);
                    _free.Jump(snapshot.FreeBytes);
                    _commit.Jump(snapshot.CommitBytes);
                    _cache.Jump(snapshot.CacheBytes);
                    _percent.Jump(snapshot.UsedPercent);
                    ApplyValues();
                }
                else
                {
                    _used.To(snapshot.TotalBytes - snapshot.FreeBytes);
                    _free.To(snapshot.FreeBytes);
                    _commit.To(snapshot.CommitBytes);
                    _cache.To(snapshot.CacheBytes);
                    _percent.To(snapshot.UsedPercent);
                    _animationTimer.Start();
                }
            }
            catch (Exception)
            {
            }
        }

        private void StepAnimation()
        {
            var active = _used.Step();
            active |= _free.Step();
            active |= _commit.Step();
            active |= _cache.Step();
            active |= _percent.Step();

            ApplyValues();

            if (!active)
            {
                _animationTimer.Stop();
            }
        }

        private void ApplyValues()
        {
            _percentLabel.Text = Humanize.Percent(_percent.Value);
            _bar.Value = _percent.Value;
            _usedValue.Text = Humanize.Bytes(_used.Value);
            _freeValue.Text = Humanize.Bytes(_free.Value);
            _cacheValue.Text = Platform.SupportsFileCache ? Humanize.Bytes(_cache.Value) : L.T("metricNone");
            _commitValue.Text = Humanize.Bytes(_commit.Value);
            _totalLabel.Text = L.F("totalOf", Humanize.Bytes(_totalBytes));
        }

        private void UpdateTooltip(MemorySnapshot snapshot)
        {
            if (_tray == null)
            {
                return;
            }

            var text = "memcleaner · " + Humanize.Percent(snapshot.UsedPercent) + " · " + Humanize.Bytes(snapshot.FreeBytes);
            _tray.Text = text.Length > 62 ? text.Substring(0, 62) : text;
        }

        private void BeginClean(string trigger)
        {
            if (_busy)
            {
                return;
            }

            _busy = true;
            _cleanButton.Enabled = false;
            _cleanButton.Text = L.T("cleanButtonBusy");
            _resultLabel.ForeColor = Muted;
            _resultLabel.Text = L.T("resultCleaning");

            var areas = _settings.Areas;
            var excluded = _settings.Excluded;

            ThreadPool.QueueUserWorkItem(delegate
            {
                CleanOutcome outcome;

                try
                {
                    outcome = MemoryEngine.Clean(areas, excluded);
                }
                catch (Exception error)
                {
                    outcome = new CleanOutcome();
                    outcome.Errors.Add(error.Message);
                }

                try
                {
                    BeginInvoke(new Action(delegate { FinishClean(trigger, outcome); }));
                }
                catch (Exception)
                {
                }
            });
        }

        private void FinishClean(string trigger, CleanOutcome outcome)
        {
            _busy = false;
            _cleanButton.Enabled = true;
            _cleanButton.Text = L.T("cleanButton");
            _lastClean = DateTime.Now;
            _thresholdArmed = false;

            var meaningful = outcome.FreedBytes >= 1024 * 1024;

            if (outcome.Errors.Count > 0)
            {
                _resultLabel.ForeColor = Warning;

                if (outcome.Errors.Count > 1)
                {
                    _resultLabel.Text = outcome.Errors[0] + L.F("resultPartialMore", outcome.Errors.Count - 1);
                }
                else
                {
                    _resultLabel.Text = outcome.Errors[0];
                }
            }
            else if (!meaningful)
            {
                _resultLabel.ForeColor = Muted;
                _resultLabel.Text = L.T("resultNothing");
            }
            else
            {
                _resultLabel.ForeColor = Accent;
                _resultLabel.Text = L.F("resultFreed", Humanize.Bytes(outcome.FreedBytes), outcome.Duration.TotalSeconds.ToString("0.0", L.Culture));
            }

            if (meaningful)
            {
                var entry = new HistoryEntry
                {
                    Time = DateTime.Now,
                    Freed = outcome.FreedBytes,
                    Trigger = trigger
                };

                HistoryStore.Append(entry);
                _history.Insert(0, entry);

                while (_history.Count > 6)
                {
                    _history.RemoveAt(_history.Count - 1);
                }

                RebuildHistory();
            }

            if (trigger == "auto" && _settings.NotifyOnClean && outcome.FreedBytes >= 32UL * 1024 * 1024 && _tray != null)
            {
                _tray.ShowBalloonTip(3500, "memcleaner", L.F("notifyFreed", Humanize.Bytes(outcome.FreedBytes)), ToolTipIcon.Info);
            }

            RefreshValues(false);
        }

        private void CheckAutoClean()
        {
            if (_busy || !_settings.AutoEnabled)
            {
                return;
            }

            if (!_settings.UseInterval && !_settings.UseThreshold)
            {
                return;
            }

            var sinceLast = _lastClean == DateTime.MinValue
                ? double.MaxValue
                : (DateTime.Now - _lastClean).TotalMinutes;

            if (sinceLast < _settings.CooldownMinutes)
            {
                return;
            }

            var intervalDue = _settings.UseInterval && sinceLast >= _settings.IntervalMinutes;

            if (intervalDue)
            {
                BeginClean("auto");
                return;
            }

            if (!_settings.UseThreshold)
            {
                return;
            }

            try
            {
                var snapshot = MemoryEngine.Read();

                if (!_thresholdArmed && snapshot.UsedPercent <= _settings.ThresholdPercent - 5)
                {
                    _thresholdArmed = true;
                }

                if (_thresholdArmed && snapshot.UsedPercent >= _settings.ThresholdPercent)
                {
                    _thresholdArmed = false;
                    BeginClean("auto");
                }
            }
            catch (Exception)
            {
            }
        }

        private void RebuildHistory()
        {
            var previous = _updating;
            _updating = true;
            _historyList.Items.Clear();

            foreach (var entry in _history)
            {
                var row = new HistoryRow
                {
                    Time = entry.Time.ToString(L.Code == "ru" ? "HH:mm dd.MM" : "MMM d, HH:mm", L.Culture),
                    Trigger = L.T(entry.Trigger == "auto" ? "triggerAuto" : "triggerManual"),
                    Freed = entry.Freed >= 1024UL * 1024 ? Humanize.Bytes(entry.Freed) : L.T("historyLess")
                };
                _historyList.Items.Add(row);
            }

            _historyEmpty.Visible = _history.Count == 0;
            _historyList.Visible = _history.Count > 0;
            _updating = previous;
        }

        private void DrawHistoryItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0)
            {
                return;
            }

            var row = _historyList.Items[e.Index] as HistoryRow;

            if (row == null)
            {
                return;
            }

            using (var background = new SolidBrush(Card))
            {
                e.Graphics.FillRectangle(background, e.Bounds);
            }

            var middle = e.Bounds.Top + (e.Bounds.Height - Font.Height) / 2;
            var width = e.Bounds.Width;

            TextRenderer.DrawText(
                e.Graphics,
                row.Time,
                Ui(8.5f, FontStyle.Regular),
                new Rectangle(e.Bounds.Left + 2, middle, 110, e.Bounds.Height),
                Dim,
                TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter);

            TextRenderer.DrawText(
                e.Graphics,
                row.Trigger,
                Ui(9f, FontStyle.Regular),
                new Rectangle(e.Bounds.Left + 118, middle, Math.Max(80, width - 240), e.Bounds.Height),
                Muted,
                TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            TextRenderer.DrawText(
                e.Graphics,
                row.Freed,
                Ui(9f, FontStyle.Bold),
                new Rectangle(e.Bounds.Right - 110, middle, 108, e.Bounds.Height),
                TextColor,
                TextFormatFlags.NoPadding | TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
        }

        private static string GetVersion()
        {
            var version = Application.ProductVersion;
            var parts = version.Split('.');

            if (parts.Length >= 3)
            {
                return parts[0] + "." + parts[1] + "." + parts[2];
            }

            return version;
        }

        private sealed class AreaRow
        {
            public CleanAreas Area;
            public CheckBox Check;
            public Label Description;
            public string LabelKey;
            public string DescriptionKey;
            public bool Supported;
        }

        private sealed class HistoryRow
        {
            public string Time;
            public string Trigger;
            public string Freed;

            public override string ToString()
            {
                return Time + "  " + Trigger + "  " + Freed;
            }
        }

        private sealed class LanguageOption
        {
            public LanguageOption(string setting, string text)
            {
                Setting = setting;
                Text = text;
            }

            public string Setting;
            public string Text;

            public override string ToString()
            {
                return Text;
            }
        }
    }
}
