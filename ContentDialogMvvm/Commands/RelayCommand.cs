namespace ContentDialogMvvm.Commands; using System.Windows.Input;

public class RelayCommand : ICommand
{
    private readonly Action<object?> action;
    private readonly Func<object?, bool>? canExecute;

    public RelayCommand(Action<object?> action, Func<object?, bool>? canExecute = null)
    {
        this.action = action ?? throw new ArgumentNullException(nameof(action));
        this.canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return canExecute == null ? true : canExecute(parameter);
    }

    public void Execute(object? parameter)
    {
        action(parameter);
    }
}