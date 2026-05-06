using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Acme.Industrial.UI.Controls.Industrial;

/// <summary>
/// 饼图数据项。
/// </summary>
public class PieChartItem
{
    /// <summary>
    /// 标签。
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 数值。
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// 颜色。
    /// </summary>
    public Color Color { get; set; } = Color.Gray;

    /// <summary>
    /// 是否高亮。
    /// </summary>
    public bool IsHighlighted { get; set; }
}

/// <summary>
/// 饼图类型。
/// </summary>
public enum PieChartType
{
    /// <summary>标准饼图。</summary>
    Pie,

    /// <summary>环形图。</summary>
    Donut,

    /// <summary>分离饼图。</summary>
    Exploded
}

/// <summary>
/// 饼图控件。
/// 用于显示比例数据，支持多种样式。
/// </summary>
public class PieChart : Control
{
    #region 属性

    private List<PieChartItem> _items = new();
    private PieChartType _chartType = PieChartType.Pie;
    private bool _showLabels = true;
    private bool _showPercentages = true;
    private bool _showLegend = true;
    private bool _showValues = false;
    private bool _draw3D = false;
    private int _donutRadius = 40;
    private double _rotationAngle = 0;
    private Color _emptyColor = Color.FromArgb(230, 230, 230);
    private int _highlightOffset = 10;
    private bool _animateOnChange = true;
    private int _hoveredIndex = -1;

    private static readonly Color[] DefaultColors = new[]
    {
        Color.FromArgb(0, 150, 200),
        Color.FromArgb(50, 180, 50),
        Color.FromArgb(255, 180, 0),
        Color.FromArgb(220, 80, 80),
        Color.FromArgb(150, 100, 200),
        Color.FromArgb(0, 180, 180),
        Color.FromArgb(200, 150, 80),
        Color.FromArgb(180, 80, 180)
    };

    /// <summary>
    /// 数据项列表。
    /// </summary>
    [Category("数据")]
    public List<PieChartItem> Items
    {
        get => _items;
        set { _items = value ?? new List<PieChartItem>(); Invalidate(); }
    }

    /// <summary>
    /// 饼图类型。
    /// </summary>
    [Category("外观")]
    [DefaultValue(PieChartType.Pie)]
    public PieChartType ChartType
    {
        get => _chartType;
        set { _chartType = value; Invalidate(); }
    }

    /// <summary>
    /// 是否显示标签。
    /// </summary>
    [Category("行为")]
    [DefaultValue(true)]
    public bool ShowLabels
    {
        get => _showLabels;
        set { _showLabels = value; Invalidate(); }
    }

    /// <summary>
    /// 是否显示百分比。
    /// </summary>
    [Category("行为")]
    [DefaultValue(true)]
    public bool ShowPercentages
    {
        get => _showPercentages;
        set { _showPercentages = value; Invalidate(); }
    }

    /// <summary>
    /// 是否显示图例。
    /// </summary>
    [Category("行为")]
    [DefaultValue(true)]
    public bool ShowLegend
    {
        get => _showLegend;
        set { _showLegend = value; Invalidate(); }
    }

    /// <summary>
    /// 是否绘制3D效果。
    /// </summary>
    [Category("外观")]
    [DefaultValue(false)]
    public bool Draw3D
    {
        get => _draw3D;
        set { _draw3D = value; Invalidate(); }
    }

    /// <summary>
    /// 环形图内半径百分比。
    /// </summary>
    [Category("外观")]
    [DefaultValue(40)]
    public int DonutRadius
    {
        get => _donutRadius;
        set { _donutRadius = Math.Max(10, Math.Min(90, value)); Invalidate(); }
    }

    /// <summary>
    /// 空数据颜色。
    /// </summary>
    [Category("外观")]
    public Color EmptyColor
    {
        get => _emptyColor;
        set { _emptyColor = value; Invalidate(); }
    }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化饼图。
    /// </summary>
    public PieChart()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        Size = new Size(300, 300);
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 添加数据项。
    /// </summary>
    public void AddItem(string label, double value, Color? color = null)
    {
        var item = new PieChartItem
        {
            Label = label,
            Value = value,
            Color = color ?? GetNextColor()
        };
        _items.Add(item);
        Invalidate();
    }

