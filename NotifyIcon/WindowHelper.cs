namespace NotifyIcon;

using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;


internal class WindowHelper : IDisposable
{
    private readonly Window window;
    private readonly WNDPROC windowProc, nativeWindowProc;

    public WindowHelper(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        this.window = window;

        windowProc = new WNDPROC(WindowProc);

        var proc = PInvoke.SetWindowLongPtr(
            hWnd: new HWND(WinRT.Interop.WindowNative.GetWindowHandle(this.window)),
            nIndex: WINDOW_LONG_PTR_INDEX.GWL_WNDPROC,
            dwNewLong: Marshal.GetFunctionPointerForDelegate(windowProc));

        nativeWindowProc = Marshal.GetDelegateForFunctionPointer<WNDPROC>(proc);
    }

    ~WindowHelper()
    {
        Dispose(disposing: false);
    }

    public HWND Handle => (HWND)WinRT.Interop.WindowNative.GetWindowHandle(window);

    public bool IsDisposed { get; private set; } = false;

    public event Action<uint, uint, int>? Message;

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!IsDisposed)
        {
            PInvoke.SetWindowLongPtr(
                    hWnd: new HWND(WinRT.Interop.WindowNative.GetWindowHandle(this.window)),
                    nIndex: WINDOW_LONG_PTR_INDEX.GWL_WNDPROC,
                    dwNewLong: Marshal.GetFunctionPointerForDelegate(nativeWindowProc));

            IsDisposed = true;
        }
    }

    private LRESULT WindowProc(HWND hWnd, uint msg, WPARAM wParam, LPARAM lParam)
    {
        Message?.Invoke(msg, (uint)wParam.Value, (int)lParam.Value);

        return PInvoke.CallWindowProc(nativeWindowProc, hWnd, msg, wParam, lParam);
    }
}
