using Cordspan.Models;

namespace Cordspan.ViewModels;

public sealed class DeviceViewModel : ObservableObject
{
    private bool isBusy;

    public DeviceViewModel(UsbDevice device)
    {
        BusId = device.BusId;
        Vid = device.Vid;
        Pid = device.Pid;
        Name = string.IsNullOrWhiteSpace(device.Name) ? "Unknown USB device" : device.Name;
        State = string.IsNullOrWhiteSpace(device.State) ? "Unknown" : device.State;
    }

    public string BusId { get; set; }

    public string Vid { get; set; }

    public string Pid { get; set; }

    public string VidPid => $"{Vid}:{Pid}";

    public string Name { get; set; }

    public string State { get; set; }

    public string Category => InferCategory(Name);

    public bool IsShared => State.Contains("Shared", StringComparison.OrdinalIgnoreCase);

    public bool IsAttached => State.Contains("Attached", StringComparison.OrdinalIgnoreCase);

    public bool IsAvailable => !IsAttached && !State.Contains("Unavailable", StringComparison.OrdinalIgnoreCase);

    public bool IsBusy
    {
        get => isBusy;
        set
        {
            if (SetProperty(ref isBusy, value))
            {
                OnPropertyChanged(nameof(CanShare));
                OnPropertyChanged(nameof(CanUnshare));
                OnPropertyChanged(nameof(CanForceShare));
                OnPropertyChanged(nameof(CanAttach));
                OnPropertyChanged(nameof(CanDetach));
            }
        }
    }

    public bool CanShare => !IsBusy && !IsShared && !IsAttached && IsAvailable;

    public bool CanUnshare => !IsBusy && IsShared && !IsAttached;

    public bool CanForceShare => !IsBusy && !IsAttached;

    public bool CanAttach => !IsBusy && IsShared && !IsAttached;

    public bool CanDetach => !IsBusy && IsAttached;

    private static string InferCategory(string name)
    {
        var value = name.ToLowerInvariant();
        if (value.Contains("serial") || value.Contains("uart") || value.Contains("com"))
        {
            return "Serial";
        }

        if (value.Contains("camera") || value.Contains("webcam"))
        {
            return "Camera";
        }

        if (value.Contains("storage") || value.Contains("disk") || value.Contains("flash"))
        {
            return "Storage";
        }

        if (value.Contains("bluetooth") || value.Contains("wireless") || value.Contains("network"))
        {
            return "Network";
        }

        return "Peripheral";
    }
}
