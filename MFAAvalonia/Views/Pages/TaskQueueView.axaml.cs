using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using MFAAvalonia.Configuration;
using MFAAvalonia.Helper;
using MFAAvalonia.Helper.ValueType;
using MFAAvalonia.ViewModels.Pages;
using MFAAvalonia.Views.UserControls;
using MFAWPF.Helper;
using SukiUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace MFAAvalonia.Views.Pages;

public partial class TaskQueueView : UserControl
{
    public TaskQueueView()
    {
        DataContext = Instances.TaskQueueViewModel;
        InitializeComponent();
    }

    private void SelectingItemsControl_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is ListBox { SelectedItem: DragItemViewModel itemViewModel })
        {
            itemViewModel.EnableSetting = true;
        }
    }

    private void OnPointerMoved(object sender, PointerEventArgs e)
    {
        if (e.GetCurrentPoint(sender as Control).Properties.IsLeftButtonPressed)
        {
            try
            {
                var listBox = (ListBox)sender;
                var data = new DataObject();
                data.Set(DataFormats.Text, listBox.SelectedIndex.ToString());
                DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
            }
            catch (COMException ex)
            {
                LoggerHelper.Error($"DragDrop failed: {ex.Message}");
            }
        }
    }

    private DragAdorner? _currentAdorner;

    private void OnDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.Get(DataFormats.Text) is string sourceIndexStr
            && int.TryParse(sourceIndexStr, out int sourceIndex)
            && DataContext is TaskQueueViewModel vm)
        {
            var listBox = (ListBox)sender;
            var targetIndex = GetTargetIndex(e.GetPosition(listBox), listBox, sourceIndex, out _, false);
            targetIndex = Math.Clamp(targetIndex, 0, vm.TaskItemViewModels.Count);
            bool end = targetIndex == vm.TaskItemViewModels.Count;
            if (end) targetIndex--;
            var container = listBox.ContainerFromIndex(targetIndex);
            var absolutePos = GetAbsolutePosition(container, AdornerLayer.GetAdornerLayer(listBox));
            if (end)
                absolutePos += new Point(0, container.Bounds.Height);
            var layer = AdornerLayer.GetAdornerLayer(listBox);
            if (_currentAdorner == null)
            {
                _currentAdorner = new DragAdorner(
                    absolutePos.X,
                    listBox.Bounds.Width, SukiTheme.GetInstance().ActiveColorTheme.PrimaryBrush
                );
                layer.Children.Add(_currentAdorner);
            }

            _currentAdorner.UpdatePosition(absolutePos.Y, targetIndex == 0, end);

            e.DragEffects = DragDropEffects.Move;
            e.Handled = true;
        }
    }

    private Point GetAbsolutePosition(Control item, Visual relativeTo)
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

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.Get(DataFormats.Text) is string sourceIndexStr
            && int.TryParse(sourceIndexStr, out int sourceIndex)
            && DataContext is TaskQueueViewModel vm)
        {
            var listBox = (ListBox)sender;
            var targetIndex = GetTargetIndex(e.GetPosition(listBox), listBox, sourceIndex, out var self);
            AdornerLayer.GetAdornerLayer(listBox).Children.Clear();
            _currentAdorner = null;
            targetIndex = Math.Clamp(targetIndex, 0, vm.TaskItemViewModels.Count - 1);
            if (sourceIndex != targetIndex && targetIndex >= 0 && !self && targetIndex < vm.TaskItemViewModels.Count)
            {
                try
                {
                    vm.TaskItemViewModels.Move(sourceIndex, targetIndex);
                }
                catch (Exception)
                {
                }
            }
        }
    }

    private void OnDragLeave(object sender, DragEventArgs e)
    {
        if (e.Data.Get(DataFormats.Text) is string sourceIndexStr)
        {
            var listBox = (ListBox)sender;
            AdornerLayer.GetAdornerLayer(listBox).Children.Clear();
            _currentAdorner = null;
        }
    }

    private int GetTargetIndex(Point position, ListBox listBox, int sourceIndex, out bool isSelf, bool shouldMove = true)
    {
        isSelf = false;
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
            isSelf = targetIndex == sourceIndex;
            var itemBounds = hitItem.Bounds;
            var result = (adjustedPosition.Y > itemBounds.Top + itemBounds.Height / 2)
                ? targetIndex + 1
                : targetIndex;
            if (targetIndex > sourceIndex && shouldMove)
            {
                result--;
            }
            return result;
        }

        var itemHeight = lastItem.Bounds.Height;
        isSelf = (int)(adjustedPosition.Y / itemHeight) == sourceIndex;
        var index = Convert.ToInt32(Math.Round(adjustedPosition.Y / itemHeight));
        return Math.Clamp(index, 0, listBox.ItemCount);
    }

    private void Delete(object? sender, RoutedEventArgs e)
    {
        var menuItem = sender as MenuItem;
        if (menuItem.DataContext is DragItemViewModel taskItemViewModel && DataContext is TaskQueueViewModel vm)
        {
            int index = vm.TaskItemViewModels.IndexOf(taskItemViewModel);
            vm.TaskItemViewModels.RemoveAt(index);
            // Instances.TaskOptionSettingsUserControl.SetOption(taskItemViewModel, false);
            ConfigurationManager.Current.SetValue(ConfigurationKeys.TaskItems, vm.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));

        }
    }
}
