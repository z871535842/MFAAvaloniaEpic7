using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Diagnostics;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Interactivity;
using Avalonia.Layout;

namespace MFAAvalonia.Extensions;

public static class ScrollViewerExtensions
{
    // 滚动方向控制
    public static readonly AttachedProperty<PanningMode> PanningModeProperty =
        AvaloniaProperty.RegisterAttached<ScrollViewer, PanningMode>(
            "PanningMode", typeof(ScrollViewerExtensions), PanningMode.Both);

    // 自动滚动控制
    public static readonly AttachedProperty<bool> AutoScrollProperty =
        AvaloniaProperty.RegisterAttached<ScrollViewer, bool>(
            "AutoScroll", typeof(ScrollViewerExtensions), false);

    static ScrollViewerExtensions()
    {
        PanningModeProperty.Changed.AddClassHandler<ScrollViewer>(
          OnPanningModeChanged);

        AutoScrollProperty.Changed.AddClassHandler<ScrollViewer>(
            OnAutoScrollChanged);
    }

    
    #region 属性设置器

    public static void SetPanningMode(ScrollViewer element, PanningMode value) =>
        element.SetValue(PanningModeProperty, value);

    public static PanningMode GetPanningMode(ScrollViewer element) =>
        element.GetValue(PanningModeProperty);

    public static void SetAutoScroll(ScrollViewer element, bool value) =>
        element.SetValue(AutoScrollProperty, value);

    public static bool GetAutoScroll(ScrollViewer element) =>
        element.GetValue(AutoScrollProperty);

    #endregion

    #region 逻辑处理

    private static void OnPanningModeChanged(ScrollViewer scrollViewer, AvaloniaPropertyChangedEventArgs args)
    {
        scrollViewer.HorizontalScrollBarVisibility = args.NewValue switch
        {
            PanningMode.VerticalOnly => ScrollBarVisibility.Disabled,
            PanningMode.HorizontalOnly => ScrollBarVisibility.Auto,
            _ => ScrollBarVisibility.Auto
        };

        scrollViewer.VerticalScrollBarVisibility = args.NewValue switch
        {
            PanningMode.HorizontalOnly => ScrollBarVisibility.Disabled,
            PanningMode.VerticalOnly => ScrollBarVisibility.Auto,
            _ => ScrollBarVisibility.Auto
        };

    }

    private static void OnAutoScrollChanged(ScrollViewer scrollViewer, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.NewValue is bool value && value)
        {
            if (scrollViewer.Content is Layoutable layoutable)
            {
                layoutable.PropertyChanged += (s, e) =>
                {
                    if (e.Property == Layoutable.BoundsProperty)
                    {
                        scrollViewer.ScrollToEnd();
                    }
                };
            }
        }
    }

    #endregion
}

public enum PanningMode
{
    VerticalOnly,
    HorizontalOnly,
    Both
}
