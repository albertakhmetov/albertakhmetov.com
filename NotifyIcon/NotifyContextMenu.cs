namespace NotifyIcon;

using Microsoft.UI.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.AI.MachineLearning;

using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

class NotifyContextMenu : IDisposable
{
    private DestroyMenuSafeHandle handle;

    private readonly List<MenuItem> items = [];

    public NotifyContextMenu()
    {
        handle = PInvoke.CreatePopupMenu_SafeHandle();
    }

    public NotifyContextMenuItem this[int index] => items[index];

    public int Count => items.Count;

    public void Show(HWND hWnd, int x, int y)
    {
        var activeWindow = PInvoke.GetForegroundWindow();
        PInvoke.SetForegroundWindow(hWnd);
        try
        {
            var command = PInvoke.TrackPopupMenuEx(
                handle,
                (uint)(TRACK_POPUP_MENU_FLAGS.TPM_RETURNCMD | TRACK_POPUP_MENU_FLAGS.TPM_NONOTIFY),
                x,
                y,
                hWnd,
                null);

            if (command == 0)
            {
                return;
            }

            var item = items.FirstOrDefault(x => x.Id == command.Value);
            item?.PerformClick();
        }
        finally
        {
            PInvoke.SetForegroundWindow(activeWindow);
        }
    }

    public NotifyContextMenuItem AddMenuItem(string text)
    {
        var item = new MenuItem(this)
        {
            Text = text
        };

        var flags = MENU_ITEM_FLAGS.MF_STRING;
        if (text == "--")
        {
            flags |= MENU_ITEM_FLAGS.MF_SEPARATOR;
        }

        PInvoke.AppendMenu(handle, flags, item.Id, item.Text);

        items.Add(item);

        return item;
    }

    public void Dispose()
    {
        if (!handle.IsInvalid)
        {
            handle.Dispose();
        }
    }

    private void UpdateMenuItem(MenuItem item)
    {
        var flags = MENU_ITEM_FLAGS.MF_STRING;

        if (item.Text == "--")
        {
            flags |= MENU_ITEM_FLAGS.MF_SEPARATOR;
        }

        if (item.IsEnabled == false)
        {
            flags |= MENU_ITEM_FLAGS.MF_DISABLED;
        }

        PInvoke.ModifyMenu(handle, item.Id, flags, item.Id, item.Text);
    }

    sealed class MenuItem : NotifyContextMenuItem
    {
        private static uint idCount = 100;
        private NotifyContextMenu menu;
        private string text = string.Empty;
        private bool isEnabled = true;

        public MenuItem(NotifyContextMenu menu)
        {
            ArgumentNullException.ThrowIfNull(menu);

            this.menu = menu;

            Id = ++idCount;
        }

        public uint Id { get; }

        public override string Text
        {
            get => text;
            set
            {
                if (text == value)
                {
                    return;
                }

                text = value ?? string.Empty;
                menu.UpdateMenuItem(this);
            }
        }

        public override bool IsEnabled
        {
            get => isEnabled;
            set
            {
                if (isEnabled == value)
                {
                    return;
                }

                isEnabled = value;
                menu.UpdateMenuItem(this);
            }
        }

        public void PerformClick()
        {
            Click?.Invoke(this, EventArgs.Empty);
        }
    }
}
