namespace ContentDialogMvvm;

using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using ContentDialogMvvm.Services;
using ContentDialogMvvm.ViewModels;

public sealed partial class MainWindow : Window
{
    private IApp app;

    public MainWindow(IApp app, MainViewModel viewModel)
    {
        this.app = app ?? throw new ArgumentNullException(nameof(app));

        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        InitializeComponent();
    }

    public XamlRoot XamlRoot => Root.XamlRoot;

    public MainViewModel ViewModel { get; }
}
