using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using ContentDialogMvvm.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ContentDialogMvvm.Views
{
    public sealed partial class IconSizeView : UserControl
    {
        public IconSizeView()
        {
            this.InitializeComponent();
        }

        // Workaround for the checkbox list
        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var i in e.AddedItems)
            {
                if (i is IconSizeViewModel.Item item)
                {
                    item.IsSelected = true;
                }
            }

            foreach (var i in e.RemovedItems)
            {
                if (i is IconSizeViewModel.Item item)
                {
                    item.IsSelected = false;
                }
            }
        }
    }
}
