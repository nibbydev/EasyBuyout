using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Pricer.hooks {
    public sealed class WindowDiscovery {
        private const int nChars = 256;

        public static string GetActiveWindowTitle() {
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
                return Buff.ToString();
            else
                return null;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
    }
}
