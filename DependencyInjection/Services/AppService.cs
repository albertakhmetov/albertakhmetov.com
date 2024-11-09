namespace DependencyInjection.Services;

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

internal class AppService : IHostedService
{
    private readonly ILogger<AppService> logger;
    private readonly IServiceProvider serviceProvider;
    private readonly IHostApplicationLifetime appLifetime;

    private IApp? app;
    private Task? appTask;

    public AppService(
        ILogger<AppService> logger,
        IServiceProvider serviceProvider,
        IHostApplicationLifetime appLifetime)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        this.appLifetime = appLifetime ?? throw new ArgumentNullException(nameof(appLifetime));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        XamlCheckProcessRequirements();
        WinRT.ComWrappersSupport.InitializeComWrappers();

        appTask = Task.Factory
            .StartNew(() => Application.Start(InitAndStartApp))
            .ContinueWith(_ => OnAppClosed());

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        app?.Exit();

        return Task.CompletedTask;
    }

    private void InitAndStartApp(ApplicationInitializationCallbackParams p)
    {
        try
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);

            app = serviceProvider.GetRequiredService<IApp>();
            app.UnhandledException += OnAppOnUnhandledException;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the application");
        }
    }

    private void OnAppClosed()
    {
        appTask = null;

        if (app != null)
        {
            app.UnhandledException -= OnAppOnUnhandledException;
            app = null;
        }

        appLifetime.StopApplication();
    }

    private void OnAppOnUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        logger.LogError(args.Exception, "An unhandled application exception occured");

        args.Handled = false;
    }

    [DllImport("microsoft.ui.xaml.dll")]
    private static extern void XamlCheckProcessRequirements();
}
