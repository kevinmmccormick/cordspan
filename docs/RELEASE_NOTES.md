# Cordspan prerelease notes

## Initial alpha

This is the first Cordspan prerelease and is intended for evaluation on trusted
Windows networks.

Implemented:

- Native WinUI 3 interface for local devices, network devices, and sessions
- Local device listing, sharing, force sharing, and stopping a share through
  `usbipd-win`
- Manual remote-host queries and remote device attachment through `usbip-win2`
- Imported-device listing and detach-by-port
- Executable discovery through application-local and machine `PATH` locations
- Unit coverage for parsers, command construction, executable resolution, and
  network-host validation

Known limitations:

- Automatic local-network discovery is still under development.
- Cordspan requires administrator approval at startup.
- `usbipd-win` and `usbip-win2` must be installed separately.
- USB/IP should be used only on a trusted or separately secured network.
- The release has not yet been broadly validated across USB device classes,
  VPN configurations, or multiple Windows versions.
