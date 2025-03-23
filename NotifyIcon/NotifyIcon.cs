namespace NotifyIcon;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement.Core;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.WindowsAndMessaging;

class NotifyIcon
{
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONUP = 0x0205;
    private const int WM_CONTEXTMENU = 0x007B;

    private const uint MESSAGE_ID = 5800;
    private static readonly uint WM_TASKBARCREATED = PInvoke.RegisterWindowMessage("TaskbarCreated");

    private readonly WindowHelper windowHelper;

    private string? text;
    private IIconFile? icon;

    private bool keepIconAlive;

    public NotifyIcon(WindowHelper windowHelper, bool keepIconAlive = false)
    {
        ArgumentNullException.ThrowIfNull(windowHelper);

        this.windowHelper = windowHelper;
        this.keepIconAlive = keepIconAlive;

        ContextMenu = new NotifyContextMenu();

        this.windowHelper.Message += ProcessMessage;
    }

    ~NotifyIcon()
    {
        Dispose(disposing: false);
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

    public NotifyContextMenu ContextMenu { get; }

    public event EventHandler<NotifyIconEventArgs>? Click;

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
            hWnd = windowHelper.Handle,
            uCallbackMessage = MESSAGE_ID,
            guidItem = Id,
            Anonymous =
            {
                uVersion = 5
            }
        };

        return data;
    }

    private void OnClick(NotifyIconEventArgs e)
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
    }

    private void ProcessMessage(uint messageId, uint wParam, int lParam)
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

        switch (lParam)
        {
            case WM_LBUTTONUP:
                OnClick(new NotifyIconEventArgs { Rect = GetIconRectangle() });
                break;

            case WM_CONTEXTMENU:
            case WM_RBUTTONUP:
                ShowContextMenu();
                break;
        }
    }

    private void ShowContextMenu()
    {
        var rect = GetIconRectangle();

        ContextMenu.Show(
            windowHelper.Handle,
            (int)(rect.Left + rect.Right) / 2,
            (int)(rect.Top + rect.Bottom) / 2);
    }

    private unsafe Windows.Foundation.Rect GetIconRectangle()
    {
        var notifyIcon = new NOTIFYICONIDENTIFIER
        {
            cbSize = (uint)sizeof(NOTIFYICONIDENTIFIER),
            hWnd = windowHelper.Handle,
            guidItem = Id
        };

        return PInvoke.Shell_NotifyIconGetRect(notifyIcon, out var rect) != 0
            ? Windows.Foundation.Rect.Empty
            : new Windows.Foundation.Rect(
                rect.left,
                rect.top,
                rect.right - rect.left,
                rect.bottom - rect.top);
    }
}
