namespace NotifyIcon;

using System;
using System.Collections.Immutable;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using WinRT.Interop;

public partial class App : Application
{
    [STAThread]
    public static void Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        Start(_ =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);

            instance = new App(args);
        });

        instance?.host?.StopAsync().Wait();
        instance?.host?.Dispose();
    }

    private static App? instance;

    private readonly ImmutableArray<string> arguments;
    private IHost? host;
    private MainWindow? mainWindow;

    public App(string[] args)
    {
        this.arguments = ImmutableArray.Create(args);

        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        host = CreateHost();

        mainWindow = host.Services.GetRequiredService<MainWindow>();
        mainWindow.Closed += OnMainWindowClosed;
        mainWindow.AppWindow.Show(true);

        _ = host.RunAsync();
    }

    private void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        Exit();
    }

    private IHost CreateHost()
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddSingleton<MainWindow>();

        return builder.Build();
    }
}