namespace ContentDialogMvvm.Services;

using System;
using System.Threading.Tasks;
using ContentDialogMvvm.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

internal class DialogService : IDialogService
{
    private readonly IServiceProvider serviceProvider;

    public DialogService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task<bool> Show(DialogViewModel viewModel)
    {
        ContentDialog dialog = new ContentDialog
        {
            DataContext = viewModel,
            Content = serviceProvider.GetRequiredKeyedService<UserControl>(viewModel.GetType().Name),
            XamlRoot = serviceProvider.GetRequiredService<IApp>().XamlRoot
        };

        BindProperty(
            dialog,
            ContentDialog.PrimaryButtonCommandProperty,
            nameof(DialogViewModel.PrimaryCommand),
            BindingMode.OneTime);

        BindProperty(
            dialog,
            ContentDialog.IsPrimaryButtonEnabledProperty,
            nameof(DialogViewModel.IsPrimaryEnabled),
            BindingMode.OneWay);

        BindProperty(
            dialog,
            ContentDialog.PrimaryButtonTextProperty,
            nameof(DialogViewModel.PrimaryText),
            BindingMode.OneWay);

        BindProperty(
            dialog,
            ContentDialog.CloseButtonTextProperty,
            nameof(DialogViewModel.CloseText),
            BindingMode.OneWay);

        var result = await dialog.ShowAsync();

        dialog.Content = null;

        return result == ContentDialogResult.Primary;
    }

    private void BindProperty(
        ContentDialog contentDialog,
        DependencyProperty property,
        string path,
        BindingMode mode)
    {
        contentDialog.SetBinding(
            property,
            new Binding
            {
                Path = new PropertyPath(path),
                Mode = mode
            });
    }
}
