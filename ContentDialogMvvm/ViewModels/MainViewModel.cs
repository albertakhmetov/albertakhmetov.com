namespace ContentDialogMvvm.ViewModels;

using System.Windows.Input;
using ContentDialogMvvm.Commands;
using ContentDialogMvvm.Services;
using Microsoft.Extensions.DependencyInjection;

public class MainViewModel : ObservableObject
{
    private string text = "";

    public MainViewModel(IApp app, IDialogService dialogService, IServiceProvider serviceProvider)
    {
        ShowDialogCommand = new RelayCommand(async _ =>
        {
            var model = serviceProvider.GetRequiredService<IconSizeViewModel>();

            if (await dialogService.Show(model) == true)
            {
                Text = $"Ok is clicked. Selected: {string.Join(", ", model.Items.Where(i => i.IsSelected).Select(i => i.Text))}";
            }
            else
            {
                Text = "Cancel is clicked";
            }
        });

        ExitCommand = new RelayCommand(_ => app.Exit());
    }

    public string Text
    {
        get => text;
        private set => Set(ref text, value);
    }

    public ICommand ShowDialogCommand { get; }

    public ICommand ExitCommand { get; }
}
