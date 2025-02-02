namespace ContentDialogMvvm.Services;

using System;
using ContentDialogMvvm.ViewModels;

public interface IDialogService
{
    Task<bool> Show(DialogViewModel viewModel);
}
