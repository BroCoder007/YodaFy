using System;
using System.Runtime.InteropServices;
using System.Text;

namespace YodaFy
{
    internal static class Native
    {
        [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr window, StringBuilder value, int maximum);
        [DllImport("user32.dll")] internal static extern uint GetClipboardSequenceNumber();
        [DllImport("user32.dll")] internal static extern bool GetLastInputInfo(ref LastInput info);
        [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll")] internal static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, uint key);
        [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr window, int id);
        [DllImport("user32.dll")] static extern bool GetGUIThreadInfo(uint thread, ref GuiThreadInfo info);
        [DllImport("imm32.dll")] static extern IntPtr ImmGetContext(IntPtr window);
        [DllImport("imm32.dll")] static extern bool ImmReleaseContext(IntPtr window, IntPtr context);
        [DllImport("imm32.dll", CharSet = CharSet.Unicode)] static extern int ImmGetCompositionStringW(IntPtr context, uint index, IntPtr buffer, uint length);
        [StructLayout(LayoutKind.Sequential)] internal struct LastInput { public uint Size; public uint Time; }
        [StructLayout(LayoutKind.Sequential)] struct NativeRect { public int Left, Top, Right, Bottom; }
        [StructLayout(LayoutKind.Sequential)] struct GuiThreadInfo
        {
            public uint Size, Flags;
            public IntPtr Active, Focus, Capture, MenuOwner, MoveSize, Caret;
            public NativeRect CaretRect;
        }

        internal static string WindowTitle(IntPtr window)
        {
            StringBuilder text = new StringBuilder(1024);
            GetWindowText(window, text, text.Capacity);
            return text.ToString();
        }

        internal static uint InputTick()
        {
            LastInput input = new LastInput { Size = (uint)Marshal.SizeOf(typeof(LastInput)) };
            return GetLastInputInfo(ref input) ? input.Time : unchecked((uint)Environment.TickCount);
        }

        internal static uint IdleTime() { return unchecked((uint)Environment.TickCount - InputTick()); }

        internal static bool ModifierDown()
        {
            return GetAsyncKeyState(0x10) < 0 || GetAsyncKeyState(0x11) < 0 || GetAsyncKeyState(0x12) < 0 || GetAsyncKeyState(0x5B) < 0 || GetAsyncKeyState(0x5C) < 0;
        }

        internal static bool IsComposing(IntPtr foreground)
        {
            uint process;
            uint thread = GetWindowThreadProcessId(foreground, out process);
            GuiThreadInfo info = new GuiThreadInfo { Size = (uint)Marshal.SizeOf(typeof(GuiThreadInfo)) };
            if (!GetGUIThreadInfo(thread, ref info) || info.Focus == IntPtr.Zero) return false;
            IntPtr context = ImmGetContext(info.Focus);
            if (context == IntPtr.Zero) return false;
            try { return ImmGetCompositionStringW(context, 8, IntPtr.Zero, 0) > 0; }
            finally { ImmReleaseContext(info.Focus, context); }
        }
    }
}
