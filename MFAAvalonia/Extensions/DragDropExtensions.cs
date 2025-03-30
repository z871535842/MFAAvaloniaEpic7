using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit.Utils;
using MFAAvalonia.Helper;
using MFAAvalonia.ViewModels.Pages;
using MFAAvalonia.Views.UserControls;
using Microsoft.VisualBasic;
using SukiUI;
using SukiUI.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MFAAvalonia.Extensions;

public class DragDropExtensions
{
    // 定义附加属性：是否启用拖放功能
    public static readonly AttachedProperty<bool> EnableFileDragDropProperty =
        AvaloniaProperty.RegisterAttached<TextBox, bool>(
            "EnableDragDrop",
            typeof(DragDropExtensions),
            defaultValue: false,
            inherits: false);

    // 获取附加属性值
    public static bool GetEnableFileDragDrop(TextBox textBox) =>
        textBox.GetValue(EnableFileDragDropProperty);

    // 设置附加属性值
    public static void SetEnableFileDragDrop(TextBox textBox, bool value) =>
        textBox.SetValue(EnableFileDragDropProperty, value);

    // 当附加属性值变化时触发
    private static void OnEnableDragDropChanged(AvaloniaPropertyChangedEventArgs<bool> args)
    {
        if (args.Sender is TextBox textBox)
        {
            if (args.NewValue.Value)
            {
                textBox.AddHandler(DragDrop.DragOverEvent, File_DragOver);
                textBox.AddHandler(DragDrop.DropEvent, File_Drop);
            }
            else
            {
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

    public static readonly AttachedProperty<bool> EnableDragDropProperty =
        AvaloniaProperty.RegisterAttached<ListBox, bool>(
            "EnableDragDrop",
            typeof(DragDropExtensions),
            defaultValue: false);

    // 存储当前拖拽项的私有属性
    private static readonly AttachedProperty<DragAdorner?> DragAdornerProperty =
        AvaloniaProperty.RegisterAttached<ListBox, DragAdorner?>(
            "DragAdorner",
            typeof(DragDropExtensions));

    public static readonly AttachedProperty<bool> EnableAnimationProperty =
        AvaloniaProperty.RegisterAttached<ListBox, bool>(
            "EnableAnimation",
            typeof(DragDropExtensions),
            defaultValue: false);

    public static readonly AttachedProperty<double> AnimationDurationProperty =
        AvaloniaProperty.RegisterAttached<ListBox, double>(
            "AnimationDuration",
            typeof(DragDropExtensions),
            defaultValue: 400.0);

    static DragDropExtensions()
    {
        EnableFileDragDropProperty.Changed.Subscribe(OnEnableDragDropChanged);
        EnableDragDropProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is ListBox listBox)
            {
                if (args.NewValue.Value)
                {
                    EnableDragDrop(listBox);
                }
                else
                {
                    DisableDragDrop(listBox);
                }
            }
        });
    }
    public static bool GetEnableAnimation(ListBox element) =>
        element.GetValue(EnableAnimationProperty);

    public static void SetEnableAnimation(ListBox element, bool value) =>
        element.SetValue(EnableAnimationProperty, value);

    public static bool GetEnableDragDrop(ListBox element) =>
        element.GetValue(EnableDragDropProperty);

    public static void SetEnableDragDrop(ListBox element, bool value) =>
        element.SetValue(EnableDragDropProperty, value);


    public static double GetAnimationDuration(ListBox element) =>
        element.GetValue(AnimationDurationProperty);

    public static void SetAnimationDuration(ListBox element, double value) =>
        element.SetValue(AnimationDurationProperty, value switch
        {
            >= 150 => value,
            _ => throw new ArgumentOutOfRangeException($"AnimationDuration must be greater than or equal to 150, but was {value}")
        });

    private static void EnableDragDrop(ListBox listBox)
    {
        if (listBox.Parent is not AdornerLayer adornerLayer)
        {
            adornerLayer = new AdornerLayer();
            if (listBox.Parent is Panel panel)
            {
                panel.Children.Remove(listBox);
                panel.Children.Add(adornerLayer);

            }
            else if (listBox.Parent is ContentControl control)
            {
                control.Content = null;
                control.Content = adornerLayer;
            }
            else if (listBox.Parent is Border border)
            {
                border.Child = null;
                border.Child = adornerLayer;
            }

            AdornerLayer.SetAdornedElement(adornerLayer, listBox);

        }
        adornerLayer.Children.Add(listBox);
        listBox.AddHandler(ListBox.PointerMovedEvent, OnPointerMoved);
        listBox.AddHandler(DragDrop.DragOverEvent, OnDragOver);
        listBox.AddHandler(DragDrop.DropEvent, OnDrop);
        listBox.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
    }

