using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using MFAAvalonia.Helper;
using System.Linq;

namespace MFAAvalonia.Views.UserControls.Settings;

public partial class GameSettingsUserControl : UserControl
{
    public GameSettingsUserControl()
    {
        DataContext = Instances.GameSettingsUserControlModel;
        InitializeComponent();
    }
    
    private void File_DragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;
    }
    
    private void File_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.Files))
        {
            return;
        }
        
        var storageItems = e.Data.GetFiles()?.ToList(); // 获取 IStorageItem 集合
        if (storageItems?.Count > 0 && sender is TextBox textBox)
        {
            var firstFile = storageItems[0].TryGetLocalPath(); // 提取本地路径
            textBox.Text = firstFile ?? string.Empty;
        }
    }
}

