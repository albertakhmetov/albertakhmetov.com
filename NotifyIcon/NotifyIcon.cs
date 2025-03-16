namespace NotifyIcon;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.ViewManagement.Core;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

public class NotifyIcon
{
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONUP = 0x0205;
    private const uint MESSAGE_ID = 5800;
    private static readonly uint WM_TASKBARCREATED = PInvoke.RegisterWindowMessage("TaskbarCreated");
  
    private NativeWindow window;
    private string? text;
    private IIconFile? icon;

    private bool keepIconAlive;

    public NotifyIcon(bool keepIconAlive = false)
    {
        this.keepIconAlive = keepIconAlive;
        window = new NativeWindow(this);
    }

    public bool IsVisible { get; private set; }

    public Guid Id { get; init; }

    public string Text
    {
        get => text ?? string.Empty;
        set
        {
            if (text == value)
            {
                return;
            }

            text = value;
            Update();
        }
    }

    public IIconFile? Icon
    {
        get => icon;
        set
        {
            if (icon != null && !keepIconAlive)
            {
                icon.Dispose();
            }

            icon = value;
            Update();
        }
    }

    public event EventHandler? Click;

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public void Show()
    {
        var data = GetData();

        var result = PInvoke.Shell_NotifyIcon(
            dwMessage: NOTIFY_ICON_MESSAGE.NIM_ADD,
            lpData: data);

        if (result)
        {
            IsVisible = true;
        }
    }

    public void Hide()
    {
        var data = GetData();

        var result = PInvoke.Shell_NotifyIcon(
            dwMessage: NOTIFY_ICON_MESSAGE.NIM_DELETE,
            lpData: data);

        if (result)
        {
            IsVisible = false;
        }
    }

    private void Update()
    {
        if (IsVisible)
        {
            var data = GetData();

            PInvoke.Shell_NotifyIcon(
                dwMessage: NOTIFY_ICON_MESSAGE.NIM_MODIFY,
                lpData: data);
        }
    }

    private unsafe NOTIFYICONDATAW GetData()
    {
        var data = new NOTIFYICONDATAW
        {
            cbSize = (uint)sizeof(NOTIFYICONDATAW),
            uFlags =
                NOTIFY_ICON_DATA_FLAGS.NIF_TIP |
                NOTIFY_ICON_DATA_FLAGS.NIF_MESSAGE |
                NOTIFY_ICON_DATA_FLAGS.NIF_GUID |
                NOTIFY_ICON_DATA_FLAGS.NIF_ICON,
            szTip = Text,
            hIcon = new HICON(Icon?.Handle ?? nint.Zero),
            hWnd = window.Hwnd,
            uCallbackMessage = MESSAGE_ID,
            guidItem = Id,
            Anonymous =
            {
                uVersion = 5
            }
        };

        return data;
    }

    private void OnClick(EventArgs e)
    {
        Click?.Invoke(this, e);
    }

    private void Dispose(bool disposing)
    {
        Hide();

        if (icon != null && !keepIconAlive)
        {
            icon.Dispose();
        }

        window.Dispose();
    }

    private void ProcessMessage(uint messageId, WPARAM wParam, LPARAM lParam)
    {
        if (messageId == WM_TASKBARCREATED)
        {
            Show();
            return;
        }

        if (messageId != MESSAGE_ID)
        {
            return;
        }

        switch (lParam.Value)
        {
            case WM_LBUTTONUP:
            case WM_RBUTTONUP:
                OnClick(EventArgs.Empty);
                break;
        }
    }

    private sealed class NativeWindow
    {
        private readonly string windowId;
        private NotifyIcon owner;

        private WNDPROC proc;

        public unsafe NativeWindow(NotifyIcon owner)
        {
            ArgumentNullException.ThrowIfNull(owner);

            this.owner = owner;

            windowId = $"class:{owner.Id}";
            proc = OnWindowMessageReceived;

            fixed (char* className = windowId)
            {
                var classInfo = new WNDCLASSW()
                {
                    lpfnWndProc = proc,
                    lpszClassName = new PCWSTR(className),
                };

                PInvoke.RegisterClass(classInfo);

                Hwnd = PInvoke.CreateWindowEx(
                    dwExStyle: 0,
                    lpClassName: windowId,
                    lpWindowName: windowId,
                    dwStyle: 0,
                    X: 0,
                    Y: 0,
                    nWidth: 0,
                    nHeight: 0,
                    hWndParent: new HWND(IntPtr.Zero),
                    hMenu: null,
                    hInstance: null,
                    lpParam: null);
            }
        }

        ~NativeWindow()
        {
            Dispose(false);
        }

        public HWND Hwnd { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (Hwnd != HWND.Null)
            {
                PInvoke.DestroyWindow(hWnd: Hwnd);
                Hwnd = HWND.Null;

                PInvoke.UnregisterClass(
                    lpClassName: windowId,
                    hInstance: null);
            }
        }

        private LRESULT OnWindowMessageReceived(HWND hwnd, uint msg, WPARAM wParam, LPARAM lParam)
        {
            owner.ProcessMessage(msg, wParam, lParam);

            return PInvoke.DefWindowProc(
                hWnd: hwnd,
                Msg: msg,
                wParam: wParam,
                lParam: lParam);
        }
    }
}
