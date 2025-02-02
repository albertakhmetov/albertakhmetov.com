namespace ContentDialogMvvm.ViewModels;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ContentDialogMvvm.Commands;

public abstract class DialogViewModel : ObservableObject
{
    private bool isPrimaryEnabled;
    private string primaryText, closeText;

    protected DialogViewModel()
    {
        PrimaryCommand = new RelayCommand(OnPrimaryExecuted);

        isPrimaryEnabled = true;
        primaryText = "Ok";
        closeText = "Cancel";
    }

    public ICommand PrimaryCommand { get; }

    public bool IsPrimaryEnabled
    {
        get => isPrimaryEnabled;
        protected set => Set(ref isPrimaryEnabled, value);
    }

    public string PrimaryText
    {
        get => primaryText;
        protected set => Set(ref primaryText, value);
    }

    public string CloseText
    {
        get => closeText;
        protected set => Set(ref closeText, value);
    }

    protected abstract void OnPrimaryExecuted(object? parameter);
}
