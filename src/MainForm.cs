using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace YodaFy
{
    public sealed class MainForm : Form
    {
        static readonly Color Background = Color.FromArgb(17, 22, 20);
        static readonly Color Card = Color.FromArgb(27, 34, 30);
        static readonly Color Field = Color.FromArgb(20, 27, 23);
        static readonly Color Ink = Color.FromArgb(235, 241, 229);
        static readonly Color Muted = Color.FromArgb(153, 172, 157);
        static readonly Color Accent = Color.FromArgb(186, 226, 125);
        readonly Settings settings = Settings.Load();
        readonly TextBox source = new TextBox();
        readonly TextBox result = new TextBox();
        readonly TextBox browserTitles = new TextBox();
        readonly CheckedListBox apps = new CheckedListBox();
        readonly CheckBox browserEnabled = new CheckBox();
        readonly NumericUpDown delay = new NumericUpDown();
        readonly Button toggle = new Button();
        readonly Label status = new Label();
        readonly Label mode = new Label();
        readonly Label feedback = new Label();
        readonly NotifyIcon tray = new NotifyIcon();
        readonly ToolStripMenuItem trayToggle = new ToolStripMenuItem();
        readonly Timer previewTimer = new Timer();
        string previewInput;
        DraftMonitor monitor;
        bool loading = true;
        bool quitting;
        bool hotkeyAvailable;

        public MainForm()
        {
            Text = "YodaFy - a wiser way to message";
            ClientSize = new Size(1060, 810);
            MinimumSize = new Size(900, 650);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Background;
            ForeColor = Ink;
            Font = new Font("Segoe UI", 10F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Icon = SystemIcons.Application;
            BuildLayout();
            BuildTray();
            loading = false;
            RefreshMode();
            source.Text = "I am ready to help you. We will finish the project tomorrow.";
            previewTimer.Interval = 250;
            previewTimer.Tick += delegate { RefreshPreview(); };
            previewTimer.Start();
            monitor = new DraftMonitor(settings);
            monitor.StatusChanged += delegate(string message)
            {
                if (!IsDisposed && IsHandleCreated)
                    try { BeginInvoke((MethodInvoker)delegate { status.Text = message; }); } catch (InvalidOperationException) { }
            };
            FormClosing += HandleClosing;
            Shown += delegate
            {
                Rectangle area = Screen.FromControl(this).WorkingArea;
                MinimumSize = new Size(Math.Min(900, area.Width), Math.Min(650, area.Height));
                Size = new Size(Math.Min(Width, area.Width - 12), Math.Min(Height, area.Height - 12));
                Location = new Point(area.Left + (area.Width - Width) / 2, area.Top + (area.Height - Height) / 2);
                source.Select(source.Text.Length, 0); ActiveControl = toggle;
            };
        }

        void BuildLayout()
        {
            Panel viewport = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            TableLayoutPanel shell = new TableLayoutPanel { Dock = DockStyle.Fill, MinimumSize = new Size(900, 750), Padding = new Padding(30, 20, 30, 18), ColumnCount = 1, RowCount = 4 };
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            viewport.Controls.Add(shell);
            Controls.Add(viewport);

            TableLayoutPanel header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
            Label logo = LabelFor("YodaFy", 25, Accent); logo.Font = new Font("Segoe UI Semibold", 25F); logo.Dock = DockStyle.Fill;
            header.Controls.Add(logo, 0, 0);
            Label badge = LabelFor("DESKTOP  /  WINDOWS", 9, Muted); badge.TextAlign = ContentAlignment.MiddleRight; badge.Dock = DockStyle.Fill;
            header.Controls.Add(badge, 1, 0);
            shell.Controls.Add(header, 0, 0);

            Panel hero = new Panel { Dock = DockStyle.Fill };
            Label headline = LabelFor("A little wiser, your messages become.", 25, Ink);
            headline.Font = new Font("Segoe UI Semibold", 25F); headline.Dock = DockStyle.None;
            headline.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            headline.Location = new Point(0, 10); headline.Size = new Size(960, 45);
            Label subhead = LabelFor("Type naturally. Pause for a moment. Let the Force handle the word order.", 11, Muted);
            subhead.Dock = DockStyle.None; subhead.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            subhead.Location = new Point(2, 64); subhead.Size = new Size(950, 30);
            hero.Controls.Add(headline); hero.Controls.Add(subhead); shell.Controls.Add(hero, 0, 1);

            TableLayoutPanel columns = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54));
            columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));
            columns.Controls.Add(PreviewCard(), 0, 0);
            columns.Controls.Add(AutomationCard(), 1, 0);
            shell.Controls.Add(columns, 0, 2);
            Label footer = LabelFor("OFFLINE BY DESIGN     No accounts. No API keys. No message history saved.", 9, Muted);
            footer.Dock = DockStyle.Fill; footer.TextAlign = ContentAlignment.BottomLeft;
            shell.Controls.Add(footer, 0, 3);
        }

        Control PreviewCard()
        {
            TableLayoutPanel card = CardLayout(7);
            card.Margin = new Padding(0, 0, 14, 0);
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            card.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            card.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            card.Controls.Add(LabelFor("Try the transformation", 15, Ink), 0, 0);
            card.Controls.Add(LabelFor("YOUR WORDS", 9, Muted), 0, 1);
            StyleText(source, false); source.TextChanged += delegate { RefreshPreview(); };
            source.MaxLength = 8000; card.Controls.Add(source, 0, 2);
            Label outputCaption = LabelFor("A WISER VERSION", 9, Accent); outputCaption.TextAlign = ContentAlignment.BottomLeft; outputCaption.Padding = new Padding(0, 0, 0, 8);
            card.Controls.Add(outputCaption, 0, 3);
            StyleText(result, true); result.ForeColor = Accent; card.Controls.Add(result, 0, 4);
            Button copy = ButtonFor("Copy Yoda text", true); copy.Margin = new Padding(0, 8, 0, 0);
            copy.Click += delegate { try { if (result.TextLength > 0) { Clipboard.SetText(result.Text); feedback.Text = "Copied. Paste it into your conversation."; } } catch { feedback.Text = "Clipboard is busy. Please try again."; } };
            card.Controls.Add(copy, 0, 5);
            feedback.Text = "Rule-based English conversion. Names, links and code stay intact; review the result before sending.";
            feedback.ForeColor = Muted; feedback.Dock = DockStyle.Fill; feedback.Padding = new Padding(0, 12, 0, 0); feedback.Font = new Font("Segoe UI", 9F);
            card.Controls.Add(feedback, 0, 6);
            return card;
        }

        Control AutomationCard()
        {
            TableLayoutPanel card = CardLayout(11);
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
            card.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            card.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            mode.Font = new Font("Segoe UI Semibold", 14F); mode.Dock = DockStyle.Fill; mode.ForeColor = Accent;
            card.Controls.Add(mode, 0, 0);
            toggle.Dock = DockStyle.Fill; StyleButton(toggle, true); toggle.Click += delegate { settings.AutomationEnabled = !settings.AutomationEnabled; Save(); RefreshMode(); };
            card.Controls.Add(toggle, 0, 1);
            status.ForeColor = Muted; status.Font = new Font("Segoe UI", 9F); status.Dock = DockStyle.Fill; status.Padding = new Padding(0, 9, 0, 0);
            status.Text = "Ready. Enable automatic mode when you want to convert drafts.";
            card.Controls.Add(status, 0, 2);
            TableLayoutPanel appHeading = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, Margin = new Padding(0) };
            appHeading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            appHeading.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 76));
            appHeading.Controls.Add(LabelFor("SELECT MESSAGING APPS", 9, Muted), 0, 0);
            Button addApp = ButtonFor("Add app", false); addApp.Font = new Font("Segoe UI", 8F); addApp.Margin = new Padding(0);
            addApp.Click += delegate { AddApplication(); };
            appHeading.Controls.Add(addApp, 1, 0); card.Controls.Add(appHeading, 0, 3);
            apps.Dock = DockStyle.Fill; apps.BackColor = Field; apps.ForeColor = Ink; apps.BorderStyle = BorderStyle.None;
            apps.CheckOnClick = true; apps.IntegralHeight = false; apps.Font = new Font("Segoe UI", 10F);
            apps.MultiColumn = true; apps.ColumnWidth = 160;
            string[] names = { "Discord", "WhatsApp", "Telegram", "slack", "ms-teams", "Teams", "Signal" };
            foreach (string name in names.Concat(settings.Processes).Distinct(StringComparer.OrdinalIgnoreCase)) apps.Items.Add(name, settings.Processes.Contains(name, StringComparer.OrdinalIgnoreCase));
            apps.ItemCheck += delegate { if (!loading && IsHandleCreated) BeginInvoke((MethodInvoker)delegate { settings.Processes = apps.CheckedItems.Cast<string>().ToArray(); Save(); }); };
            card.Controls.Add(apps, 0, 4);
            browserEnabled.Text = "Also allow messaging tabs in browsers"; browserEnabled.Checked = settings.BrowserEnabled;
            browserEnabled.Dock = DockStyle.Fill; browserEnabled.Font = new Font("Segoe UI", 9F); browserEnabled.CheckedChanged += delegate { settings.BrowserEnabled = browserEnabled.Checked; browserTitles.Enabled = browserEnabled.Checked; Save(); };
            card.Controls.Add(browserEnabled, 0, 5);
            browserTitles.Text = String.Join("; ", settings.BrowserTitles); browserTitles.BackColor = Field; browserTitles.ForeColor = Ink;
            browserTitles.BorderStyle = BorderStyle.FixedSingle; browserTitles.Dock = DockStyle.Fill; browserTitles.Font = new Font("Segoe UI", 9F);
            browserTitles.Enabled = settings.BrowserEnabled; browserTitles.Leave += delegate { settings.BrowserTitles = browserTitles.Text.Split(';').Select(delegate(string s) { return s.Trim(); }).Where(delegate(string s) { return s.Length > 0; }).ToArray(); Save(); };
            card.Controls.Add(browserTitles, 0, 6);
            card.Controls.Add(LabelFor("Browser tab title matches, separated by semicolons.", 8, Muted), 0, 7);
            FlowLayoutPanel delayRow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false, Margin = new Padding(0, 5, 0, 0) };
            Label delayLabel = LabelFor("Typing pause (seconds)", 9, Muted); delayLabel.Width = 166; delayLabel.Height = 28;
            delay.Minimum = 1; delay.Maximum = 10; delay.DecimalPlaces = 1; delay.Increment = 0.2M; delay.Value = settings.IdleMilliseconds / 1000M;
            delay.Width = 72; delay.BackColor = Field; delay.ForeColor = Ink;
            delay.ValueChanged += delegate { settings.IdleMilliseconds = (int)(delay.Value * 1000); Save(); };
            delayRow.Controls.Add(delayLabel); delayRow.Controls.Add(delay); card.Controls.Add(delayRow, 0, 8);
            Button clipboard = ButtonFor("Convert clipboard  /  Ctrl + Alt + Y", false);
            clipboard.Click += delegate { ConvertClipboard(); }; card.Controls.Add(clipboard, 0, 9);
            Label note = LabelFor("Drafts only. You press Send. Unsupported composers? Copy, convert clipboard, then paste.", 8.5F, Muted);
            note.Padding = new Padding(0, 7, 0, 0); note.Dock = DockStyle.Fill; card.Controls.Add(note, 0, 10);
            return card;
        }

        TableLayoutPanel CardLayout(int rows)
        {
            return new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = rows, BackColor = Card, Padding = new Padding(22), Margin = new Padding(0) };
        }

        static Label LabelFor(string text, float size, Color color)
        {
            return new Label { Text = text, ForeColor = color, Font = new Font("Segoe UI", size), Dock = DockStyle.Fill, AutoSize = false };
        }

        static void StyleText(TextBox text, bool readOnly)
        {
            text.Multiline = true; text.ReadOnly = readOnly; text.Dock = DockStyle.Fill;
            text.BackColor = Field; text.ForeColor = Ink; text.BorderStyle = BorderStyle.FixedSingle;
            text.Font = new Font("Segoe UI", 12F); text.ScrollBars = ScrollBars.Vertical; text.Margin = new Padding(0);
            text.AcceptsReturn = true; text.ShortcutsEnabled = true;
        }

        static Button ButtonFor(string text, bool primary)
        {
            Button button = new Button { Text = text, Dock = DockStyle.Fill, Cursor = Cursors.Hand, Margin = new Padding(0, 3, 0, 3) };
            StyleButton(button, primary); return button;
        }

        static void StyleButton(Button button, bool primary)
        {
            button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(74, 93, 71);
            button.BackColor = primary ? Accent : Card; button.ForeColor = primary ? Background : Ink;
            button.Font = new Font("Segoe UI Semibold", 10F); button.Cursor = Cursors.Hand;
        }

        void RefreshPreview()
        {
            string input = source.Text;
            if (input == previewInput) return;
            previewInput = input;
            result.Text = YodaTransformer.Transform(input);
        }

        void AddApplication()
        {
            using (Form dialog = new Form { Text = "Allow another messaging app", ClientSize = new Size(420, 190), FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent, MaximizeBox = false, MinimizeBox = false, BackColor = Card, ForeColor = Ink, Font = Font })
            {
                Label help = LabelFor("Enter its process name from Task Manager, without .exe. Only labeled message composers are converted.", 10, Muted);
                help.Dock = DockStyle.None; help.Location = new Point(20, 18); help.Size = new Size(380, 58);
                TextBox process = new TextBox { Location = new Point(20, 86), Size = new Size(380, 30), BackColor = Field, ForeColor = Ink };
                Button add = ButtonFor("Allow app", true); add.Dock = DockStyle.None; add.Location = new Point(260, 136); add.Size = new Size(140, 34);
                add.Click += delegate
                {
                    string name = process.Text.Trim();
                    if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) name = name.Substring(0, name.Length - 4);
                    if (String.IsNullOrWhiteSpace(name) || name.Length > 100 || name.IndexOfAny(new char[] { '/', '\\', ':', '*', '?' }) >= 0)
                    { help.Text = "Enter one process name, such as Messenger or Zoom. Folder paths and wildcards are not supported."; return; }
                    int index = -1;
                    for (int i = 0; i < apps.Items.Count; i++) if (String.Equals((string)apps.Items[i], name, StringComparison.OrdinalIgnoreCase)) index = i;
                    if (index < 0) apps.Items.Add(name, true); else apps.SetItemChecked(index, true);
                    dialog.Close();
                };
                dialog.AcceptButton = add; dialog.Controls.Add(help); dialog.Controls.Add(process); dialog.Controls.Add(add); dialog.ShowDialog(this);
            }
        }

        void Save()
        {
            if (loading) return;
            try { settings.Save(); }
            catch (IOException) { status.Text = "Settings could not be saved. This session still works."; }
            catch (UnauthorizedAccessException) { status.Text = "Settings could not be saved. This session still works."; }
            if (monitor != null) monitor.Update(settings);
        }

        void RefreshMode()
        {
            mode.Text = settings.AutomationEnabled ? "Automatic mode is on" : "Your words, on your terms";
            toggle.Text = settings.AutomationEnabled ? "Pause automatic conversion" : "Enable automatic conversion";
            trayToggle.Text = settings.AutomationEnabled ? "Pause automatic conversion" : "Enable automatic conversion";
            tray.Text = settings.AutomationEnabled ? "YodaFy - automatic mode on" : "YodaFy - paused";
        }

        void BuildTray()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Open YodaFy", null, delegate { ShowWindow(); });
            trayToggle.Click += delegate { settings.AutomationEnabled = !settings.AutomationEnabled; Save(); RefreshMode(); };
            menu.Items.Add(trayToggle);
            menu.Items.Add("Convert clipboard", null, delegate { ConvertClipboard(); });
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Quit", null, delegate { quitting = true; Close(); });
            tray.Icon = SystemIcons.Application; tray.ContextMenuStrip = menu; tray.Visible = true;
            tray.DoubleClick += delegate { ShowWindow(); };
        }

        void ShowWindow() { Show(); WindowState = FormWindowState.Normal; Activate(); }

        void ConvertClipboard()
        {
            try
            {
                if (!Clipboard.ContainsText()) { status.Text = "Copy some text first, then convert your clipboard."; return; }
                string original = Clipboard.GetText();
                if (original.Length > 8000) { status.Text = "Clipboard text is too long. Convert up to 8,000 characters at a time."; return; }
                Clipboard.SetText(YodaTransformer.Transform(original));
                status.Text = "Clipboard converted. Paste it into your conversation.";
                if (!Visible) tray.ShowBalloonTip(1800, "YodaFy", "Clipboard converted. Paste it where you want.", ToolTipIcon.Info);
            }
            catch { status.Text = "Clipboard is busy. Try the shortcut again in a moment."; }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            hotkeyAvailable = Native.RegisterHotKey(Handle, 1, 0x4000 | 0x0001 | 0x0002, (uint)Keys.Y);
            if (!hotkeyAvailable) status.Text = "Ctrl + Alt + Y is already in use. Use Convert clipboard here or in the tray.";
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == 0x0312 && message.WParam.ToInt32() == 1) ConvertClipboard();
            base.WndProc(ref message);
        }

        void HandleClosing(object sender, FormClosingEventArgs e)
        {
            if (!quitting && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true; Hide();
                tray.ShowBalloonTip(1600, "YodaFy is still running", "Open YodaFy or quit from the system tray.", ToolTipIcon.Info);
                return;
            }
            if (monitor != null) monitor.Dispose();
            previewTimer.Stop(); previewTimer.Dispose();
            if (hotkeyAvailable) Native.UnregisterHotKey(Handle, 1);
            tray.Visible = false; tray.Dispose();
        }
    }
}
