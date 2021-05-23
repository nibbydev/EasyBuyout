﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace EasyBuyout
{
    public static class MouseHook
    {
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        public static event Action MouseAction;
        private static LowLevelMouseProc _proc       = HookCallback;
        private static IntPtr            _hookID     = IntPtr.Zero;
        private const  int               WH_MOUSE_LL = 14;

        public static void Start()
        {
            if (_hookID == IntPtr.Zero) _hookID = SetHook(_proc);
        }

        public static void Stop()
        {
            UnhookWindowsHookEx(_hookID);
        }

        private static IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
                }
        }

        private static IntPtr HookCallback(
        int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && MouseMessages.WM_RBUTTONDOWN == (MouseMessages) wParam)
            {
                MouseAction?.Invoke();
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private enum MouseMessages
        {
            WM_LBUTTONDOWN = 0x0201,
            WM_LBUTTONUP   = 0x0202,
            WM_MOUSEMOVE   = 0x0200,
            WM_MOUSEWHEEL  = 0x020A,
            WM_RBUTTONDOWN = 0x0204,
            WM_RBUTTONUP   = 0x0205
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}