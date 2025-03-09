namespace Awake;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Win32.System.Power;

public partial class MainWindow : Window
{
    public MainWindow(IServiceProvider serviceProvider)
    {
        this.InitializeComponent();
    }

    private void toggle_Click(object sender, RoutedEventArgs e)
    {
        var state = EXECUTION_STATE.ES_CONTINUOUS;
        if (toggle.IsChecked == true)
        {
            state |= EXECUTION_STATE.ES_DISPLAY_REQUIRED;
        }

        Windows.Win32.PInvoke.SetThreadExecutionState(state);
    }
}
