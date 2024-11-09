namespace DependencyInjection;

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using DependencyInjection.Services;
using WinRT.Interop;

public partial class App : Application, IApp
{
    public static void Main(string[] args)
    {
        var builder = CreateBuilder(args);

        var host = builder.Build();
        host.Run();
    }

    private static HostApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSingleton<IApp, App>();
        builder.Services.AddSingleton<MainWindow>();

        builder.Services.AddHostedService<AppService>();

        return builder;
    }

    private readonly IServiceProvider serviceProvider;
    private MainWindow? mainWindow;

    public App(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        mainWindow = serviceProvider.GetRequiredService<MainWindow>();
        // The application exits when the main window closes.
        mainWindow.Closed += OnMainWindowClosed;
        mainWindow.AppWindow.Show(true);
    }

    private void OnMainWindowClosed(object sender, WindowEventArgs args)
    {
       Exit();
    }
}
