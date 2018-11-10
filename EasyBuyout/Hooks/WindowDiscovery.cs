using System;
using System.Runtime.InteropServices;
using System.Text;

namespace EasyBuyout.hooks {
    public sealed class WindowDiscovery {
        private const int NChars = 256;

        public static string GetActiveWindowTitle() {
            var buff = new StringBuilder(NChars);
            var handle = GetForegroundWindow();
      
            return GetWindowText(handle, buff, NChars) > 0 ? buff.ToString() : null;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
    }
}
