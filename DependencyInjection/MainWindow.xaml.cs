namespace DependencyInjection;

using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using DependencyInjection.Services;

public sealed partial class MainWindow : Window
{
    private IApp app;

    public MainWindow(IApp app)
    {
        this.app = app ?? throw new ArgumentNullException(nameof(app));

        InitializeComponent();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        app.Exit();
    }
}
