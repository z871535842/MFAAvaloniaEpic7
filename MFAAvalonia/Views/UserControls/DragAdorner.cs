using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Media;
using SukiUI;
using System;

namespace MFAAvalonia.Views.UserControls;

public class DragAdorner : Control
{
    private readonly Pen _linePen;
    private readonly double _originX;
    private readonly double _totalWidth;
    private double _yPosition;
    private bool _begin;
    private bool _end;
    public DragAdorner(double x, double width, IBrush brush)
    {
        _linePen = new Pen(brush, 2);
        _originX = x;
        _totalWidth = width;
        IsHitTestVisible = false;
    }

    public void UpdatePosition(double y, bool begin, bool end)
    {
        if (_yPosition == y) return;
        _yPosition = y;
        _begin = begin;
        _end = end;
        InvalidateVisual();
    }


    public override void Render(DrawingContext context)
    {
        const double arrowHeight = 3; // 箭头高度
        const double arrowWidth = 1.5; // 箭头宽度
        const double linePadding = 8; // 线端留白
        const double xOffSet = 5; // x轴偏移
        // 主横线坐标计算（带抗锯齿偏移）
        var lineY = _yPosition + 0.5;
        var startX = _originX + linePadding - xOffSet;
        var endX = _originX + _totalWidth - linePadding - xOffSet;

        // 绘制主横线
        context.DrawLine(
            _linePen,
            new Point(startX, lineY),
            new Point(endX, lineY)
        );


        if (!_begin)
        {
            // 左侧箭头 (>)
            context.DrawLine(_linePen,
                new Point(startX, lineY),
                new Point(startX - arrowWidth, lineY - arrowHeight));
            context.DrawLine(_linePen,
                new Point(startX - arrowWidth, lineY + 1),
                new Point(startX - arrowWidth, lineY - arrowHeight));

            // 右侧箭头 (<)
            context.DrawLine(_linePen,
                new Point(endX, lineY),
                new Point(endX + arrowWidth, lineY - arrowHeight));
            context.DrawLine(_linePen,
                new Point(endX + arrowWidth, lineY + 1),
                new Point(endX + arrowWidth, lineY - arrowHeight));
        }
        if (!_end)
        {
            // 左侧箭头 (>)
            context.DrawLine(_linePen,
                new Point(startX, lineY),
                new Point(startX - arrowWidth, lineY + arrowHeight));
            context.DrawLine(_linePen,
                new Point(startX - arrowWidth, lineY - 1),
                new Point(startX - arrowWidth, lineY + arrowHeight));

            // 右侧箭头 (<)
            context.DrawLine(_linePen,
                new Point(endX, lineY),
                new Point(endX + arrowWidth, lineY + arrowHeight));
            context.DrawLine(_linePen,
                new Point(endX + arrowWidth, lineY - 1),
                new Point(endX + arrowWidth, lineY + arrowHeight));
        }
    }
}
