namespace ContentDialogMvvm.Services;

using Microsoft.UI.Xaml;

public interface IApp
{
    XamlRoot XamlRoot { get; }

    void Exit();

    event UnhandledExceptionEventHandler UnhandledException;
}
