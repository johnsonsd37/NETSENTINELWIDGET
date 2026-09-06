using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace NetSentinelWidget;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private long lastReceived, lastSent;
    private int highUploadSeconds;
    private const long AlertBytesPerSecond = 5 * 1024 * 1024;

    public MainWindow()
    {
        InitializeComponent();
        StartupToggle.IsChecked = StartupManager.IsEnabled;
        (lastReceived, lastSent) = ReadTotals();
        timer.Tick += Tick;
        timer.Start();
    }

    private void Tick(object? sender, EventArgs e)
    {
        var (received, sent) = ReadTotals();
        var down = Math.Max(0, received - lastReceived);
        var up = Math.Max(0, sent - lastSent);
        lastReceived = received; lastSent = sent;

        DownloadRate.Text = $"{FormatBytes(down)}/s";
        UploadRate.Text = $"{FormatBytes(up)}/s";
        DownloadTotal.Text = $"Total {FormatBytes(received)}";
        UploadTotal.Text = $"Total {FormatBytes(sent)}";

        highUploadSeconds = up >= AlertBytesPerSecond ? highUploadSeconds + 1 : 0;
        if (AlertToggle.IsChecked == true && highUploadSeconds >= 5)
        {
            StatusCard.Background = new SolidColorBrush(Color.FromRgb(68, 29, 35));
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(255, 124, 124));
            StatusText.Text = "High outbound transfer detected";
            StatusDetail.Text = $"Upload has exceeded 5 MB/s for {highUploadSeconds} seconds. Review the processes and remote addresses below.";
            System.Media.SystemSounds.Exclamation.Play();
        }
        else
        {
            StatusCard.Background = new SolidColorBrush(Color.FromRgb(25, 43, 36));
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(103, 232, 165));
            StatusText.Text = "Monitoring network activity";
            StatusDetail.Text = "No unusual outbound transfer detected";
        }

        try
        {
            var rows = TcpConnections.Get().Where(c => c.State == "ESTABLISHED").Take(40).ToList();
            ConnectionsList.ItemsSource = rows;
            ConnectionCount.Text = rows.Count.ToString();
        }
        catch { ConnectionCount.Text = "Unavailable"; }
    }

    private static (long Received, long Sent) ReadTotals()
    {
        long rx = 0, tx = 0;
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.OperationalStatus != OperationalStatus.Up || nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
            try { var s = nic.GetIPv4Statistics(); rx += s.BytesReceived; tx += s.BytesSent; } catch { }
        }
        return (rx, tx);
    }

    private static string FormatBytes(long value)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double n = value; var i = 0;
        while (n >= 1024 && i < units.Length - 1) { n /= 1024; i++; }
        return $"{n:0.#} {units[i]}";
    }

    private void DragWindow(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
    private void CloseWindow(object sender, RoutedEventArgs e) => Close();
    private void StartupChanged(object sender, RoutedEventArgs e)
    {
        if (IsLoaded) StartupManager.SetEnabled(StartupToggle.IsChecked == true);
    }
}

public record ConnectionRow(string Process, string Remote, string State);

internal static class TcpConnections
{
    private const int AfInet = 2;
    private enum TcpTableClass { OwnerPidAll = 5 }
    [StructLayout(LayoutKind.Sequential)]
    private struct Row { public uint State, LocalAddr, LocalPort, RemoteAddr, RemotePort, Pid; }
    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(IntPtr table, ref int size, bool order, int family, TcpTableClass tableClass, uint reserved);

    public static IEnumerable<ConnectionRow> Get()
    {
        var size = 0;
        GetExtendedTcpTable(IntPtr.Zero, ref size, true, AfInet, TcpTableClass.OwnerPidAll, 0);
        var ptr = Marshal.AllocHGlobal(size);
        try
        {
            if (GetExtendedTcpTable(ptr, ref size, true, AfInet, TcpTableClass.OwnerPidAll, 0) != 0) yield break;
            var count = Marshal.ReadInt32(ptr);
            var rowPtr = IntPtr.Add(ptr, 4);
            var rowSize = Marshal.SizeOf<Row>();
            for (var i = 0; i < count; i++)
            {
                var row = Marshal.PtrToStructure<Row>(IntPtr.Add(rowPtr, i * rowSize));
                var process = "PID " + row.Pid;
                try { process = Process.GetProcessById((int)row.Pid).ProcessName; } catch { }
                var ip = new System.Net.IPAddress(row.RemoteAddr).ToString();
                var port = (ushort)System.Net.IPAddress.NetworkToHostOrder((short)(row.RemotePort & 0xffff));
                yield return new ConnectionRow(process, $"{ip}:{port}", ((TcpState)row.State).ToString().ToUpperInvariant());
            }
        }
        finally { Marshal.FreeHGlobal(ptr); }
    }
}
