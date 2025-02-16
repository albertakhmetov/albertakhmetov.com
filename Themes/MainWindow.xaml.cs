namespace Themes;

using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Dwm;

public partial class MainWindow : Window
{
    private readonly SystemEventsService systemEventsService;
    private readonly BehaviorSubject<Theme> themeSubject;
    
    private IDisposable disposable;

    public MainWindow(SystemEventsService systemEventsService)
    {
        this.systemEventsService = systemEventsService;
        this.InitializeComponent();

        themeSubject = new BehaviorSubject<Theme>(Theme.System);

        disposable = ListenForApp();
    }

    private IDisposable ListenForApp()
    {
        return themeSubject
            .CombineLatest(systemEventsService.AppDarkTheme.Select(x => x))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(x => UpdateTheme(x.First == Theme.System ? x.Second : (x.First == Theme.Dark)));
    }

    private IDisposable ListenForSystem()
    {
        return themeSubject
            .CombineLatest(systemEventsService.SystemDarkTheme.Select(x => x))
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(x => UpdateTheme(x.First == Theme.System ? x.Second : (x.First == Theme.Dark)));
    }

    private unsafe void UpdateTheme(bool isDarkTheme)
    {
        var hwnd = new HWND(WinRT.Interop.WindowNative.GetWindowHandle(this));

        var isDark = isDarkTheme ? 1 : 0;

        // Set the theme for the title bar
        var result = PInvoke.DwmSetWindowAttribute(
            hwnd: hwnd,
            dwAttribute: DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
            pvAttribute: Unsafe.AsPointer(ref isDark),
            cbAttribute: sizeof(int));

        if (result != 0)
        {
            throw Marshal.GetExceptionForHR(result) ?? throw new ApplicationException("Can't switch dark mode setting");
        }

        // Set the theme for the content
        if (Content is FrameworkElement element)
        {
            element.RequestedTheme = isDarkTheme ? ElementTheme.Dark : ElementTheme.Light;
        }
    }

    private enum Theme
    {
        System,
        Light,
        Dark
    }

    private void Dark_Click(object sender, RoutedEventArgs e)
    {
        themeSubject.OnNext(Theme.Dark);
    }

    private void Light_Click(object sender, RoutedEventArgs e)
    {
        themeSubject.OnNext(Theme.Light);
    }

    private void App_Click(object sender, RoutedEventArgs e)
    {
        disposable?.Dispose();
        disposable = ListenForApp();
        themeSubject.OnNext(Theme.System);
    }

    private void System_Click(object sender, RoutedEventArgs e)
    {
        disposable?.Dispose();
        disposable = ListenForSystem();
        themeSubject.OnNext(Theme.System);
    }
}
