namespace SingleInstance;

using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using SingleInstance.Services;

public partial class MainWindow : Window
{
    private readonly ISingleInstanceService singleInstanceService;
    private IDisposable disposable;

    public MainWindow(ISingleInstanceService singleInstanceService)
    {
        this.singleInstanceService = singleInstanceService;

        disposable = this.singleInstanceService
            .Activated
            .ObserveOn(SynchronizationContext.Current!)
            .Subscribe(x =>
            {
                log.Text += Environment.NewLine + "Activated: " + (x.IsEmpty ? "No args" : string.Join(',', x));
            });

        this.InitializeComponent();
        this.Closed += (_, _) => disposable?.Dispose();
    }
}
