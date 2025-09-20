using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
#if WINDOWS
using WinRT.Interop;
#endif

namespace winwallhaven.Services;

public static class ScreenResolutionHelper
{
#if WINDOWS
    [SupportedOSPlatform("windows")]
    public static bool TryGetCurrentMonitorResolution(out int width, out int height)
    {
        width = 0;
        height = 0;
        try
        {
            var window = App.MainAppWindow;
            if (window == null) return false;
            var hwnd = WindowNative.GetWindowHandle(window);
            var hMonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (hMonitor == IntPtr.Zero) return false;
            var info = new MONITORINFO { cbSize = (uint)Marshal.SizeOf<MONITORINFO>() };
            if (!GetMonitorInfo(hMonitor, ref info)) return false;
            width = info.rcMonitor.Right - info.rcMonitor.Left;
            height = info.rcMonitor.Bottom - info.rcMonitor.Top;
            return width > 0 && height > 0;
        }
        catch
        {
            return false;
        }
    }

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("User32.dll", SetLastError = false)]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("User32.dll", SetLastError = true)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
#else
    public static bool TryGetCurrentMonitorResolution(out int width, out int height)
    {
        width = 0;
        height = 0;
        return false;
    }
#endif
}