    /// <summary>
    /// 设置数据。
    /// </summary>
    public void SetData(Dictionary<string, double> data)
    {
        _items.Clear();
        var index = 0;
        foreach (var kvp in data)
        {
            _items.Add(new PieChartItem
            {
                Label = kvp.Key,
                Value = kvp.Value,
                Color = DefaultColors[index % DefaultColors.Length]
            });
            index++;
        }
        Invalidate();
    }

    /// <summary>
    /// 清空数据。
    /// </summary>
    public void Clear()
    {
        _items.Clear();
        Invalidate();
    }

    /// <summary>
    /// 更新指定项的值。
    /// </summary>
    public void UpdateValue(int index, double value)
    {
        if (index >= 0 && index < _items.Count)
        {
            _items[index].Value = value;
            Invalidate();
        }
    }

    /// <summary>
    /// 高亮指定项。
    /// </summary>
    public void HighlightItem(int index, bool highlight = true)
    {
        if (index >= 0 && index < _items.Count)
        {
            _items[index].IsHighlighted = highlight;
            Invalidate();
        }
    }

    private static int _colorIndex = 0;

    private static Color GetNextColor()
    {
        var color = DefaultColors[_colorIndex % DefaultColors.Length];
        _colorIndex++;
        return color;
    }

    #endregion

    #region 绘制

