# Cordspan

Cordspan is a native WinUI 3 desktop app for sharing and attaching USB/IP devices between Windows PCs.

The previous Electron and WebSocket bridge have been replaced with a single elevated Windows desktop application. The app calls USB/IP tools directly, parses device state, and presents a Windows 11 interface for remote USB access.

> [!WARNING]
> USB/IP exposes physical USB devices over the network. Use Cordspan only on a
> trusted LAN or a network you secure with technology such as WireGuard or
> Tailscale. Do not expose USB/IP port 3240 directly to the public internet.

## Features

- Native WinUI 3 interface using Windows App SDK.
- Elevated process manifest for USB/IP operations that require administrator rights.
- Share local USB devices from this PC with `usbipd-win`.
- Attach remote USB devices from another PC with `usbip-win2`.
- Review imported USB/IP sessions and detach by port.
- Device search for local USB hardware.
- Manual remote host query, with local network discovery under active development.
- Clear status feedback for scans and command failures.

## Requirements

- Windows 10 1809 or newer.
- .NET SDK 10 or newer.
- `usbipd-win` installed and available on `PATH`, or `usbipd.exe` copied next to the app executable.
- `usbip-win2` installed and available on `PATH`, or `usbip.exe` copied next to the app executable.

## Build

```powershell
dotnet restore .\Cordspan.sln
dotnet build .\Cordspan.sln -c Release -p:Platform=x64
```

## Run

```powershell
dotnet run --project .\src\Cordspan\Cordspan.csproj -p:Platform=x64
```

Windows will request elevation when the app starts.

## Current status

Cordspan is prerelease software. Local sharing, manual remote-host queries,
remote attachment, active-session listing, and detach operations are
implemented. Automatic local-network discovery is still under development.

The command-line tools are installed separately; Cordspan releases do not
bundle `usbipd-win` or `usbip-win2`.

## Troubleshooting

- If a required executable is not found, install the corresponding tool or add
  its directory to the machine `PATH`, then restart Cordspan.
- Run Cordspan with administrator approval when Windows prompts for elevation.
- If a remote host cannot be queried, confirm TCP port 3240 is reachable and
  allowed by the host firewall.
- Force sharing can interrupt software currently using a local device. Use it
  only when you understand that impact.

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for the development workflow. The current
prerelease notes are in [docs/RELEASE_NOTES.md](docs/RELEASE_NOTES.md).

## Architecture

- `MainWindow.xaml` defines the WinUI shell for This PC, Network, and Sessions workflows.
- `UsbipdWinService` owns host-side `usbipd-win` sharing commands.
- `UsbipWin2Service` owns client-side `usbip-win2` remote listing, attach, port, and detach commands.
- `NetworkDiscoveryService` validates candidate USB/IP hosts through TCP probing and remote listing.
- `UsbipdParser` and `UsbipWin2Parser` convert command output into typed device models.
- `DeviceViewModel` computes command availability and visual state for each device.

## Project origin

Cordspan is the new name for this project and was influenced by the original
[VirtualThere](https://github.com/lmichaelwar/VirtualThere) project.

## License

The application source and documentation are available under the
[MIT License](LICENSE). See [third-party notices](THIRD_PARTY_NOTICES.md) for
the external components and tools used by the application.