    private static void DisableDragDrop(ListBox listBox)
    {
        listBox.RemoveHandler(ListBox.PointerMovedEvent, OnPointerMoved);
        listBox.RemoveHandler(DragDrop.DragOverEvent, OnDragOver);
        listBox.RemoveHandler(DragDrop.DropEvent, OnDrop);
        listBox.RemoveHandler(DragDrop.DragLeaveEvent, OnDragLeave);
    }

    private static void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not ListBox listBox || !e.GetCurrentPoint(listBox).Properties.IsLeftButtonPressed)
            return;
        var sourceItem = GetSourceIndex(listBox, e.GetPosition(listBox), -1);
        if (sourceItem == -1) return;

        var data = new DataObject();
        listBox.SelectedIndex = Math.Clamp(sourceItem, 0, listBox.Items.Count - 1);
        data.Set(DataFormats.Text, sourceItem.ToString());

        DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
    }

    private static void OnDragOver(object? sender, DragEventArgs e)
    {
        if (sender is not ListBox listBox || listBox.ItemsSource is not IList items || e.Data.Get(DataFormats.Text) is not string sourceIndexStr || !int.TryParse(sourceIndexStr, out int sourceIndex))
            return;
        var position = e.GetPosition(listBox);
        var targetIndex = GetTargetIndex(listBox, position);

        if (targetIndex == -1 || sourceIndex == -1) return;

        UpdateAdorner(listBox, targetIndex, items.Count);

        e.DragEffects = DragDropEffects.Move;
        e.Handled = true;
    }

    private static void OnDrop(object? sender, DragEventArgs e)
    {
        if (sender is not ListBox listBox || listBox.ItemsSource is not IList items || e.Data.Get(DataFormats.Text) is not string sourceIndexStr || !int.TryParse(sourceIndexStr, out int sourceIndex))
            return;
        var position = e.GetPosition(listBox);
        var targetIndex = GetTargetIndex(listBox, position);

        if (sourceIndex >= 0 && targetIndex >= 0 && sourceIndex != targetIndex)
        {
            if (GetEnableAnimation(listBox))
            {
                MoveWithAnimation(listBox, items, sourceIndex, targetIndex);
            }
            else
            {
                items.MoveTo(sourceIndex, targetIndex);
            }
        }
        ClearAdorner(listBox);
    }

    async private static Task AnimateItemMovement(ListBoxItem item, double startY, double endY, double duration = 400)
    {
        var completionSource = new TaskCompletionSource<bool>();
        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(duration),
            Easing = new CubicEaseOut(),
            Children =
            {
                new KeyFrame
                {
                    KeyTime = TimeSpan.Zero,
                    Setters =
                    {
                        new Setter(TranslateTransform.YProperty, startY)
                    }
                },
                new KeyFrame
                {
                    KeyTime = TimeSpan.FromMilliseconds(duration - 3),
                    Setters =
                    {
                        new Setter(TranslateTransform.YProperty, endY)
                    }
                },
                new KeyFrame
                {
                    KeyTime = TimeSpan.FromMilliseconds(duration),
                    Setters =
                    {
                        new Setter(TranslateTransform.YProperty, endY)
                    }
                }
            }
        };
        animation.RunAsync(item).ContinueWith(t =>
        {
            completionSource.SetResult(true);
        });

        await completionSource.Task;
    }

    async private static Task MoveWithAnimation(ListBox listBox, IList items, int sourceIndex, int targetIndex)
    {
        var affectedItems = GetAffectedItems(listBox, sourceIndex, targetIndex);
        await RunPreUpdateAnimations(listBox, affectedItems, () =>
        {
            items.MoveTo(sourceIndex, targetIndex);
        });
    }

    private class ListBoxItemAndIndex(ListBoxItem? item, int sourceIndex, int targetIndex)
    {
        public ListBoxItem? ListBoxItem => item;
        public int SourceIndex => sourceIndex;
        public int TargetIndex => targetIndex;
    }

    private static IEnumerable<ListBoxItemAndIndex> GetAffectedItems(
        ListBox listBox,
        int sourceIndex,
        int targetIndex)
    {
        listBox.UpdateLayout();
        Dispatcher.UIThread.RunJobs();

        var start = Math.Min(sourceIndex, targetIndex);
        var end = Math.Max(sourceIndex, targetIndex);

        var increment = sourceIndex < targetIndex ? -1 : +1;

        var affectedItems = new List<ListBoxItemAndIndex>();
        if (sourceIndex < targetIndex)
            targetIndex--;

        affectedItems.Add(new ListBoxItemAndIndex(listBox.ContainerFromIndex(sourceIndex) as ListBoxItem, sourceIndex, targetIndex));
        foreach (var i in Enumerable.Range(start, end - start))
        {
            if (i != sourceIndex)
                affectedItems.Add(new ListBoxItemAndIndex(listBox.ContainerFromIndex(i) as ListBoxItem, i, i + increment));
        }
        return affectedItems;
    }

    async private static Task RunPreUpdateAnimations(ListBox listBox,
        IEnumerable<ListBoxItemAndIndex> items,
        Action action)
    {
        var animations = items.Select(item =>
        {
            var startY = listBox.ContainerFromIndex(item.SourceIndex).Bounds.Y;
            var endY = listBox.ContainerFromIndex(item.TargetIndex).Bounds.Y;

            return AnimateItemMovement(item.ListBoxItem, 0, endY - startY, GetAnimationDuration(listBox));
        }).ToList();
        var delayTask = Task.Delay((int)GetAnimationDuration(listBox) - 2).ContinueWith(_ => action());
        animations.Add(delayTask);
        await Task.WhenAll(animations);
    }

    private static void OnDragLeave(object? sender, DragEventArgs e)
    {
        if (sender is ListBox listBox)
        {
            ClearAdorner(listBox);
        }
    }

    private static int GetSourceIndex(ListBox listBox, Point position, int defaultValue)
    {
        var scrollViewer = listBox.GetVisualDescendants()
            .OfType<ScrollViewer>()
            .FirstOrDefault();
        var items = listBox.GetVisualDescendants()
            .OfType<ListBoxItem>()
            .ToList();

        var adjustedPosition = position + (scrollViewer?.Offset ?? new Vector(0, 0));

        var hitControl = listBox.InputHitTest(adjustedPosition) as Visual;

        var hitItem = hitControl?
            .GetVisualAncestors()
            .OfType<ListBoxItem>()
            .FirstOrDefault();

        if (hitItem != null)
        {
            var targetIndex = listBox.IndexFromContainer(hitItem);
            return targetIndex;
        }

        return defaultValue;
    }

    private static int GetTargetIndex(ListBox listBox, Point position)
    {
        var scrollViewer = listBox.GetVisualDescendants()
            .OfType<ScrollViewer>()
            .FirstOrDefault();
        var items = listBox.GetVisualDescendants()
            .OfType<ListBoxItem>()
            .ToList();
        var firstItem = items.FirstOrDefault();
        var lastItem = items.LastOrDefault();

        if (firstItem == null) return -1;

        var adjustedPosition = position + (scrollViewer?.Offset ?? new Vector(0, 0));

        var hitControl = listBox.InputHitTest(adjustedPosition) as Visual;

        if (hitControl == null && position.Y > lastItem.Bounds.Y) return listBox.ItemCount;

        var hitItem = hitControl?
            .GetVisualAncestors()
            .OfType<ListBoxItem>()
            .FirstOrDefault();

        if (hitItem != null)
        {
            var targetIndex = listBox.IndexFromContainer(hitItem);
            var itemBounds = hitItem.Bounds;
            var result = (adjustedPosition.Y > itemBounds.Top + itemBounds.Height / 2)
                ? targetIndex + 1
                : targetIndex;
            return result;
        }

        var itemHeight = lastItem.Bounds.Height;
        var index = Convert.ToInt32(Math.Round(adjustedPosition.Y / itemHeight));
        return Math.Clamp(index, 0, listBox.ItemCount);
    }


    // 修改装饰器更新逻辑
    private static void UpdateAdorner(ListBox listBox, int index, int count)
    {
        var adornerLayer = AdornerLayer.GetAdornerLayer(listBox);
        if (adornerLayer == null) return;
        var end = index == count;
        if (end) index--;
        var container = listBox.ContainerFromIndex(index);
        var absolutePos = GetAbsolutePosition(container, adornerLayer);
        if (end)
            absolutePos += new Point(0, container.Bounds.Height);

        if (listBox.GetValue(DragAdornerProperty) is not DragAdorner adorner)
        {
            adorner = new DragAdorner(
                absolutePos.X,
                listBox.Bounds.Width, SukiTheme.GetInstance().ActiveColorTheme.PrimaryBrush
            );
            listBox.SetValue(DragAdornerProperty, adorner);
        }
        if (!adornerLayer.Children.Contains(adorner))
            adornerLayer.Children.Add(adorner);
        adorner.UpdatePosition(absolutePos.Y, index == 0, end);
    }

    private static Point GetAbsolutePosition(Control item, Visual relativeTo)
    {
        // 递归计算所有父容器的偏移
        var position = item.TranslatePoint(new Point(0, 0), relativeTo) ?? new Point(0, 0);

        // 处理滚动偏移
        var scrollViewer = item.GetVisualParent()?.GetVisualDescendants()
            .OfType<ScrollViewer>()
            .FirstOrDefault();

        return position
            + new Point(
                scrollViewer?.Offset.X ?? 0,
                scrollViewer?.Offset.Y ?? 0
            );
    }

    private static void ClearAdorner(ListBox listBox)
    {
        var adornerLayer = AdornerLayer.GetAdornerLayer(listBox);
        if (listBox.GetValue(DragAdornerProperty) is DragAdorner adorner)
        {
            adornerLayer?.Children.Remove(adorner);
        }
    }
}
