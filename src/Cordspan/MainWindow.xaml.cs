using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Cordspan.Models;
using Cordspan.Services;
using Cordspan.ViewModels;

namespace Cordspan;

public sealed partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly UsbipdWinService hostService = new();
    private readonly UsbipWin2Service clientService = new();
    private readonly NetworkDiscoveryService discoveryService;
    private readonly ObservableCollection<DeviceViewModel> allLocalDevices = [];
    private readonly CancellationTokenSource shutdown = new();
    private string searchText = string.Empty;
    private string pageTitle = "This PC";
    private string pageSubtitle = "Share local gaming USB devices with other Windows PCs on your network.";
    private string remoteHost = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<DeviceViewModel> VisibleDevices { get; } = [];

    public ObservableCollection<RemoteUsbDevice> RemoteDevices { get; } = [];

    public ObservableCollection<DiscoveredUsbipHost> DiscoveredHosts { get; } = [];

    public ObservableCollection<ImportedUsbDevice> ImportedDevices { get; } = [];

    public string ToolSummary => $"usbipd: {hostService.ExecutablePath}; usbip: {clientService.ExecutablePath}";

    public int DeviceCount => allLocalDevices.Count;

    public int VisibleDeviceCount => VisibleDevices.Count;

    public int SharedCount => allLocalDevices.Count(device => device.IsShared);

    public int ImportedCount => ImportedDevices.Count;

    public string LastAction { get; private set; } = "Waiting for the first device scan.";

    public string LastUpdated { get; private set; } = string.Empty;

    public string PageTitle
    {
        get => pageTitle;
        private set => SetProperty(ref pageTitle, value);
    }

    public string PageSubtitle
    {
        get => pageSubtitle;
        private set => SetProperty(ref pageSubtitle, value);
    }

    public string RemoteHost
    {
        get => remoteHost;
        set => SetProperty(ref remoteHost, value);
    }

    public bool IsStatusOpen { get; private set; } = true;

    public InfoBarSeverity StatusSeverity { get; private set; } = InfoBarSeverity.Informational;

    public string StatusTitle { get; private set; } = "Ready";

    public string StatusMessage { get; private set; } = "Refresh to scan local devices and imported USB/IP sessions.";

    public MainWindow()
    {
        discoveryService = new NetworkDiscoveryService(new TcpNetworkProbe(), clientService);
        InitializeComponent();
        Root.DataContext = this;
        SystemBackdrop = new MicaBackdrop();
        ExtendsContentIntoTitleBar = false;

        SetWindowSize();
        ApplyStartupPage();
        UpdateEmptyStates();
        _ = RefreshAllAsync();
    }

    private void ContentRoot_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var useCompactDeviceLayout = e.NewSize.Width < 980;
        ThisPcSummaryRail.Visibility = useCompactDeviceLayout ? Visibility.Collapsed : Visibility.Visible;
        ThisPcSummaryColumn.Width = useCompactDeviceLayout
            ? new GridLength(0)
            : new GridLength(300);
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAllAsync();
    }

    private async void ListRemoteButton_Click(object sender, RoutedEventArgs e)
    {
        await RefreshRemoteDevicesAsync();
    }

    private async void CheckHostButton_Click(object sender, RoutedEventArgs e)
    {
        await CheckRemoteHostAsync();
    }

    private async void ListDiscoveredHostButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: DiscoveredUsbipHost host })
        {
            RemoteHost = host.Host;
            RemoteHostBox.Text = host.Host;
            await RefreshRemoteDevicesAsync();
        }
    }

    private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
        {
            return;
        }

        searchText = sender.Text.Trim();
        ApplyLocalFilters();
    }

    private void Shell_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        var tag = (args.SelectedItem as NavigationViewItem)?.Tag as string;
        ShowPage(tag);
    }

    private async void ShareButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteLocalDeviceActionAsync(sender, "Sharing device", device => hostService.ShareAsync(device.BusId, force: false, shutdown.Token));
    }

    private async void ForceShareButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteLocalDeviceActionAsync(sender, "Force sharing device", device => hostService.ShareAsync(device.BusId, force: true, shutdown.Token));
    }

    private async void UnshareButton_Click(object sender, RoutedEventArgs e)
    {
        await ExecuteLocalDeviceActionAsync(sender, "Stopping sharing", device => hostService.StopSharingAsync(device.BusId, shutdown.Token));
    }

    private async void AttachRemoteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: RemoteUsbDevice device })
        {
            return;
        }

        SetStatus(InfoBarSeverity.Informational, "Attaching remote device", $"{device.Name} from {device.Host}");
        try
        {
            var result = await clientService.AttachAsync(device.Host, device.BusId, shutdown.Token);
            LastAction = string.IsNullOrWhiteSpace(result.DisplayText)
                ? $"Attached {device.Name} from {device.Host}."
                : result.DisplayText;
            LastUpdated = $"Updated {DateTimeOffset.Now:t}";
            RaiseDashboardProperties();
            SetStatus(InfoBarSeverity.Success, "Device attached", LastAction);
            await RefreshImportedDevicesAsync();
        }
        catch (Exception ex) when (ex is UsbipdException or OperationCanceledException)
        {
            LastAction = $"Attach failed for {device.Name}.";
            LastUpdated = $"Failed {DateTimeOffset.Now:t}";
            RaiseDashboardProperties();
            SetStatus(InfoBarSeverity.Error, "Attach failed", ex.Message);
        }
    }

    private async void DetachImportedButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: ImportedUsbDevice device })
        {
            return;
        }

        SetStatus(InfoBarSeverity.Informational, "Detaching device", $"{device.Name} on port {device.Port}");
        try
        {
            var result = await clientService.DetachAsync(device.Port, shutdown.Token);
            LastAction = string.IsNullOrWhiteSpace(result.DisplayText)
                ? $"Detached {device.Name} from port {device.Port}."
                : result.DisplayText;
            LastUpdated = $"Updated {DateTimeOffset.Now:t}";
            RaiseDashboardProperties();
            SetStatus(InfoBarSeverity.Success, "Device detached", LastAction);
            await RefreshImportedDevicesAsync();
        }
        catch (Exception ex) when (ex is UsbipdException or OperationCanceledException)
        {
            LastAction = $"Detach failed for port {device.Port}.";
            LastUpdated = $"Failed {DateTimeOffset.Now:t}";
            RaiseDashboardProperties();
            SetStatus(InfoBarSeverity.Error, "Detach failed", ex.Message);
        }
    }

    private void OpenUsbipDocs_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/dorssel/usbipd-win",
            UseShellExecute = true
        });
    }

    private void ApplyStartupPage()
    {
        var pageArg = Environment.GetCommandLineArgs()
            .Select(arg => arg.Trim())
            .FirstOrDefault(arg => arg.StartsWith("--page=", StringComparison.OrdinalIgnoreCase));

        var page = pageArg?.Split('=', 2)[1];
        if (string.IsNullOrWhiteSpace(page))
        {
            return;
        }

        var tag = page.Equals("Network", StringComparison.OrdinalIgnoreCase)
            ? "Network"
            : page.Equals("Sessions", StringComparison.OrdinalIgnoreCase)
                ? "Sessions"
                : "ThisPc";

        Shell.SelectedItem = tag switch
        {
            "Network" => NetworkNavItem,
            "Sessions" => SessionsNavItem,
            _ => ThisPcNavItem
        };
        ShowPage(tag);
    }

    private void ShowPage(string? tag)
    {
        ThisPcPage.Visibility = tag == "ThisPc" || string.IsNullOrWhiteSpace(tag) ? Visibility.Visible : Visibility.Collapsed;
        NetworkPage.Visibility = tag == "Network" ? Visibility.Visible : Visibility.Collapsed;
        SessionsPage.Visibility = tag == "Sessions" ? Visibility.Visible : Visibility.Collapsed;

        PageTitle = tag switch
        {
            "Network" => "Network",
            "Sessions" => "Sessions",
            _ => "This PC"
        };
        PageSubtitle = tag switch
        {
            "Network" => "Find a USB/IP host and attach remote gaming devices to this PC.",
            "Sessions" => "Review active imports and shared devices.",
            _ => "Share local gaming USB devices with other Windows PCs on your network."
        };
    }

    private async Task RefreshAllAsync()
    {
        await RefreshLocalDevicesAsync();
        await RefreshImportedDevicesAsync();
    }

    private async Task RefreshLocalDevicesAsync()
    {
        SetStatus(InfoBarSeverity.Informational, "Scanning this PC", "Reading local device state from usbipd-win.");

        try
        {
            var devices = await hostService.ListLocalDevicesAsync(shutdown.Token);
            allLocalDevices.Clear();
            foreach (var device in devices.OrderBy(device => device.BusId, StringComparer.OrdinalIgnoreCase))
            {
                allLocalDevices.Add(new DeviceViewModel(device));
            }

            searchText = string.Empty;
            ApplyLocalFilters();
            LastAction = devices.Count == 0
                ? "No local USB devices were returned by usbipd-win."
                : $"Loaded {devices.Count} local device{(devices.Count == 1 ? string.Empty : "s")}.";
            LastUpdated = $"Updated {DateTimeOffset.Now:t}";
            RaiseDashboardProperties();
            SetStatus(InfoBarSeverity.Success, "This PC is up to date", LastAction);
        }
        catch (Exception ex) when (ex is UsbipdException or OperationCanceledException)
        {
            LastAction = "Local device scan failed.";
            LastUpdated = $"Failed {DateTimeOffset.Now:t}";
            RaiseDashboardProperties();
            SetStatus(InfoBarSeverity.Error, "usbipd-win unavailable", ex.Message);
        }
    }

    private async Task RefreshRemoteDevicesAsync()
    {
        var host = RemoteHostBox.Text.Trim();
        RemoteHost = host;
        if (string.IsNullOrWhiteSpace(host))
        {
            SetStatus(InfoBarSeverity.Warning, "Remote host required", "Enter a hostname or IP address before listing remote devices.");
            return;
        }

        SetStatus(InfoBarSeverity.Informational, "Listing remote devices", $"Querying {host} with usbip-win2.");

        try
        {
            var devices = await clientService.ListRemoteDevicesAsync(host, shutdown.Token);
            RemoteDevices.Clear();
            foreach (var device in devices)
            {
                RemoteDevices.Add(device);
            }
            UpdateEmptyStates();

            LastAction = devices.Count == 0
                ? $"No shared USB devices were returned by {host}."
                : $"Loaded {devices.Count} remote device{(devices.Count == 1 ? string.Empty : "s")} from {host}.";
            LastUpdated = $"Updated {DateTimeOffset.Now:t}";
            RaiseDashboardProperties();
            SetStatus(InfoBarSeverity.Success, "Remote host listed", LastAction);
        }
        catch (Exception ex) when (ex is UsbipdException or OperationCanceledException)
        {
            LastAction = $"Remote query failed for {host}.";
            LastUpdated = $"Failed {DateTimeOffset.Now:t}";
            RaiseDashboardProperties();
            SetStatus(InfoBarSeverity.Error, "Remote query failed", ex.Message);
        }
    }

    private async Task CheckRemoteHostAsync()
    {
        var host = RemoteHostBox.Text.Trim();
        RemoteHost = host;
        if (string.IsNullOrWhiteSpace(host))
        {
            SetStatus(InfoBarSeverity.Warning, "Remote host required", "Enter a hostname or IP address before checking a USB/IP host.");
            return;
        }

        SetStatus(InfoBarSeverity.Informational, "Checking host", $"Testing USB/IP reachability for {host}.");

        try
        {
            var hosts = await discoveryService.ValidateHostsAsync([host], cancellationToken: shutdown.Token);
            DiscoveredHosts.Clear();
            foreach (var discoveredHost in hosts)
            {
                DiscoveredHosts.Add(discoveredHost);
            }
            UpdateEmptyStates();

            var result = hosts.SingleOrDefault();
            if (result is null)
            {
                SetStatus(InfoBarSeverity.Warning, "No host checked", "No valid host value was provided.");
                return;
            }

            SetStatus(
                result.IsReachable ? InfoBarSeverity.Success : InfoBarSeverity.Warning,
                result.IsReachable ? "Host reachable" : "Host unreachable",
                $"{result.Host}: {result.Status}");
        }
        catch (OperationCanceledException)
        {
            SetStatus(InfoBarSeverity.Warning, "Discovery canceled", "The host check was canceled.");
        }
    }

    private async Task RefreshImportedDevicesAsync()
    {
        try
        {
            var imported = await clientService.ListImportedPortsAsync(shutdown.Token);
            ImportedDevices.Clear();
            foreach (var device in imported)
            {
                ImportedDevices.Add(device);
            }

            RaiseDashboardProperties();
            UpdateEmptyStates();
        }
        catch (Exception ex) when (ex is UsbipdException or OperationCanceledException)
        {
            SetStatus(InfoBarSeverity.Warning, "usbip-win2 unavailable", ex.Message);
        }
    }

    private async Task ExecuteLocalDeviceActionAsync(object sender, string title, Func<DeviceViewModel, Task<CommandResult>> action)
    {
        if (sender is not FrameworkElement { Tag: DeviceViewModel device })
        {
            return;
        }

        device.IsBusy = true;
        SetStatus(InfoBarSeverity.Informational, title, $"{device.BusId} - {device.Name}");

        try
        {
            var result = await action(device);
            LastAction = string.IsNullOrWhiteSpace(result.DisplayText)
                ? $"{title} completed for {device.BusId}."
                : result.DisplayText;
            LastUpdated = $"Updated {DateTimeOffset.Now:t}";
            SetStatus(InfoBarSeverity.Success, "Command completed", LastAction);
            await RefreshLocalDevicesAsync();
        }
        catch (Exception ex) when (ex is UsbipdException or OperationCanceledException)
        {
            LastAction = $"{title} failed for {device.BusId}.";
            LastUpdated = $"Failed {DateTimeOffset.Now:t}";
            SetStatus(InfoBarSeverity.Error, "Command failed", ex.Message);
            RaiseDashboardProperties();
        }
        finally
        {
            device.IsBusy = false;
        }
    }

    private void ApplyLocalFilters()
    {
        VisibleDevices.Clear();

        foreach (var device in allLocalDevices.Where(MatchesLocalFilter))
        {
            VisibleDevices.Add(device);
        }

        OnPropertyChanged(nameof(VisibleDeviceCount));
    }

    private bool MatchesLocalFilter(DeviceViewModel device)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        return device.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || device.BusId.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || device.VidPid.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || device.State.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || device.Category.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }

    private void SetStatus(InfoBarSeverity severity, string title, string message)
    {
        StatusSeverity = severity;
        StatusTitle = title;
        StatusMessage = message;
        IsStatusOpen = true;
        RaiseStatusProperties();
    }

    private void UpdateEmptyStates()
    {
        if (NetworkEmptyState is not null)
        {
            NetworkEmptyState.Visibility = RemoteDevices.Count == 0 && DiscoveredHosts.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        if (SessionsEmptyState is not null)
        {
            SessionsEmptyState.Visibility = ImportedDevices.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void RaiseDashboardProperties()
    {
        OnPropertyChanged(nameof(DeviceCount));
        OnPropertyChanged(nameof(VisibleDeviceCount));
        OnPropertyChanged(nameof(SharedCount));
        OnPropertyChanged(nameof(ImportedCount));
        OnPropertyChanged(nameof(LastAction));
        OnPropertyChanged(nameof(LastUpdated));
    }

    private void RaiseStatusProperties()
    {
        OnPropertyChanged(nameof(StatusSeverity));
        OnPropertyChanged(nameof(StatusTitle));
        OnPropertyChanged(nameof(StatusMessage));
        OnPropertyChanged(nameof(IsStatusOpen));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void SetWindowSize()
    {
        var appWindow = AppWindow;
        appWindow.Resize(new Windows.Graphics.SizeInt32(1180, 760));

        var titleBar = appWindow.TitleBar;
        titleBar.ButtonBackgroundColor = Microsoft.UI.Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Microsoft.UI.Colors.Transparent;
    }
}
