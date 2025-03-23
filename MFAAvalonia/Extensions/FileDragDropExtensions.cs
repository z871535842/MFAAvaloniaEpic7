using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using AvaloniaEdit.Utils;
using System.Linq;

namespace MFAAvalonia.Extensions;

public class FileDragDropExtensions
{
    // 定义附加属性：是否启用拖放功能
    public static readonly AttachedProperty<bool> EnableFileDragDropProperty =
        AvaloniaProperty.RegisterAttached<TextBox, bool>(
            "EnableDragDrop",
            typeof(FileDragDropExtensions),
            defaultValue: false,
            inherits: false);

    // 获取附加属性值
    public static bool GetEnableFileDragDrop(TextBox textBox) =>
        textBox.GetValue(EnableFileDragDropProperty);

    // 设置附加属性值
    public static void SetEnableFileDragDrop(TextBox textBox, bool value) =>
        textBox.SetValue(EnableFileDragDropProperty, value);

    // 初始化附加属性
    static FileDragDropExtensions()
    {
        EnableFileDragDropProperty.Changed.Subscribe(OnEnableDragDropChanged);
    }

    // 当附加属性值变化时触发
    private static void OnEnableDragDropChanged(AvaloniaPropertyChangedEventArgs<bool> args)
    {
        if (args.Sender is TextBox textBox)
        {
            if (args.NewValue.Value)
            {
                // 启用拖放功能
                DragDrop.SetAllowDrop(textBox, true);
                textBox.AddHandler(DragDrop.DragOverEvent, File_DragOver);
                textBox.AddHandler(DragDrop.DropEvent, File_Drop);
            }
            else
            {
                // 禁用拖放功能
                textBox.RemoveHandler(DragDrop.DragOverEvent, File_DragOver);
                textBox.RemoveHandler(DragDrop.DropEvent, File_Drop);
            }
        }
    }

    // 拖放事件处理：拖拽经过时
    private static void File_DragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;
    }

    // 拖放事件处理：文件拖放时
    private static void File_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.Files))
        {
            return;
        }

        var storageItems = e.Data.GetFiles()?.ToList();
        if (storageItems?.Count > 0 && sender is TextBox textBox)
        {
            var firstFile = storageItems[0].TryGetLocalPath();
            textBox.Text = firstFile ?? string.Empty;
        }
    }
}
