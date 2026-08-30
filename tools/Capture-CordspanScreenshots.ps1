param(
    [string]$Configuration = "Release",
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root "src\Cordspan\bin\$Platform\$Configuration\net10.0-windows10.0.19041.0\Cordspan.exe"
$outDir = Join-Path $root "artifacts\ui"
New-Item -ItemType Directory -Force $outDir | Out-Null

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class CordspanCaptureNative {
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
  [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int X, int Y);
  [DllImport("user32.dll")] public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
}
"@

function Capture-Window {
    param(
        [System.Diagnostics.Process]$Process,
        [string]$Name
    )

    $handle = $Process.MainWindowHandle
    $deadline = (Get-Date).AddSeconds(8)
    while ($handle -eq [IntPtr]::Zero -and (Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 250
        $Process.Refresh()
        $handle = $Process.MainWindowHandle
    }

    if ($handle -eq [IntPtr]::Zero) {
        throw "Cordspan did not create a main window in time."
    }

    [CordspanCaptureNative]::ShowWindow($handle, 9) | Out-Null
    [CordspanCaptureNative]::SetWindowPos($handle, [IntPtr]::Zero, 20, 60, 900, 660, 0x0040) | Out-Null
    [CordspanCaptureNative]::SetForegroundWindow($handle) | Out-Null
    Start-Sleep -Milliseconds 900

    [CordspanCaptureNative+RECT]$rect = New-Object CordspanCaptureNative+RECT
    [CordspanCaptureNative]::GetWindowRect($handle, [ref]$rect) | Out-Null
    $width = $rect.Right - $rect.Left
    $height = $rect.Bottom - $rect.Top
    $bitmap = New-Object System.Drawing.Bitmap $width, $height
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.CopyFromScreen($rect.Left, $rect.Top, 0, 0, [System.Drawing.Size]::new($width, $height))
    $path = Join-Path $outDir "$Name.png"
    $bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $graphics.Dispose()
    $bitmap.Dispose()
    $path
}

function Capture-AppPage {
    param(
        [string]$Name,
        [string]$PageArg = ""
    )

    $app = if ([string]::IsNullOrWhiteSpace($PageArg)) {
        Start-Process -FilePath $exe -PassThru
    }
    else {
        Start-Process -FilePath $exe -ArgumentList @("--page=$PageArg") -PassThru
    }
    try {
    Start-Sleep -Seconds 7
        Capture-Window $app $Name
    }
    finally {
    if (!$app.HasExited) {
        try {
            if ($app.MainWindowHandle -ne [IntPtr]::Zero) {
                [CordspanCaptureNative]::SetForegroundWindow($app.MainWindowHandle) | Out-Null
                Start-Sleep -Milliseconds 250
                [System.Windows.Forms.SendKeys]::SendWait("%{F4}")
                Start-Sleep -Milliseconds 900
                $app.Refresh()
            }

            $app.CloseMainWindow() | Out-Null
            Start-Sleep -Milliseconds 600
            if (!$app.HasExited) {
                Stop-Process -Id $app.Id -Force
            }
        }
        catch {
            Write-Warning "Captured screenshots, but could not stop Cordspan automatically: $($_.Exception.Message)"
        }
    }
    }
}

Capture-AppPage "cordspan-this-pc"
Capture-AppPage "cordspan-network" "Network"
Capture-AppPage "cordspan-sessions" "Sessions"
