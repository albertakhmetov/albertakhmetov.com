namespace ContentDialogMvvm.ViewModels;

using System;

public class IconSizeViewModel : DialogViewModel
{
    public IconSizeViewModel()
    {
        IsPrimaryEnabled = false;

        Items = new Item[] {
            new(this) { Text="32x32" },
            new(this) { Text="48x48" },
            new(this) { Text="64x64" },
            new(this) { Text="256x256" },
        }.AsReadOnly();
    }

    public IReadOnlyCollection<Item> Items { get; }

    protected override void OnPrimaryExecuted(object? parameter)
    {
        ; // primary button is clicked
    }

    private void OnItemSelected()
    {
        IsPrimaryEnabled = Items.Any(i => i.IsSelected);
    }

    public sealed class Item : ObservableObject
    {
        private readonly IconSizeViewModel owner;

        private bool isSelected = false;
        private string? text;

        public Item(IconSizeViewModel owner)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (Set(ref isSelected, value))
                {
                    owner.OnItemSelected();
                }
            }
        }

        public string? Text
        {
            get => text;
            set => Set(ref text, value);
        }
    }
}
