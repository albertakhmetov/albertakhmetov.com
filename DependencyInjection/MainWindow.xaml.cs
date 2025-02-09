namespace DependencyInjection;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

public partial class MainWindow : Window
{
    private readonly IApp app;

    public MainWindow(IApp app)
    {
        this.app = app;

        this.InitializeComponent();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
        throw new Exception();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        app.Exit();
    }
}
