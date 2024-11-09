namespace DependencyInjection.Services;

using Microsoft.UI.Xaml;

public interface IApp
{
    void Exit();

    event UnhandledExceptionEventHandler UnhandledException;
}
