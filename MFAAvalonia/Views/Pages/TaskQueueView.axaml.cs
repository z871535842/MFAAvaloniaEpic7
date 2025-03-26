using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using AvaloniaExtensions.Axaml.Markup;
using ExCSS;
using MFAAvalonia.Configuration;
using MFAAvalonia.Extensions;
using MFAAvalonia.Extensions.MaaFW;
using MFAAvalonia.Helper;
using MFAAvalonia.Helper.Converters;
using MFAAvalonia.Helper.ValueType;
using MFAAvalonia.ViewModels.Pages;
using MFAAvalonia.ViewModels.UsersControls;
using MFAAvalonia.Views.UserControls;
using SukiUI;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Color = Avalonia.Media.Color;
using FontStyle = Avalonia.Media.FontStyle;
using FontWeight = Avalonia.Media.FontWeight;
using HorizontalAlignment = Avalonia.Layout.HorizontalAlignment;
using Point = Avalonia.Point;
using VerticalAlignment = Avalonia.Layout.VerticalAlignment;

namespace MFAAvalonia.Views.Pages;

public partial class TaskQueueView : UserControl
{
    public TaskQueueView()
    {
        DataContext = Instances.TaskQueueViewModel;
        InitializeComponent();
        MaaProcessor.Instance.InitializeData();
        // Introduction.TextArea.TextView.LineTransformers.Add(new RichTextLineTransformer());
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
                var sourceIndex = GetSourceIndex(e.GetPosition(listBox), listBox, listBox.SelectedIndex);
                if (sourceIndex != -1)
                {
                    var data = new DataObject();
                    data.Set(DataFormats.Text, sourceIndex.ToString());
                    DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
                }
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

    private int GetSourceIndex(Point position, ListBox listBox, int defaultValue)
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
            Instances.TaskQueueView.SetOption(taskItemViewModel, false);
            ConfigurationManager.Current.SetValue(ConfigurationKeys.TaskItems, vm.TaskItemViewModels.ToList().Select(model => model.InterfaceItem));
        }
    }

    #region 任务选项

    private static readonly ConcurrentDictionary<string, StackPanel> CommonPanelCache = new();
    private static readonly ConcurrentDictionary<string, string> IntroductionsCache = new();
    private void SetMarkDown(string markDown)
    {
        Introduction.Markdown = markDown;
    }

    public void SetOption(DragItemViewModel dragItem, bool value)
    {
        var cacheKey = $"{dragItem.Name}_{dragItem.InterfaceItem.GetHashCode()}";

        if (!value)
        {
            HideCurrentPanel(cacheKey);
            return;
        }

        var newPanel = CommonPanelCache.GetOrAdd(cacheKey, key =>
        {
            var p = new StackPanel();
            GeneratePanelContent(p, dragItem);
            CommonOptionSettings.Children.Add(p);
            return p;
        });

        var newIntroduction = IntroductionsCache.GetOrAdd(cacheKey, key =>
        {
            var input = string.Empty;

            // 原始带标记的文本
            if (dragItem.InterfaceItem?.Document?.Count > 0)
            {
                input = Regex.Unescape(string.Join("\\n", dragItem.InterfaceItem.Document));
            }
            input = LanguageHelper.GetLocalizedString(input);
            return ConvertCustomMarkup(input);
        });

        SetMarkDown(newIntroduction);
        if (newPanel.Children.Count == 0)
            CommonPanelCache.Remove(cacheKey, out _);
        newPanel.IsVisible = true;
    }


    private void GeneratePanelContent(StackPanel panel, DragItemViewModel dragItem)
    {
        AddRepeatOption(panel, dragItem);

        if (dragItem.InterfaceItem?.Option != null)
        {
            foreach (var option in dragItem.InterfaceItem.Option)
            {
                AddOption(panel, option, dragItem);
            }
        }
    }

    private void HideCurrentPanel(string key)
    {
        if (CommonPanelCache.TryGetValue(key, out var oldPanel))
        {
            oldPanel.IsVisible = false;
        }

        Introduction.Markdown = "";
    }

    private void HideAllPanels()
    {
        foreach (var panel in CommonPanelCache.Values)
        {
            panel.IsVisible = false;
        }

        Introduction.Markdown = "";
    }


    private void AddRepeatOption(Panel panel, DragItemViewModel source)
    {
        if (source.InterfaceItem is not { Repeatable: true }) return;
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition
                {
                    Width = new GridLength(7, GridUnitType.Star),
                },
                new ColumnDefinition
                {
                    Width = new GridLength(4, GridUnitType.Star),
                    MinWidth = 140
                },
            },
            Margin = new Thickness(8, 0, 5, 5)
        };

        var textBlock = new TextBlock
        {
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        Grid.SetColumn(textBlock, 0);
        textBlock.Bind(TextBlock.TextProperty, new I18nBinding("RepeatOption"));
        textBlock.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension("SukiLowText"));
        grid.Children.Add(textBlock);
        var numericUpDown = new NumericUpDown
        {
            Value = source.InterfaceItem.RepeatCount ?? 1,
            Margin = new Thickness(5),
            Increment = 1,
            Minimum = -1,
        };
        numericUpDown.ValueChanged += (_, _) =>
        {
            source.InterfaceItem.RepeatCount = Convert.ToInt32(numericUpDown.Value);
            SaveConfiguration();
        };
        Grid.SetColumn(numericUpDown, 1);
        grid.Children.Add(numericUpDown);
        panel.Children.Add(grid);
    }

    private void AddOption(Panel panel, MaaInterface.MaaInterfaceSelectOption option, DragItemViewModel source)
    {
        if (MaaProcessor.Interface?.Option?.TryGetValue(option.Name ?? string.Empty, out var interfaceOption) != true) return;

        var converter = new CustomIsEnabledConverter();

        Control control = interfaceOption.Cases.ShouldSwitchButton(out var yes, out var no)
            ? CreateToggleControl(option, yes, no, source, converter)
            : CreateComboControl(option, interfaceOption, source, converter);

        panel.Children.Add(control);
    }

    private Grid CreateToggleControl(
        MaaInterface.MaaInterfaceSelectOption option,
        int yesValue,
        int noValue,
        DragItemViewModel source,
        IMultiValueConverter? customConverter)
    {
        var button = new ToggleSwitch
        {
            IsChecked = option.Index == yesValue,
            Classes =
            {
                "Switch"
            },
            MaxHeight = 60,
            MaxWidth = 100,
            HorizontalAlignment = HorizontalAlignment.Right,
            Tag = option.Name,
            VerticalAlignment = VerticalAlignment.Center
        };

        var multiBinding = new MultiBinding
        {
            Converter = customConverter,
            Mode = BindingMode.OneWay
        };
        multiBinding.Bindings.Add(new Binding("IsCheckedWithNull")
        {
            Source = source
        });
        multiBinding.Bindings.Add(new Binding("Idle")
        {
            Source = Instances.RootViewModel
        });
        button.Bind(IsEnabledProperty, multiBinding);


        button.IsCheckedChanged += (_, _) =>
        {
            option.Index = button.IsChecked == true ? yesValue : noValue;
            SaveConfiguration();
        };

        button.SetValue(ToolTip.TipProperty, LanguageHelper.GetLocalizedString(option.Name));
        var textBlock = new TextBlock
        {
            Text = LanguageHelper.GetLocalizedString(option.Name),
            Margin = new Thickness(8, 0, 5, 0),
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        };

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition
                {
                    Width = GridLength.Auto
                },
                new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star)
                },
                new ColumnDefinition
                {
                    Width = GridLength.Auto
                }
            },
            Margin = new Thickness(0, 0, 0, 5)
        };

        Grid.SetColumn(textBlock, 0);
        Grid.SetColumn(button, 2);
        grid.Children.Add(textBlock);
        grid.Children.Add(button);

        return grid;
    }

    private Grid CreateComboControl(
        MaaInterface.MaaInterfaceSelectOption option,
        MaaInterface.MaaInterfaceOption interfaceOption,
        DragItemViewModel source,
        IMultiValueConverter? customConverter)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition
                {
                    Width = new GridLength(7, GridUnitType.Star),
                },
                new ColumnDefinition
                {
                    Width = new GridLength(4, GridUnitType.Star),
                    MinWidth = 150
                },
            },
            Margin = new Thickness(8, 0, 0, 5)
        };

        var combo = new ComboBox
        {
            DisplayMemberBinding = new Binding("Name"),
            Margin = new Thickness(5),
            ItemsSource = interfaceOption.Cases?.Select(c => new
            {
                Name = LanguageHelper.GetLocalizedString(c.Name)
            }),
            SelectedIndex = option.Index ?? 0,
        };

        var multiBinding = new MultiBinding
        {
            Converter = customConverter,
            Mode = BindingMode.OneWay
        };
        multiBinding.Bindings.Add(new Binding("IsCheckedWithNull")
        {
            Source = source
        });
        multiBinding.Bindings.Add(new Binding("Idle")
        {
            Source = Instances.RootViewModel
        });
        combo.Bind(IsEnabledProperty, multiBinding);

        combo.SelectionChanged += (_, _) =>
        {
            option.Index = combo.SelectedIndex;
            SaveConfiguration();
        };
        Grid.SetColumn(combo, 1);
        var textBlock = new TextBlock
        {
            FontSize = 14,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Left,
            Text = LanguageHelper.GetLocalizedString(option.Name),
        };
        textBlock.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension("SukiLowText"));
        Grid.SetColumn(textBlock, 0);
        grid.Children.Add(combo);
        grid.Children.Add(textBlock);
        return grid;
    }


    private void SaveConfiguration()
    {
        ConfigurationManager.Current.SetValue(ConfigurationKeys.TaskItems,
            Instances.TaskQueueViewModel.TaskItemViewModels.Select(m => m.InterfaceItem));
    }
    public static string ConvertCustomMarkup(string input, string outputFormat = "markdown")
    {
        // 预处理换行符
        input = input.Replace(@"\n", "\n");

        // 定义替换规则字典
        // 定义替换规则字典
        var replacementRules = new Dictionary<string, Dictionary<string, string>>
        {
            // 颜色标记 [color:red]
            {
                @"\[color:(.*?)\]", new Dictionary<string, string>
                {
                    {
                        "markdown", "%{color:$1}"
                    },
                    {
                        "html", "<span style='color: $1;'>"
                    }
                }
            },
            // 字号标记 [size:20]
            {
                @"\[size:(\d+)\]", new Dictionary<string, string>
                {
                    {
                        "markdown", ""
                    },
                    {
                        "html", "<span style='font-size: $1px;'>"
                    }
                }
            },
            // 对齐标记 [align:center]
            {
                @"\[align:(left|center|right)\]", new Dictionary<string, string>
                {
                    {
                        "markdown", "$1" switch { "center" => "p=.", "right" => "p>.", _ => "p<." }
                    },
                    {
                        "html", "<div style='text-align: $1;'>"
                    }
                }
            },
            {
                @"\[/(color)\]", new Dictionary<string, string>
                {
                    {
                        "markdown", "%"
                    },
                    {
                        "html", "$1" switch { "align" => "</div>", _ => "</span>" }
                    }
                }
            },
            {
                @"\[/(align)\]", new Dictionary<string, string>
                {
                    {
                        "markdown", ""
                    },
                    {
                        "html", "$1" switch { "align" => "</div>", _ => "</span>" }
                    }
                }
            },
            // 关闭标记 [/color] [/size] [/align]
            {
                @"\[/(size)\]", new Dictionary<string, string>
                {
                    {
                        "markdown", ""
                    },
                    {
                        "html", "$1" switch { "align" => "</div>", _ => "</span>" }
                    }
                }
            },
            // 粗体、斜体等简单标记
            {
                @"\[(b|i|u|s)\]", new Dictionary<string, string>
                {
                    {
                        "markdown", "$1" switch
                        {
                            "b" => "**", "i" => "*", "u" => "<u>", "s" => "~~", _ => ""
                        }
                    },
                    {
                        "html", "$1" switch
                        {
                            "b" => "<strong>", "i" => "<em>", "u" => "<u>", "s" => "<s>", _ => ""
                        }
                    }
                }
            },
            {
                @"\[/(b|i|u|s)\]", new Dictionary<string, string>
                {
                    {
                        "markdown", "$1" switch
                        {
                            "b" => "**", "i" => "*", "u" => "</u>", "s" => "~~", _ => ""
                        }
                    },
                    {
                        "html", "$1" switch
                        {
                            "b" => "</strong>", "i" => "</em>", "u" => "</u>", "s" => "</s>", _ => ""
                        }
                    }
                }
            }
        };

        // 执行正则替换
        foreach (var rule in replacementRules)
        {
            input = Regex.Replace(
                input,
                rule.Key,
                m => rule.Value[outputFormat].Replace("$1", m.Groups[1].Value),
                RegexOptions.IgnoreCase
            );
        }

        // 处理换行符
        input = outputFormat switch
        {
            "markdown" => input.Replace("\n", "  \n"), // Markdown换行需两个空格
            "html" => input.Replace("\n", "<br/>"), // HTML换行用<br/>
            _ => input
        };

        return input;
    }
    // private static List<TextStyleMetadata> _currentStyles = new();
    //
    // private class RichTextLineTransformer : DocumentColorizingTransformer
    // {
    //     protected override void ColorizeLine(DocumentLine line)
    //     {
    //         _currentStyles = _currentStyles.OrderByDescending(s => s.EndOffset).ToList();
    //         int lineStart = line.Offset;
    //         int lineEnd = line.Offset + line.Length;
    //
    //         foreach (var style in _currentStyles)
    //         {
    //             if (style.EndOffset <= lineStart || style.StartOffset >= lineEnd)
    //                 continue;
    //
    //             int start = Math.Max(style.StartOffset, lineStart);
    //             int end = Math.Min(style.EndOffset, lineEnd);
    //             ApplyStyle(start, end, style.Tag, style.Value);
    //         }
    //     }
    //
    //
    //     /// <summary>
    //     /// 应用样式到指定范围的文本
    //     /// </summary>
    //     /// <param name="startOffset">起始偏移量</param>
    //     /// <param name="endOffset">结束偏移量</param>
    //     /// <param name="tag">标记名称</param>
    //     /// <param name="value">标记值</param>
    //     private void ApplyStyle(int startOffset, int endOffset, string tag, string value)
    //     {
    //         switch (tag)
    //         {
    //             case "color":
    //                 ChangeLinePart(startOffset, endOffset, element => element.TextRunProperties.SetForegroundBrush(new SolidColorBrush(Color.Parse(value))));
    //                 break;
    //             case "size":
    //                 if (double.TryParse(value, out var size))
    //                 {
    //                     ChangeLinePart(startOffset, endOffset, element => element.TextRunProperties.SetFontRenderingEmSize(size));
    //                 }
    //                 break;
    //             case "b":
    //                 ChangeLinePart(startOffset, endOffset, element =>
    //                 {
    //                     var typeface = new Typeface(
    //                         element.TextRunProperties.Typeface.FontFamily,
    //                         element.TextRunProperties.Typeface.Style, FontWeight.Bold, // 设置粗体
    //                         element.TextRunProperties.Typeface.Stretch
    //                     );
    //                     element.TextRunProperties.SetTypeface(typeface);
    //                 });
    //                 break;
    //             case "i":
    //                 ChangeLinePart(startOffset, endOffset, element =>
    //                 {
    //                     var typeface = new Typeface(
    //                         element.TextRunProperties.Typeface.FontFamily,
    //                         FontStyle.Italic, // 设置斜体
    //                         element.TextRunProperties.Typeface.Weight,
    //                         element.TextRunProperties.Typeface.Stretch
    //                     );
    //                     element.TextRunProperties.SetTypeface(typeface);
    //                 });
    //                 break;
    //             case "u":
    //                 ChangeLinePart(startOffset, endOffset, element => element.TextRunProperties.SetTextDecorations(TextDecorations.Underline));
    //                 break;
    //             case "s":
    //                 ChangeLinePart(startOffset, endOffset, element => element.TextRunProperties.SetTextDecorations(TextDecorations.Strikethrough));
    //                 break;
    //         }
    //     }
    // }
    //
    // public class TextStyleMetadata
    // {
    //     public int StartOffset { get; set; }
    //     public int EndOffset { get; set; }
    //     public string Tag { get; set; }
    //     public string Value { get; set; }
    //
    //     // 新增字段存储标签部分的长度
    //     public int OriginalLength { get; set; }
    // }
    //
    // private (string CleanText, List<TextStyleMetadata> Styles) ProcessRichTextTags(string input)
    // {
    //     var styles = new List<TextStyleMetadata>();
    //     var cleanText = new StringBuilder();
    //     ProcessNestedContent(input, cleanText, styles, new Stack<(string Tag, string Value, int CleanStart)>());
    //     return (cleanText.ToString(), styles);
    // }
    //
    // private void ProcessNestedContent(string input, StringBuilder cleanText, List<TextStyleMetadata> styles, Stack<(string Tag, string Value, int CleanStart)> stack)
    // {
    //     var matches = Regex.Matches(input, @"\[(?<tag>[^\]]+):?(?<value>[^\]]*)\](?<content>.*?)\[/\k<tag>\]");
    //     int lastPos = 0;
    //
    //     foreach (Match match in matches.Cast<Match>())
    //     {
    //         // 添加非标签内容
    //         if (match.Index > lastPos)
    //         {
    //             cleanText.Append(input.Substring(lastPos, match.Index - lastPos));
    //         }
    //
    //         string tag = match.Groups["tag"].Value.ToLower();
    //         string value = match.Groups["value"].Value;
    //         string content = match.Groups["content"].Value;
    //
    //         // 记录开始位置
    //         int contentStart = cleanText.Length;
    //         stack.Push((tag, value, contentStart));
    //
    //         // 递归解析嵌套内容
    //         var nestedCleanText = new StringBuilder();
    //         ProcessNestedContent(content, nestedCleanText, styles, new Stack<(string Tag, string Value, int CleanStart)>(stack));
    //         cleanText.Append(nestedCleanText);
    //
    //         // 记录样式元数据
    //         if (stack.Count > 0 && stack.Peek().Tag == tag)
    //         {
    //             var (openTag, openValue, cleanStart) = stack.Pop();
    //             styles.Add(new TextStyleMetadata
    //             {
    //                 StartOffset = cleanStart,
    //                 EndOffset = cleanText.Length,
    //                 Tag = openTag,
    //                 Value = openValue
    //             });
    //         }
    //         lastPos = match.Index + match.Length;
    //     }
    //
    //     // 添加剩余文本
    //     if (lastPos < input.Length)
    //     {
    //         cleanText.Append(input.Substring(lastPos));
    //     }
    // }
    //
    // // 使用 MatchEvaluator 的独立方法
    //

    #endregion
}
