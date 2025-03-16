namespace NotifyIcon;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using Windows.Win32;

public partial class MainWindow : Window
{
    private NotifyIcon notifyIcon;

    public MainWindow()
    {
        this.InitializeComponent();

        notifyIcon = new NotifyIcon()
        {
            Id = Guid.Parse("55DE2A49-087C-4331-A76C-6AA2212F3C39"),
            Text = "Click me",
            Icon = new LibIcon("shell32.dll", 130)
        };

        notifyIcon.Click += NotifyIcon_Click;
        notifyIcon.Show();

        Closed += (_, _) => notifyIcon?.Dispose();
    }

    private void NotifyIcon_Click(object? sender, EventArgs e)
    {
        if (AppWindow.IsVisible)
        {
            AppWindow.Hide();
        }
        else
        {
            AppWindow.Show();
        }
    }

    public sealed class LibIcon : IIconFile
    {
        private DestroyIconSafeHandle iconHandle;

        public LibIcon(string fileName, uint iconIndex)
        {
            iconHandle = PInvoke.ExtractIcon(fileName, iconIndex);
        }

        public nint Handle => iconHandle.DangerousGetHandle();

        public void Dispose()
        {
            if (!iconHandle.IsInvalid)
            {
                iconHandle?.Dispose();
            }
        }
    }
}
