# Cordspan Network USB/IP Design

## Product Direction

Cordspan is a Windows 11 application for remote USB access between Windows PCs, focused on gaming devices such as controllers, receivers, adapters, wheels, pedals, and specialty input hardware.

The app is a two-role USB/IP configurator:

- Share devices from this PC with `usbipd-win`.
- Attach devices shared by another PC with `usbip-win2`.
- Discover reachable USB/IP hosts on the local network.
- Manage active imported and exported sessions from one native Windows interface.

WSL-specific workflows are out of scope. The app should use generic network language: share, discover, attach, detach, and stop sharing.

## Audience

The primary user wants a gaming device physically connected to one Windows machine to appear as a local USB device on another Windows machine. Common examples include a controller dongle on a couch PC, a wheel or pedal set in a sim rig, or specialty input devices used by a remote game-streaming host.

The UI should assume users understand the two PCs involved but do not want to memorize command-line syntax, bus IDs, driver state, or firewall details.

## Tool Roles

### Host: usbipd-win

`usbipd-win` runs on the PC with the physical USB device.

Core commands:

```powershell
usbipd list
usbipd bind --busid <BUSID>
usbipd bind --force --busid <BUSID>
usbipd unbind --busid <BUSID>
```

The app should show local devices, current share state, and whether a device may require force-sharing because a local application has claimed it.

### Client: usbip-win2

`usbip-win2` runs on the PC that wants to import a remote USB device.

Core commands:

```powershell
usbip list --remote <HOST>
usbip attach --remote <HOST> --busid <BUSID>
usbip port
usbip detach --port <PORT>
```

The app should show remote hosts, exported devices, active imports, and detach controls.

## Architecture

Use separate command adapters and parsers so each tool can evolve independently:

- `UsbipdWinService`: local physical devices, share, force-share, stop sharing.
- `UsbipWin2Service`: remote device listing, attach, imported port listing, detach.
- `ICommandRunner`: process boundary abstraction for tests and future elevation handling.
- `NetworkDiscoveryService`: finds candidate USB/IP hosts through local probing and app-level advertisements.
- `UsbOverIpCoordinator`: combines host, client, and discovery data into app-facing state.

Avoid putting command-line parsing or process execution in the WinUI code-behind.

## Discovery

Discovery should be layered:

1. Manual host entry by DNS name or IP address.
2. Local subnet TCP probe for the USB/IP port, default `3240`.
3. Validation by running `usbip list --remote <HOST>` against reachable candidates.
4. Cordspan advertisement over UDP or mDNS-style broadcast so two app instances can find each other quickly.

The app must tolerate blocked ports, VPNs, multiple adapters, and slow hosts. Discovery should be cancellable and should not freeze the UI.

## Security Model

USB/IP exposes physical USB devices over the network. Treat this as trusted-LAN functionality. The app should not imply that USB/IP is safe to expose to the public internet.

Recommended user-facing posture:

- Prefer trusted LAN, Tailscale, WireGuard, or SSH tunnels for non-local access.
- Show port reachability and firewall state.
- Make force-share explicit because it can take a device away from local Windows applications.

## UI Shape

Use a Windows 11 `NavigationView` shell with three primary pages:

- **This PC**: local devices, share/force-share/stop-sharing, host readiness.
- **Network**: discovered hosts, remote exported devices, attach actions, manual host entry.
- **Sessions**: active imports from `usbip port`, active shares, reconnect/detach controls.

Settings can follow later for discovery preferences, preferred network interface, custom port, and advanced device visibility.

## Testing Strategy

Unit tests should cover:

- `usbipd list` parsing, including connected and persisted sections.
- `usbip list --remote` parsing.
- `usbip port` parsing.
- Correct command argument construction for share, force-share, unshare, list remote, attach, and detach.
- Failure handling and error messages.

UI validation should include:

- Build verification for every slice.
- Automated screenshot capture after launching the app.
- Visual QA against Windows 11 conventions: no WSL labels, no clipped command text, native Mica/theme resources, readable rows, and visible empty/error states.
