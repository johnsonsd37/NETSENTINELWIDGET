# Net Sentinel Widget

A lightweight, always-on-top Windows network activity widget. It shows live system-wide download/upload rates, total traffic, active IPv4 TCP connections, remote addresses, and the process that owns each connection. It warns after upload remains above 5 MB/s for five seconds.

## Build and run

1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) on Windows.
2. Right-click `publish.ps1`, choose **Run with PowerShell**, or run `powershell -ExecutionPolicy Bypass -File .\publish.ps1` in this folder.
3. Open the generated `NetSentinelWidget.exe` shown by the script. No .NET installation is needed on the destination computer because the build is self-contained.

On its first launch, the widget adds itself to the current user's Windows startup list and will open automatically after future sign-ins. No administrator permission is required. Clear **Start automatically when I sign in** inside the widget to disable this behavior.

For easier process visibility, run the app as Administrator. Drag the title area to move it; resize from the lower-right edge.

## Important security limitation

This tool is an indicator, not a forensic data-loss-prevention system. High outbound traffic may be a backup, video call, game, browser upload, or file transfer. Encrypted connections normally prevent this widget from determining which file was sent. If an alert appears, disconnect the network if appropriate, note the process and remote address, and run Microsoft Defender Offline scan. Also review Windows file sharing and disable it on networks where it is not needed.

## Privacy

All measurements stay on the computer. The program sends no telemetry and makes no network connections of its own.
