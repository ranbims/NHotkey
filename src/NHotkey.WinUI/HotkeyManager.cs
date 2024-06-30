using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.System;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace NHotkey.WinUI
{
    public class HotkeyManager : HotkeyManagerBase
    {
        private HotkeyManager()
        {
            _window = new Window();
            IntPtr handle = WinRT.Interop.WindowNative.GetWindowHandle(_window);
            SetHwnd(handle);
            
            HWND hWND = new(handle);
            _originalWndProc = Marshal.GetDelegateForFunctionPointer<WndProc>(
                PInvoke.GetWindowLongPtr(hWND, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC));
            _wndProcHook = WndProcHook;
            PInvoke.SetWindowLongPtr(hWND, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC, 
                Marshal.GetFunctionPointerForDelegate(_wndProcHook));
        }

        ~HotkeyManager()
        {
            IntPtr handle = WinRT.Interop.WindowNative.GetWindowHandle(_window);
            HWND hWND = new(handle);
            PInvoke.SetWindowLongPtr(hWND, WINDOW_LONG_PTR_INDEX.GWL_WNDPROC,
               Marshal.GetFunctionPointerForDelegate(_originalWndProc));
        }

        #region Singleton implementation

        public static HotkeyManager Current { get { return LazyInitializer.Instance; } }

        private static class LazyInitializer
        {
            static LazyInitializer() { }
            public static readonly HotkeyManager Instance = new();
        }

        #endregion

        public void AddOrReplace(string name, KeyboardAccelerator gesture, EventHandler<HotkeyEventArgs> handler)
        {
            AddOrReplace(name, gesture.Key, gesture.Modifiers, handler);
        }

        public void AddOrReplace(string name, VirtualKey key, VirtualKeyModifiers modifiers, EventHandler<HotkeyEventArgs> handler)
        {
            var flags = GetFlags(modifiers, false);
            var vk = (uint)key;
            AddOrReplace(name, vk, flags, handler);
        }

        private IntPtr WndProcHook(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            Hotkey hotkey;
            bool handled = false;
            var result = HandleHotkeyMessage(hWnd, (int)msg, wParam, lParam, ref handled, out hotkey);
            if (handled)
                return result;

            return _originalWndProc(hWnd, msg, wParam, lParam);
        }

        private static HotkeyFlags GetFlags(VirtualKeyModifiers modifiers, bool noRepeat)
        {
            var flags = HotkeyFlags.None;
            if (modifiers.HasFlag(VirtualKeyModifiers.Shift))
                flags |= HotkeyFlags.Shift;
            if (modifiers.HasFlag(VirtualKeyModifiers.Control))
                flags |= HotkeyFlags.Control;
            if (modifiers.HasFlag(VirtualKeyModifiers.Menu))
                flags |= HotkeyFlags.Alt;
            if (modifiers.HasFlag(VirtualKeyModifiers.Windows))
                flags |= HotkeyFlags.Windows;
            if (noRepeat)
                flags |= HotkeyFlags.NoRepeat;
            return flags;
        }

        //private static VirtualKeyModifiers GetModifiers(HotkeyFlags flags)
        //{
        //    var modifiers = VirtualKeyModifiers.None;
        //    if (flags.HasFlag(HotkeyFlags.Shift))
        //        modifiers |= VirtualKeyModifiers.Shift;
        //    if (flags.HasFlag(HotkeyFlags.Control))
        //        modifiers |= VirtualKeyModifiers.Control;
        //    if (flags.HasFlag(HotkeyFlags.Alt))
        //        modifiers |= VirtualKeyModifiers.Menu;
        //    if (flags.HasFlag(HotkeyFlags.Windows))
        //        modifiers |= VirtualKeyModifiers.Windows;
        //    return modifiers;
        //}

        private Window _window;
        private WndProc _originalWndProc;
        private WndProc _wndProcHook;

        public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
    }
}
