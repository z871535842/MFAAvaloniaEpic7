using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using MFAAvalonia.Extensions;
using MFAAvalonia.Helper;
using MFAAvalonia.Helper.ValueType;
using MFAAvalonia.ViewModels.UsersControls.Settings;
using System;

namespace MFAAvalonia.Views.UserControls;

public partial class AddTaskDialogView : UserControl
{
    public AddTaskDialogView()
    {
        InitializeComponent();
    }

    private void SearchBar_OnSearchStarted(object sender, RoutedEventArgs e)
    {
        string key = SearchBar.Text;

        if (DataContext is AddTaskDialogViewModel vm)
        {
            if (string.IsNullOrEmpty(key))
            {

                vm.Items.Clear();
                vm.Items.AddRange(vm.Sources);
            }
            else
            {
                key = key.ToLower();
                vm.Items.Clear();
                foreach (DragItemViewModel item in vm.Sources)
                {
                    string name = item.Name.ToLower();
                    if (name.Contains(key))
                        vm.Items.Add(item);
                }
            }
        }

    }
}
