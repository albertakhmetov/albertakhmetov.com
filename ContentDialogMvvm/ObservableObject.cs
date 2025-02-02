namespace ContentDialogMvvm; 

using System.ComponentModel;
using System.Runtime.CompilerServices;

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool Set<T>(ref T property, T value, [CallerMemberName] string? propertyName = null)
    {
        if (property is IEquatable<T> equatable && equatable.Equals(value))
        {
            return false;
        }

        property = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        return true;
    }
}