    protected override void OnPaint(PaintEventArgs e)
    {
        try
        {
            base.OnPaint(e);

            var g = e.Graphics;
            if (g == null) return;

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var totalValue = _items.Sum(i => i.Value);
            if (totalValue <= 0)
            {
                DrawEmptyChart(g);
                return;
            }

            // 计算图表区域
            var chartRect = GetChartRect();

            // 绘制3D效果
            if (_draw3D)
            {
                Draw3DChart(g, chartRect, totalValue);
            }

            // 绘制主图表
            DrawMainChart(g, chartRect, totalValue);

            // 绘制标签
            if (_showLabels)
            {
                DrawLabels(g, chartRect, totalValue);
            }

            // 绘制图例
            if (_showLegend)
            {
                DrawLegend(g);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PieChart OnPaint error: {ex.Message}");
        }
    }

    private Rectangle GetChartRect()
    {
        var legendWidth = _showLegend ? 120 : 0;
        var chartSize = Math.Min(ClientSize.Width - legendWidth, ClientSize.Height) - 20;
        var x = (ClientSize.Width - legendWidth - chartSize) / 2;
        var y = (ClientSize.Height - chartSize) / 2;
        return new Rectangle(x, y, chartSize, chartSize);
    }

    private void DrawEmptyChart(Graphics g)
    {
        var centerX = ClientSize.Width / 2f;
        var centerY = ClientSize.Height / 2f;
        var radius = Math.Min(ClientSize.Width, ClientSize.Height) / 2f - 20;

        using var brush = new SolidBrush(_emptyColor);
        g.FillEllipse(brush, centerX - radius, centerY - radius, radius * 2, radius * 2);

        using var font = new Font("微软雅黑", 12f);
        using var brush2 = new SolidBrush(Color.FromArgb(150, 150, 150));
        var text = "无数据";
        var textSize = g.MeasureString(text, font);
        g.DrawString(text, font, brush2, centerX - textSize.Width / 2, centerY - textSize.Height / 2);
    }

    private void Draw3DChart(Graphics g, Rectangle chartRect, double totalValue)
    {
        var depth = 20;
        var layers = 15;

        for (var i = depth; i >= 0; i -= depth / layers)
        {
            var layerRect = new Rectangle(chartRect.X, chartRect.Y + i, chartRect.Width, chartRect.Height);
            DrawChartSlice(g, layerRect, totalValue, i > 0 ? true : false);
        }
    }

    private void DrawMainChart(Graphics g, Rectangle chartRect, double totalValue)
    {
        if (_draw3D)
            return;

        DrawChartSlice(g, chartRect, totalValue, false);
    }

    private void DrawChartSlice(Graphics g, Rectangle rect, double totalValue, bool isShadow)
    {
        var centerX = rect.X + rect.Width / 2f;
        var centerY = rect.Y + rect.Height / 2f;
        var radius = rect.Width / 2f;

        var innerRadius = _chartType == PieChartType.Donut ? radius * _donutRadius / 100f : 0;

        var startAngle = _rotationAngle;
        var sweepIndex = 0;

        foreach (var item in _items)
        {
            var sweepAngle = (float)(item.Value / totalValue * 360);
            if (sweepAngle <= 0) continue;

            var color = item.Color;
            if (isShadow)
            {
                color = ControlPaint.Dark(color, 0.3f);
            }

            var offsetX = 0f;
            var offsetY = 0f;
            if (item.IsHighlighted && !isShadow)
            {
                var midAngle = (startAngle + sweepAngle / 2) * (float)Math.PI / 180;
                offsetX = _highlightOffset * (float)Math.Cos(midAngle);
                offsetY = _highlightOffset * (float)Math.Sin(midAngle);
            }

            if (_chartType == PieChartType.Donut)
            {
                DrawDonutSlice(g, centerX + offsetX, centerY + offsetY, radius, innerRadius, (float)startAngle, sweepAngle, color, sweepIndex == _hoveredIndex);
            }
            else
            {
                DrawPieSlice(g, centerX + offsetX, centerY + offsetY, radius, (float)startAngle, sweepAngle, color, sweepIndex == _hoveredIndex);
            }

            startAngle += sweepAngle;
            sweepIndex++;
        }
    }

    private void DrawPieSlice(Graphics g, float cx, float cy, float radius, float startAngle, float sweepAngle, Color color, bool isHover)
    {
        using var brush = new SolidBrush(isHover ? ControlPaint.Light(color, 0.2f) : color);

        var path = new GraphicsPath();
        path.AddPie(cx - radius, cy - radius, radius * 2, radius * 2, startAngle, sweepAngle);
        g.FillPath(brush, path);

        using var pen = new Pen(Color.FromArgb(50, 50, 50), isHover ? 2 : 1);
        g.DrawPath(pen, path);

        // 高光
        if (!isHover)
        {
            using var highlightBrush = new SolidBrush(Color.FromArgb(50, 255, 255, 255));
            var highlightPath = new GraphicsPath();
            highlightPath.AddPie(cx - radius, cy - radius, radius * 2, radius * 2, startAngle, sweepAngle * 0.3f);
            g.FillPath(highlightBrush, highlightPath);
        }
    }

    private void DrawDonutSlice(Graphics g, float cx, float cy, float radius, float innerRadius, float startAngle, float sweepAngle, Color color, bool isHover)
    {
        using var brush = new SolidBrush(isHover ? ControlPaint.Light(color, 0.2f) : color);

        var path = new GraphicsPath();
        path.AddArc(cx - radius, cy - radius, radius * 2, radius * 2, startAngle, sweepAngle);
        path.AddArc(cx - innerRadius, cy - innerRadius, innerRadius * 2, innerRadius * 2, startAngle + sweepAngle, -sweepAngle);
        path.CloseFigure();

        g.FillPath(brush, path);

        using var pen = new Pen(Color.FromArgb(50, 50, 50), isHover ? 2 : 1);
        g.DrawPath(pen, path);
    }

    private void DrawLabels(Graphics g, Rectangle chartRect, double totalValue)
    {
        var centerX = chartRect.X + chartRect.Width / 2f;
        var centerY = chartRect.Y + chartRect.Height / 2f;
        var radius = chartRect.Width / 2f;

        var startAngle = _rotationAngle;

        foreach (var item in _items)
        {
            var sweepAngle = (float)(item.Value / totalValue * 360);
            if (sweepAngle < 5) // 太小不显示标签
            {
                startAngle += sweepAngle;
                continue;
            }

            var midAngle = (startAngle + sweepAngle / 2) * (float)Math.PI / 180;
            var labelRadius = radius * 0.7f;
            var labelX = centerX + labelRadius * (float)Math.Cos(midAngle);
            var labelY = centerY + labelRadius * (float)Math.Sin(midAngle);

            var percentage = item.Value / totalValue * 100;

            // 百分比文字
            if (_showPercentages && sweepAngle > 20)
            {
                using var font = new Font("Arial", 9f, FontStyle.Bold);
                using var brush = new SolidBrush(Color.White);
                var text = $"{percentage:F1}%";
                var textSize = g.MeasureString(text, font);

                var bgRect = new Rectangle(
                    (int)(labelX - textSize.Width / 2 - 2),
                    (int)(labelY - textSize.Height / 2 - 1),
                    (int)(textSize.Width + 4),
                    (int)(textSize.Height + 2));

                using var bgBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0));
                g.FillRectangle(bgBrush, bgRect);

                g.DrawString(text, font, brush, labelX - textSize.Width / 2, labelY - textSize.Height / 2);
            }

            startAngle += sweepAngle;
        }
    }

    private void DrawLegend(Graphics g)
    {
        var legendX = ClientSize.Width - 110;
        var legendY = 10;
        var itemHeight = 25;

        using var font = new Font("微软雅黑", 9f);
        using var textBrush = new SolidBrush(ForeColor);

        for (var i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            var y = legendY + i * itemHeight;

            // 颜色块
            using var brush = new SolidBrush(item.Color);
            g.FillRectangle(brush, legendX, y + 3, 16, 16);

            // 边框
            using var pen = new Pen(Color.FromArgb(100, 100, 100), 1);
            g.DrawRectangle(pen, legendX, y + 3, 16, 16);

            // 标签
            var label = item.Label;
            if (label.Length > 10)
                label = label.Substring(0, 10) + "...";

            var textSize = g.MeasureString(label, font);
            g.DrawString(label, font, textBrush, legendX + 22, y + 2);

            // 百分比
            if (_showPercentages)
            {
                var totalValue = _items.Sum(i => i.Value);
                var percentage = totalValue > 0 ? item.Value / totalValue * 100 : 0;
                using var percentFont = new Font("Arial", 8f);
                using var percentBrush = new SolidBrush(Color.FromArgb(150, 150, 150));
                var percentText = $"{percentage:F1}%";
                g.DrawString(percentText, percentFont, percentBrush, legendX + 22 + textSize.Width + 5, y + 3);
            }
        }
    }

    #endregion

    #region 控件行为

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var chartRect = GetChartRect();
        var centerX = chartRect.X + chartRect.Width / 2f;
        var centerY = chartRect.Y + chartRect.Height / 2f;
        var radius = chartRect.Width / 2f;

        var dx = e.X - centerX;
        var dy = e.Y - centerY;
        var distance = Math.Sqrt(dx * dx + dy * dy);

        var newHoveredIndex = -1;

        if (distance <= radius && distance > 0)
        {
            var angle = Math.Atan2(dy, dx) * 180 / Math.PI;
            if (angle < 0) angle += 360;
            angle = (angle - _rotationAngle + 360) % 360;

            var totalValue = _items.Sum(i => i.Value);
            var startAngle = 0.0;

            for (var i = 0; i < _items.Count; i++)
            {
                var sweepAngle = _items[i].Value / totalValue * 360;
                if (angle >= startAngle && angle < startAngle + sweepAngle)
                {
                    newHoveredIndex = i;
                    break;
                }
                startAngle += sweepAngle;
            }
        }

        if (newHoveredIndex != _hoveredIndex)
        {
            _hoveredIndex = newHoveredIndex;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoveredIndex = -1;
        Invalidate();
    }

    protected override Size DefaultSize => new Size(300, 300);

    #endregion
}
