using System.Drawing.Drawing2D;
using GH.VpnService.Contracts.Vpn;
using GH.VpnService.Win.Services;
using GH.VpnService.Win.Settings;

namespace GH.VpnService.Win;

public sealed class MainForm : Form
{
    private readonly WinClientSettings _settings;
    private readonly WireGuardWindowsService _wireGuard = new();
    private readonly DomainRoutingService _domainRouting = new();
    private readonly NotifyIcon _trayIcon;

    private readonly TextBox _txtBaseUrl = new() { Text = "https://localhost:7226", BorderStyle = BorderStyle.None };
    private readonly TextBox _txtSearch = new() { BorderStyle = BorderStyle.None, PlaceholderText = "Введите текст для поиска" };
    private readonly TextBox _txtClientName = new() { BorderStyle = BorderStyle.None, Text = Environment.MachineName };
    private readonly NumericUpDown _numDays = new() { Minimum = 1, Maximum = 3650, Value = 30, BorderStyle = BorderStyle.None };
    private readonly ComboBox _cboUsers = new() { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
    private readonly ComboBox _cboServers = new() { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
    private readonly ComboBox _cboClients = new() { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };

    private readonly FlowLayoutPanel _serverList = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true };
    private readonly Label _lblStatus = new() { Text = "● Отключён", AutoSize = true, Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.FromArgb(120, 124, 145) };
    private readonly Label _lblTimer = new() { Text = "00:00:00", AutoSize = true, Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(120, 124, 145) };
    private readonly Label _lblCountry = new() { Text = "Выберите сервер", AutoSize = true, Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = Color.FromArgb(26, 31, 45) };
    private readonly Label _lblServerInfo = new() { Text = "WireGuard / TUN", AutoSize = true, Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(145, 150, 165) };
    private readonly Label _lblDown = new() { Text = "↓ 0 KB/s", AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(55, 60, 80) };
    private readonly Label _lblUp = new() { Text = "↑ 0 KB/s", AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(55, 60, 80) };
    private readonly Label _lblRxTx = new() { Text = "RX 0 MB    TX 0 MB", AutoSize = true, Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(120, 124, 145) };
    private readonly Label _lblMode = new() { Text = "TUN", AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(111, 93, 255) };
    private readonly Label _lblStatusBar = new() { Text = "Ready", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.FromArgb(112, 117, 135) };
    private readonly TextBox _txtConfig = new() { Multiline = true, ScrollBars = ScrollBars.Both, Font = new Font("Consolas", 9), Visible = false };

    private readonly CheckBox _chkStartWithWindows = new() { Text = "Автозапуск", AutoSize = true };
    private readonly CheckBox _chkMinimizeToTray = new() { Text = "В трей", AutoSize = true };
    private readonly CheckBox _chkKillSwitch = new() { Text = "Kill Switch", AutoSize = true, Checked = true };
    private readonly CheckBox _chkAutoReconnect = new() { Text = "Auto reconnect", AutoSize = true, Checked = true };
    private readonly RadioButton _rbFullTunnel = new() { Text = "Full Tunnel", AutoSize = true, Checked = true };
    private readonly RadioButton _rbSelectedSites = new() { Text = "Только сайты", AutoSize = true };
    private readonly TextBox _txtAllowedDomains = new() { Multiline = true, ScrollBars = ScrollBars.Vertical, BorderStyle = BorderStyle.None, Height = 54, PlaceholderText = "github.com\r\nopenai.com\r\nstihi.ru" };
    private readonly ComboBox _cboDomainPresets = new() { DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
    private readonly ModernButton _btnAddPreset = new() { Text = "+ пресет", Width = 86, Height = 26 };
    private readonly ModernButton _btnImportDomains = new() { Text = "Import", Width = 86, Height = 26 };
    private readonly ModernButton _btnExportDomains = new() { Text = "Export", Width = 86, Height = 26 };
    private readonly ModernButton _btnNormalizeDomains = new() { Text = "Clean", Width = 86, Height = 26 };

    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    private DateTime? _connectedAt;
    private bool _reallyExit;
    private List<VpnClientListItem> _clients = [];
    private List<VpnServerListItem> _servers = [];
    private List<VpnUserListItem> _users = [];
    private VpnServerListItem? _selectedServer;

    public MainForm()
    {
        _settings = WinClientSettingsStore.Load();
        ApplySettingsToUi();

        Text = "GH Secure VPN";
        Width = 1180;
        Height = 760;
        MinimumSize = new Size(980, 640);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(246, 247, 252);
        Font = new Font("Segoe UI", 9);

        _trayIcon = CreateTrayIcon();
        _trayIcon.Visible = true;

        BuildUi();
        BindDomainPresets();
        WireEvents();

        _timer.Tick += (_, _) => UpdateConnectionTimer();
        Resize += (_, _) => HideToTrayIfNeeded();
        FormClosing += OnFormClosing;
        FormClosed += (_, _) => _trayIcon.Dispose();
        Shown += async (_, _) => await OnShownAsync();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.FromArgb(246, 247, 252)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 540));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildRail(), 0, 0);
        root.Controls.Add(BuildServersPanel(), 1, 0);
        root.Controls.Add(BuildDashboardPanel(), 2, 0);

