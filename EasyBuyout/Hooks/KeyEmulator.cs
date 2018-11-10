using System.Runtime.InteropServices;
using System.Threading;

namespace EasyBuyout.hooks {
    public sealed class KeyEmulator {
        public static void SendCtrlC() {
            keybd_event(0x11, 0, 0, 0); // 0x11 - ctrl, 0 - down
            keybd_event(0x43, 0, 0, 0); // 0x43 - c,    0 - down
            Thread.Sleep(1);
            keybd_event(0x43, 0, 2, 0); // 0x43 - c,    2 - up
            keybd_event(0x11, 0, 2, 0); // 0x11 - ctrl, 2 - up
        }

        public static void SendLMB() {
            mouse_event(0x0002, 0, 0, 0, 0); // 0x0002 - LMB_down
            Thread.Sleep(1);
            mouse_event(0x0004, 0, 0, 0, 0); // 0x0002 - LMB_up
        }

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, uint dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, uint dwExtraInf);
    }
}
