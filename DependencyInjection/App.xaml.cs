namespace DependencyInjection;

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

public partial class App : Application, IApp
{
    [STAThread]
    public static void Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();

        try
        {
            Start(_ =>
            {
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);

                var app = new App(args);
                app.UnhandledException += (_, _) => StopHost();

                host = CreateHost(app);
            });
        }
        finally
        {
            StopHost();
        }
    }

    private static IHost CreateHost(IApp app)
    {
        var builder = Host.CreateApplicationBuilder();

        builder.Services.AddSingleton<IApp>(app);
        builder.Services.AddSingleton<MainWindow>();

        return builder.Build();
    }

    private static void StopHost()
    {
        host?.StopAsync().Wait();
        host?.Dispose();
    }

    private static IHost? host;

    private readonly ImmutableArray<string> arguments;
    private MainWindow? mainWindow;

    public App(string[] args)
    {
        this.arguments = ImmutableArray.Create(args);

        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        // The host should already be created by this time.
        if (host == null)
        {
            throw new InvalidOperationException();
        }

        base.OnLaunched(args);

        mainWindow = host.Services.GetRequiredService<MainWindow>();
        mainWindow.Closed += OnMainWindowClosed;
        mainWindow.AppWindow.Show(true);

        _ = host.RunAsync();
    }

    private void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
        Exit();
    }
}