        Controls.Add(root);
    }

    private Control BuildRail()
    {
        var rail = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(0, 20, 0, 12) };
        var stack = new FlowLayoutPanel { Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown, WrapContents = false, Height = 340, Padding = new Padding(12, 0, 12, 0) };

        stack.Controls.Add(MakeNavButton("→", false));
        stack.Controls.Add(MakeNavButton("＋", false));
        stack.Controls.Add(MakeNavButton("🌐", true));
        stack.Controls.Add(MakeNavButton("⚙", false));
        stack.Controls.Add(MakeNavButton("⌁", false));
        stack.Controls.Add(MakeNavButton("↻", false));

        var info = new Label
        {
            Text = "i",
            Dock = DockStyle.Bottom,
            Height = 28,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(145, 150, 165),
            Font = new Font("Segoe UI", 9, FontStyle.Bold)
        };

        rail.Controls.Add(info);
        rail.Controls.Add(stack);
        return rail;
    }

    private Control BuildServersPanel()
    {
        var shell = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(243, 244, 249), Padding = new Padding(28, 26, 26, 20) };

        var title = new Label
        {
            Text = "Серверы",
            Dock = DockStyle.Top,
            Height = 36,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.Black
        };

        var searchBox = new RoundedPanel { Dock = DockStyle.Top, Height = 48, Radius = 8, FillColor = Color.White, Padding = new Padding(14, 14, 14, 6), Margin = new Padding(0, 0, 0, 12) };
        var searchLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        searchLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32));
        _txtSearch.Dock = DockStyle.Fill;
        _txtSearch.BackColor = Color.White;
        var searchIcon = new Label { Text = "⌕", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 16), ForeColor = Color.FromArgb(160, 164, 176) };
        searchLayout.Controls.Add(_txtSearch, 0, 0);
        searchLayout.Controls.Add(searchIcon, 1, 0);
        searchBox.Controls.Add(searchLayout);

        var account = new RoundedPanel { Dock = DockStyle.Top, Height = 225, Radius = 10, FillColor = Color.FromArgb(235, 238, 249), Padding = new Padding(14), Margin = new Padding(0, 0, 0, 12) };
        var accountGrid = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 7, ColumnCount = 1 };
        accountGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        accountGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        accountGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        accountGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        accountGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        accountGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        accountGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        accountGrid.Controls.Add(new Label { Text = "GH Secure VPN", AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.Black }, 0, 0);
        accountGrid.Controls.Add(new Label { Text = "Трафик: ∞     Подписка: demo", AutoSize = true, ForeColor = Color.FromArgb(80, 86, 105) }, 0, 1);
        accountGrid.Controls.Add(MakeInputPanel(_txtBaseUrl), 0, 2);
        accountGrid.Controls.Add(MakeInputPanel(_cboUsers), 0, 3);
        accountGrid.Controls.Add(MakeInputPanel(_cboClients), 0, 4);
        accountGrid.Controls.Add(MakeInputPanel(_cboServers), 0, 5);
        var flags = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        flags.Controls.Add(_chkStartWithWindows);
        flags.Controls.Add(_chkMinimizeToTray);
        accountGrid.Controls.Add(flags, 0, 6);
        account.Controls.Add(accountGrid);

        _serverList.Padding = new Padding(0, 4, 0, 0);
        var serverArea = new RoundedPanel { Dock = DockStyle.Fill, Radius = 10, FillColor = Color.White, Padding = new Padding(0) };
        serverArea.Controls.Add(_serverList);

        shell.Controls.Add(serverArea);
        shell.Controls.Add(account);
        shell.Controls.Add(searchBox);
        shell.Controls.Add(title);
        return shell;
    }

    private Control BuildDashboardPanel()
    {
        var panel = new GradientPanel { Dock = DockStyle.Fill, Padding = new Padding(36, 28, 36, 22) };

        var grid = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 1, BackColor = Color.Transparent };
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 108));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 230));
        grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        var top = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Color.Transparent };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        var namePanel = MakeInputPanel(_txtClientName);
        var daysPanel = MakeInputPanel(_numDays);
        var rightTop = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        rightTop.Controls.Add(new Label { Text = "Дней:", AutoSize = true, Padding = new Padding(0, 10, 6, 0), ForeColor = Color.FromArgb(110, 115, 133) });
        rightTop.Controls.Add(daysPanel);
        top.Controls.Add(namePanel, 0, 0);
        top.Controls.Add(rightTop, 1, 0);

        var center = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var connectButton = new CircleConnectButton { Size = new Size(190, 190), Anchor = AnchorStyles.None };
        connectButton.Click += async (_, _) => await SafeRunAsync(ToggleConnectAsync);
        var centerLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = Color.Transparent };
        centerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        centerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        var buttonHost = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        buttonHost.Controls.Add(connectButton);
        buttonHost.Resize += (_, _) => connectButton.Location = new Point((buttonHost.Width - connectButton.Width) / 2, Math.Max(10, (buttonHost.Height - connectButton.Height) / 2));
        var statusHost = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Transparent };
        statusHost.Controls.Add(_lblStatus);
        statusHost.Controls.Add(_lblTimer);
        statusHost.Padding = new Padding((Width / 2) - 120, 0, 0, 0);
        centerLayout.Controls.Add(buttonHost, 0, 0);
        centerLayout.Controls.Add(statusHost, 0, 1);
        center.Controls.Add(centerLayout);

        var country = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0, 4, 0, 0) };
        var flag = new Label { Text = "🇳🇱", Width = 220, Height = 38, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI Emoji", 25) };
        _lblCountry.Width = 220;
        _lblCountry.TextAlign = ContentAlignment.MiddleCenter;
        _lblServerInfo.Width = 220;
        _lblServerInfo.TextAlign = ContentAlignment.MiddleCenter;
        country.Controls.Add(flag);
        country.Controls.Add(_lblCountry);
        country.Controls.Add(_lblServerInfo);
        country.Resize += (_, _) => country.Padding = new Padding(Math.Max(0, (country.Width - 220) / 2), 4, 0, 0);

        var widgets = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, BackColor = Color.Transparent };
        widgets.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        widgets.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        widgets.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));
        widgets.Controls.Add(MakeTrafficCard(), 0, 0);
        widgets.Controls.Add(MakeSecurityCard(), 1, 0);
        widgets.Controls.Add(MakeActionsCard(), 2, 0);

        var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = Color.Transparent };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        bottom.Controls.Add(_lblStatusBar, 0, 0);
        bottom.Controls.Add(_lblMode, 1, 0);

        grid.Controls.Add(top, 0, 0);
        grid.Controls.Add(center, 0, 1);
        grid.Controls.Add(country, 0, 2);
        grid.Controls.Add(widgets, 0, 3);
        grid.Controls.Add(bottom, 0, 4);
        panel.Controls.Add(grid);
        panel.Controls.Add(_txtConfig);
        return panel;
    }

    private Control MakeTrafficCard()
    {
        var card = MakeCard("Трафик");
        card.Controls.Add(_lblDown);
        card.Controls.Add(_lblUp);
        card.Controls.Add(_lblRxTx);
        return card;
    }

    private Control MakeSecurityCard()
    {
        var card = MakeCard("Защита");
        card.Controls.Add(_chkKillSwitch);
        card.Controls.Add(_chkAutoReconnect);
        card.Controls.Add(new Label { Text = "DNS Leak Protect", AutoSize = true, ForeColor = Color.FromArgb(80, 86, 105) });
        card.Controls.Add(new Label { Text = "Режим туннеля", AutoSize = true, Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(20, 24, 35), Margin = new Padding(0, 8, 0, 0) });
        card.Controls.Add(_rbFullTunnel);
        card.Controls.Add(_rbSelectedSites);
        return card;
    }

    private Control MakeActionsCard()
    {
        var card = MakeCard("Управление");
        var load = new ModernButton { Text = "Обновить", Width = 130, Height = 30 };
        load.Click += async (_, _) => await SafeRunAsync(LoadAllAsync);
        var create = new ModernButton { Text = "Создать VPN", Width = 130, Height = 30 };
        create.Click += async (_, _) => await SafeRunAsync(CreateClientAsync);
        var save = new ModernButton { Text = "Save .conf", Width = 130, Height = 30 };
        save.Click += async (_, _) => await SafeRunAsync(SaveConfigToFileAsync);

        card.Controls.Add(load);
        card.Controls.Add(create);
        card.Controls.Add(save);
        card.Controls.Add(new Label { Text = "Сайты для VPN:", AutoSize = true, ForeColor = Color.FromArgb(80, 86, 105), Margin = new Padding(0, 6, 0, 0) });

        _cboDomainPresets.Width = 190;
        _cboDomainPresets.Height = 26;
        card.Controls.Add(_cboDomainPresets);

        var domains = new RoundedPanel { Width = 190, Height = 72, Radius = 8, FillColor = Color.FromArgb(246, 247, 252), Padding = new Padding(8, 5, 8, 5), Margin = new Padding(0, 4, 0, 0) };
        _txtAllowedDomains.Dock = DockStyle.Fill;
        _txtAllowedDomains.BackColor = Color.FromArgb(246, 247, 252);
        domains.Controls.Add(_txtAllowedDomains);
        card.Controls.Add(domains);

        var buttons1 = new FlowLayoutPanel { Width = 190, Height = 30, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0, 4, 0, 0) };
        buttons1.Controls.Add(_btnAddPreset);
        buttons1.Controls.Add(_btnNormalizeDomains);
        card.Controls.Add(buttons1);

        var buttons2 = new FlowLayoutPanel { Width = 190, Height = 30, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0, 0, 0, 0) };
        buttons2.Controls.Add(_btnImportDomains);
        buttons2.Controls.Add(_btnExportDomains);
        card.Controls.Add(buttons2);

        return card;
    }

    private FlowLayoutPanel MakeCard(string title)
    {
        var card = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(16, 12, 10, 8),
            Margin = new Padding(8),
            BackColor = Color.White
        };
        card.Paint += (_, e) => DrawRoundBackground(e.Graphics, card.ClientRectangle, 12, Color.White, Color.FromArgb(230, 232, 240));
        card.Controls.Add(new Label { Text = title, AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.FromArgb(20, 24, 35), Margin = new Padding(0, 0, 0, 8) });
        return card;
    }

    private Panel MakeInputPanel(Control inner)
    {
        var panel = new RoundedPanel { Width = 260, Height = 34, Radius = 8, FillColor = Color.White, Padding = new Padding(10, 7, 10, 4), Margin = new Padding(0, 0, 8, 0) };
        inner.Dock = DockStyle.Fill;
        inner.BackColor = Color.White;
        panel.Controls.Add(inner);
        return panel;
    }

    private Button MakeNavButton(string text, bool selected)
    {
        return new Button
        {
            Text = text,
            Width = 44,
            Height = 44,
            FlatStyle = FlatStyle.Flat,
            BackColor = selected ? Color.FromArgb(235, 236, 244) : Color.White,
            ForeColor = selected ? Color.FromArgb(111, 93, 255) : Color.FromArgb(50, 55, 70),
            Font = new Font("Segoe UI", 14),
            Margin = new Padding(0, 6, 0, 6),
            TabStop = false
        };
    }

    private void WireEvents()
    {
        _txtSearch.TextChanged += (_, _) => RenderServers();
        _cboServers.SelectedIndexChanged += (_, _) =>
        {
            if (_cboServers.SelectedItem is DisplayItem<VpnServerListItem> item)
                SelectServer(item.Value);
        };
        _cboClients.SelectedIndexChanged += async (_, _) => await SafeRunAsync(GetSelectedConfigAsync);

        _chkStartWithWindows.CheckedChanged += (_, _) =>
        {
            try
            {
                AutostartService.SetEnabled(_chkStartWithWindows.Checked);
                SetStatus(_chkStartWithWindows.Checked ? "Автозапуск включён" : "Автозапуск выключен");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "GH Secure VPN", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        _chkMinimizeToTray.CheckedChanged += (_, _) => SaveSettingsFromUi();
        _rbFullTunnel.CheckedChanged += (_, _) => SaveSettingsFromUi();
        _rbSelectedSites.CheckedChanged += (_, _) => SaveSettingsFromUi();
        _txtAllowedDomains.Leave += (_, _) => SaveSettingsFromUi();
        _btnAddPreset.Click += (_, _) => AddSelectedPresetDomains();
        _btnImportDomains.Click += (_, _) => ImportDomainsFromFile();
        _btnExportDomains.Click += (_, _) => ExportDomainsToFile();
        _btnNormalizeDomains.Click += (_, _) => NormalizeDomainsInEditor();
    }

    private NotifyIcon CreateTrayIcon()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open", null, (_, _) => ShowFromTray());
        menu.Items.Add("Connect", null, async (_, _) => await SafeRunAsync(ConnectAsync));
        menu.Items.Add("Disconnect", null, async (_, _) => await SafeRunAsync(DisconnectAsync));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => { _reallyExit = true; Close(); });

        var icon = new NotifyIcon
        {
            Text = "GH Secure VPN",
            Icon = SystemIcons.Shield,
            ContextMenuStrip = menu
        };
        icon.DoubleClick += (_, _) => ShowFromTray();
        return icon;
    }

    private async Task OnShownAsync()
    {
        if (Environment.GetCommandLineArgs().Any(x => x.Equals("--tray", StringComparison.OrdinalIgnoreCase)))
        {
            BeginInvoke(HideToTray);
        }
        await SafeRunAsync(LoadAllAsync);
    }

    private async Task LoadAllAsync()
    {
        using var api = CreateApiClient();
        var boot = await api.GetBootstrapAsync();
        _users = boot.Users;
        _servers = boot.Servers;
        _clients = (await api.GetClientsAsync()).ToList();

        BindCombo(_cboUsers, _users, x => $"{x.Login}  <{x.Email}>");
        BindCombo(_cboServers, _servers, x => $"{x.Country} / {x.Name}");
        BindCombo(_cboClients, _clients, x => $"{x.Name}  {x.AssignedIp}");

        if (_settings.LastUserId is Guid uid)
            SelectById(_cboUsers, _users, x => x.Id == uid);
        if (_settings.LastServerId is Guid sid)
            SelectById(_cboServers, _servers, x => x.Id == sid);

        RenderServers();
        SetStatus($"Загружено: серверов {_servers.Count}, клиентов {_clients.Count}");
    }

    private static void BindCombo<T>(ComboBox combo, IReadOnlyList<T> items, Func<T, string> display)
    {
        combo.DataSource = null;
        combo.DisplayMember = nameof(DisplayItem<T>.Text);
        combo.ValueMember = nameof(DisplayItem<T>.Value);
        combo.DataSource = items.Select(x => new DisplayItem<T>(display(x), x)).ToList();
    }

    private static void SelectById<T>(ComboBox combo, IReadOnlyList<T> items, Func<T, bool> predicate)
    {
        for (var i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is DisplayItem<T> item && predicate(item.Value))
            {
                combo.SelectedIndex = i;
                return;
            }
        }
    }

    private void RenderServers()
    {
        _serverList.SuspendLayout();
        _serverList.Controls.Clear();

        var query = _txtSearch.Text.Trim();
        foreach (var server in _servers.Where(x => string.IsNullOrWhiteSpace(query) || x.Country.Contains(query, StringComparison.OrdinalIgnoreCase) || x.Name.Contains(query, StringComparison.OrdinalIgnoreCase)))
        {
            var card = new ServerCard(server) { Width = Math.Max(430, _serverList.ClientSize.Width - 24), Height = 66, Margin = new Padding(0, 0, 0, 1) };
            card.Click += (_, _) => SelectServer(server);
            foreach (Control c in card.Controls) c.Click += (_, _) => SelectServer(server);
            _serverList.Controls.Add(card);
        }
        _serverList.ResumeLayout();
    }

    private void SelectServer(VpnServerListItem? server)
    {
        if (server is null)
            return;

        _selectedServer = server;
        _lblCountry.Text = string.IsNullOrWhiteSpace(server.Country) ? server.Name : server.Country;
        _lblServerInfo.Text = $"{server.Name} / WireGuard / TUN";
        SelectById(_cboServers, _servers, x => x.Id == server.Id);
        SaveSettingsFromUi();
    }

    private async Task CreateClientAsync()
    {
        var user = GetSelectedUser();
        var server = GetSelectedServer();

        using var api = CreateApiClient();
        var result = await api.CreateClientAsync(new CreateVpnRequest
        {
            UserId = user.Id,
            ServerId = server.Id,
            Name = string.IsNullOrWhiteSpace(_txtClientName.Text) ? Environment.MachineName : _txtClientName.Text.Trim(),
            Days = (int)_numDays.Value
        });

        _txtConfig.Text = result.Config;
        SetStatus($"Создан клиент {result.Name} / {result.AssignedIp}");
        await LoadAllAsync();
    }

    private async Task GetSelectedConfigAsync()
    {
        var client = GetSelectedClient();
        using var api = CreateApiClient();
        _txtConfig.Text = await api.GetConfigAsync(client.Id);
        _lblRxTx.Text = $"RX {FormatBytes(client.RxBytes)}    TX {FormatBytes(client.TxBytes)}";
        SetStatus($"Конфиг загружен: {client.Name}");
    }

    private async Task ToggleConnectAsync()
    {
        if (_connectedAt.HasValue)
            await DisconnectAsync();
        else
            await ConnectAsync();
    }

    private async Task ConnectAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtConfig.Text))
            await GetSelectedConfigAsync();

        var tunnelName = GetTunnelName();
        var configText = await BuildEffectiveConfigAsync();
        await _wireGuard.ConnectAsync(tunnelName, configText);
        _connectedAt = DateTime.UtcNow;
        _timer.Start();
        _lblStatus.Text = "● Подключён";
        _lblStatus.ForeColor = Color.FromArgb(111, 93, 255);
        _trayIcon.Text = $"GH VPN: {tunnelName} connected";
        SetStatus($"Подключён туннель: {tunnelName}");
    }

    private async Task<string> BuildEffectiveConfigAsync()
    {
        if (!_rbSelectedSites.Checked)
        {
            _lblMode.Text = "TUN";
            return _txtConfig.Text;
        }

        var domains = GetAllowedDomains();
        if (domains.Count == 0)
            throw new InvalidOperationException("Добавьте хотя бы один домен для режима 'Только сайты'.");

        SetStatus("Резолвлю домены для Selected Sites...");
        var route = await _domainRouting.BuildSelectedSitesConfigAsync(_txtConfig.Text, domains);
        _lblMode.Text = "SITES";
        SetStatus($"Selected Sites: {route.Domains.Count} домен(ов), {route.AllowedIps.Count} IP-маршрут(ов)");
        return route.ConfigText;
    }

    private IReadOnlyList<string> GetAllowedDomains()
    {
        return DomainRoutingService.NormalizeDomains(_txtAllowedDomains.Text.Split(new[] { '\r', '\n', ',', ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
    }

    private async Task DisconnectAsync()
    {
        var tunnelName = GetTunnelName();
        await _wireGuard.DisconnectAsync(tunnelName);
        _connectedAt = null;
        _timer.Stop();
        _lblTimer.Text = "00:00:00";
        _lblStatus.Text = "● Отключён";
        _lblStatus.ForeColor = Color.FromArgb(120, 124, 145);
        _trayIcon.Text = "GH Secure VPN";
        SetStatus($"Отключён туннель: {tunnelName}");
    }

    private string GetTunnelName()
    {
        try
        {
            var client = GetSelectedClient();
            if (!string.IsNullOrWhiteSpace(client.Name))
                return _wireGuard.GetDefaultTunnelName(client.Name);
        }
        catch { }
        return _wireGuard.GetDefaultTunnelName(_txtClientName.Text);
    }

    private VpnUserListItem GetSelectedUser()
    {
        if (_cboUsers.SelectedItem is DisplayItem<VpnUserListItem> item)
            return item.Value;
        throw new InvalidOperationException("Выберите пользователя.");
    }

    private VpnServerListItem GetSelectedServer()
    {
        if (_selectedServer is not null)
            return _selectedServer;
        if (_cboServers.SelectedItem is DisplayItem<VpnServerListItem> item)
            return item.Value;
        throw new InvalidOperationException("Выберите сервер.");
    }

    private VpnClientListItem GetSelectedClient()
    {
        if (_cboClients.SelectedItem is DisplayItem<VpnClientListItem> item)
            return item.Value;
        throw new InvalidOperationException("Создайте или выберите VPN-клиент.");
    }

    private async Task SaveConfigToFileAsync()
    {
        if (string.IsNullOrWhiteSpace(_txtConfig.Text))
        {
            MessageBox.Show("Config is empty.", "GH Secure VPN", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "WireGuard config (*.conf)|*.conf|All files (*.*)|*.*",
            FileName = $"{GetTunnelName()}.conf"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var configText = await BuildEffectiveConfigAsync();
        await File.WriteAllTextAsync(dialog.FileName, configText);
        SetStatus($"Сохранено: {dialog.FileName}");
    }


    private void BindDomainPresets()
    {
        _cboDomainPresets.DisplayMember = nameof(DomainPreset.Name);
        _cboDomainPresets.ValueMember = nameof(DomainPreset.Domains);
        _cboDomainPresets.DataSource = DomainPreset.All;
        if (_cboDomainPresets.Items.Count > 0)
            _cboDomainPresets.SelectedIndex = 0;
    }

    private void AddSelectedPresetDomains()
    {
        if (_cboDomainPresets.SelectedItem is not DomainPreset preset)
            return;

        var merged = GetAllowedDomains()
            .Concat(preset.Domains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        _txtAllowedDomains.Text = string.Join(Environment.NewLine, merged);
        SaveSettingsFromUi();
        SetStatus($"Добавлен пресет: {preset.Name}");
    }

    private void NormalizeDomainsInEditor()
    {
        var domains = GetAllowedDomains();
        _txtAllowedDomains.Text = string.Join(Environment.NewLine, domains);
        SaveSettingsFromUi();
        SetStatus($"Список очищен: {domains.Count} домен(ов)");
    }

    private void ImportDomainsFromFile()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Domain list (*.txt)|*.txt|All files (*.*)|*.*",
            Title = "Import domains"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        var imported = DomainRoutingService.NormalizeDomains(File.ReadAllText(dialog.FileName).Split(new[] { '\r', '\n', ',', ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
        var merged = GetAllowedDomains()
            .Concat(imported)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        _txtAllowedDomains.Text = string.Join(Environment.NewLine, merged);
        SaveSettingsFromUi();
        SetStatus($"Импортировано доменов: {imported.Count}");
    }

    private void ExportDomainsToFile()
    {
        var domains = GetAllowedDomains();
        if (domains.Count == 0)
        {
            MessageBox.Show("Список сайтов пуст.", "GH Secure VPN", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "Domain list (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = "gh-vpn-sites.txt"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        File.WriteAllLines(dialog.FileName, domains);
        SetStatus($"Экспортировано доменов: {domains.Count}");
    }

    private void ApplySettingsToUi()
    {
        _txtBaseUrl.Text = _settings.ApiBaseUrl;
        _txtClientName.Text = string.IsNullOrWhiteSpace(_settings.LastClientName) ? Environment.MachineName : _settings.LastClientName;
        _numDays.Value = Math.Clamp(_settings.Days, (int)_numDays.Minimum, (int)_numDays.Maximum);
        _chkMinimizeToTray.Checked = _settings.MinimizeToTray;
        _chkStartWithWindows.Checked = AutostartService.IsEnabled();
        _rbFullTunnel.Checked = _settings.TunnelMode == TunnelMode.Full;
        _rbSelectedSites.Checked = _settings.TunnelMode == TunnelMode.SelectedSites;
        _txtAllowedDomains.Text = string.Join(Environment.NewLine, _settings.AllowedDomains);
    }

    private void SaveSettingsFromUi()
    {
        _settings.ApiBaseUrl = _txtBaseUrl.Text.Trim();
        _settings.LastClientName = _txtClientName.Text.Trim();
        _settings.Days = (int)_numDays.Value;
        _settings.MinimizeToTray = _chkMinimizeToTray.Checked;
        _settings.TunnelMode = _rbSelectedSites.Checked ? TunnelMode.SelectedSites : TunnelMode.Full;
        _settings.AllowedDomains = GetAllowedDomains().ToList();

        if (_cboUsers.SelectedItem is DisplayItem<VpnUserListItem> user)
            _settings.LastUserId = user.Value.Id;
        if (_selectedServer is not null)
            _settings.LastServerId = _selectedServer.Id;
        else if (_cboServers.SelectedItem is DisplayItem<VpnServerListItem> server)
            _settings.LastServerId = server.Value.Id;

        WinClientSettingsStore.Save(_settings);
    }

    private VpnApiClient CreateApiClient()
    {
        SaveSettingsFromUi();
        return new VpnApiClient(_txtBaseUrl.Text.Trim());
    }

    private void UpdateConnectionTimer()
    {
        if (_connectedAt is null)
            return;
        var span = DateTime.UtcNow - _connectedAt.Value;
        _lblTimer.Text = span.ToString(@"hh\:mm\:ss");
        _lblDown.Text = "↓ demo";
        _lblUp.Text = "↑ demo";
    }

    private void SetStatus(string text)
    {
        _lblStatusBar.Text = text;
    }

    private async Task SafeRunAsync(Func<Task> action)
    {
        try
        {
            UseWaitCursor = true;
            SetStatus("Работаю...");
            await action();
        }
        catch (Exception ex)
        {
            SetStatus("Ошибка");
            MessageBox.Show(ex.Message, "GH Secure VPN", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        SaveSettingsFromUi();
        if (!_reallyExit && _chkMinimizeToTray.Checked && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideToTray();
        }
    }

    private void HideToTrayIfNeeded()
    {
        if (_chkMinimizeToTray.Checked && WindowState == FormWindowState.Minimized)
            HideToTray();
    }

    private void HideToTray()
    {
        Hide();
        ShowInTaskbar = false;
        _trayIcon.ShowBalloonTip(1200, "GH Secure VPN", "Клиент работает в трее.", ToolTipIcon.Info);
    }

    private void ShowFromTray()
    {
        Show();
        ShowInTaskbar = true;
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private static string FormatBytes(long value)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var size = (double)value;
        var index = 0;
        while (size >= 1024 && index < units.Length - 1)
        {
            size /= 1024;
            index++;
        }
        return $"{size:0.##} {units[index]}";
    }

    private static void DrawRoundBackground(Graphics graphics, Rectangle bounds, int radius, Color fill, Color border)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;
        bounds = new Rectangle(bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
        using var path = RoundedRect(bounds, radius);
        using var brush = new SolidBrush(fill);
        using var pen = new Pen(border);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.FillPath(brush, path);
        graphics.DrawPath(pen, path);
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private sealed record DomainPreset(string Name, string[] Domains)
    {
        public static IReadOnlyList<DomainPreset> All { get; } = new[]
        {
            new DomainPreset("GitHub", new[] { "github.com", "githubusercontent.com", "githubassets.com" }),
            new DomainPreset("OpenAI / ChatGPT", new[] { "openai.com", "chatgpt.com", "oaistatic.com", "oaiusercontent.com" }),
            new DomainPreset("YouTube", new[] { "youtube.com", "youtu.be", "googlevideo.com", "ytimg.com" }),
            new DomainPreset("Google", new[] { "google.com", "gstatic.com", "googleapis.com" }),
            new DomainPreset("Telegram", new[] { "telegram.org", "t.me", "telegram.me" }),
            new DomainPreset("Стихи.ру", new[] { "stihi.ru" }),
        };
    }

    private sealed record DisplayItem<T>(string Text, T Value);

    private sealed class RoundedPanel : Panel
    {
        public int Radius { get; set; } = 10;
        public Color FillColor { get; set; } = Color.White;
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            DrawRoundBackground(e.Graphics, ClientRectangle, Radius, FillColor, Color.FromArgb(226, 229, 238));
        }
    }

    private sealed class GradientPanel : Panel
    {
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using var brush = new LinearGradientBrush(ClientRectangle, Color.FromArgb(248, 249, 253), Color.FromArgb(232, 234, 244), 45f);
            e.Graphics.FillRectangle(brush, ClientRectangle);
        }
    }

    private sealed class ModernButton : Button
    {
        public ModernButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.FromArgb(111, 93, 255);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9, FontStyle.Bold);
            Cursor = Cursors.Hand;
        }
    }

    private sealed class CircleConnectButton : Control
    {
        public CircleConnectButton()
        {
            Cursor = Cursors.Hand;
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(8, 8, Width - 16, Height - 16);
            using var outer = new Pen(Color.FromArgb(225, 228, 239), 16);
            using var middle = new Pen(Color.FromArgb(250, 250, 252), 24);
            using var innerBrush = new SolidBrush(Color.White);
            using var glowBrush = new SolidBrush(Color.FromArgb(245, 246, 252));
            e.Graphics.FillEllipse(glowBrush, rect);
            e.Graphics.DrawEllipse(outer, rect);
            var inner = Rectangle.Inflate(rect, -36, -36);
            e.Graphics.FillEllipse(innerBrush, inner);
            e.Graphics.DrawEllipse(middle, inner);
            TextRenderer.DrawText(e.Graphics, "↻", new Font("Segoe UI", 32, FontStyle.Bold), inner, Color.FromArgb(111, 93, 255), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    private sealed class ServerCard : Panel
    {
        private readonly VpnServerListItem _server;

        public ServerCard(VpnServerListItem server)
        {
            _server = server;
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            BackColor = Color.White;
            Padding = new Padding(12, 8, 12, 8);

            var flag = new Label { Text = GetFlag(server.Country), Width = 38, Dock = DockStyle.Left, Font = new Font("Segoe UI Emoji", 18), TextAlign = ContentAlignment.MiddleCenter };
            var arrow = new Label { Text = "›", Width = 24, Dock = DockStyle.Right, Font = new Font("Segoe UI", 18), ForeColor = Color.FromArgb(150, 154, 168), TextAlign = ContentAlignment.MiddleCenter };
            var title = new Label { Text = string.IsNullOrWhiteSpace(server.Country) ? server.Name : server.Country, Dock = DockStyle.Top, Height = 24, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.Black };
            var sub = new Label { Text = $"WIREGUARD / UDP / {server.Name}", Dock = DockStyle.Top, Height = 22, Font = new Font("Segoe UI", 8), ForeColor = Color.FromArgb(150, 154, 168) };
            var texts = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 4, 0, 0) };
            texts.Controls.Add(sub);
            texts.Controls.Add(title);

            Controls.Add(texts);
            Controls.Add(arrow);
            Controls.Add(flag);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var pen = new Pen(Color.FromArgb(235, 237, 244));
            e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
        }

        private static string GetFlag(string country)
        {
            var c = country.ToLowerInvariant();
            if (c.Contains("netherlands") || c.Contains("нидер")) return "🇳🇱";
            if (c.Contains("poland") || c.Contains("поль")) return "🇵🇱";
            if (c.Contains("germany") || c.Contains("герм")) return "🇩🇪";
            if (c.Contains("usa") || c.Contains("сша")) return "🇺🇸";
            if (c.Contains("uk") || c.Contains("англ")) return "🇬🇧";
            return "🌐";
        }
    }
}
