using System;
using System.Runtime.InteropServices;

namespace Jamify
{
    static class User32
    {
        [DllImport("user32.dll", EntryPoint = "GetSystemMetrics")]
        public static extern int GetSystemMetrics(int which);

        [DllImport("user32.dll", EntryPoint="SetWindowPos")]
        public static extern void SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int width, int height, uint flags);

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;
        private static readonly IntPtr HWND_TOP = IntPtr.Zero;
        private const int SWP_SHOWWINDOW = 64; // 0x0040

        public static void SetWinFullScreen(IntPtr hwnd)
        {
            var screenX = GetSystemMetrics(SM_CXSCREEN);
            var screenY = GetSystemMetrics(SM_CYSCREEN);
            SetWindowPos(hwnd, HWND_TOP, 0, 0, screenX, screenY, SWP_SHOWWINDOW);
        }
    }
}
