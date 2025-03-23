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
    private readonly WindowHelper windowHelper;

    private NotifyIcon notifyIcon;
    private NotifyContextMenuItem toggleVisibilityMenuItem;

    public MainWindow()
    {
        this.InitializeComponent();

        windowHelper = new WindowHelper(this);

        notifyIcon = new NotifyIcon(windowHelper)
        {
            Id = Guid.Parse("55DE2A49-087C-4331-A76C-6AA2212F3C39"),
            Text = "Click me",
            Icon = new LibIcon("shell32.dll", 130)
        };

        toggleVisibilityMenuItem = notifyIcon.ContextMenu.AddMenuItem("Hide");
        toggleVisibilityMenuItem.Click += (_, _) => ToggleVisibility();

        notifyIcon.ContextMenu.AddMenuItem("Check for updates...").IsEnabled = false;
        notifyIcon.ContextMenu.AddMenuItem("--");
        notifyIcon.ContextMenu.AddMenuItem("Exit").Click += (_, _) => App.Current.Exit();

        notifyIcon.Click += (_,_) => ToggleVisibility();
        notifyIcon.Show();

        Closed += (_, _) => notifyIcon?.Dispose();
    }

    private void UpdateToggleText()
    {
        if (toggleVisibilityMenuItem != null)
        {
            toggleVisibilityMenuItem.Text = AppWindow.IsVisible ? "Hide" : "Show";
        }
    }

    private void ToggleVisibility()
    {
        if (AppWindow.IsVisible)
        {           
            AppWindow.Hide();
        }
        else
        {
            AppWindow.Show();
        }

        UpdateToggleText();
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
