using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Acme.Industrial.UI.Controls.Industrial;

/// <summary>
/// 棒图控件。
/// 用于显示多个数值的对比，支持水平和垂直显示。
/// </summary>
[DefaultProperty(nameof(Values))]
public class BarChart : Control
{
    #region 属性

    private List<double> _values = new() { 30, 50, 70, 40, 90 };
    private List<string> _labels = new() { "A区", "B区", "C区", "D区", "E区" };
    private double _minimum = 0;
    private double _maximum = 100;
    private bool _isHorizontal = true;
    private bool _showValues = true;
    private bool _showLabels = true;
    private bool _showGrid = true;
    private int _barCount = 5;
    private float _barSpacing = 0.2f;
    private Color _barColor = Color.FromArgb(0, 150, 200);
    private Color _barColor2 = Color.FromArgb(0, 100, 180);
    private Color _gridColor = Color.FromArgb(230, 230, 230);
    private Color _labelColor = Color.FromArgb(80, 80, 80);
    private bool _useGradient = true;
    private bool _showBackground = true;
    private bool _roundedBars = true;

    /// <summary>
    /// 数值列表。
    /// </summary>
    [Category("数据")]
    [Description("棒图数值列表")]
    public List<double> Values
    {
        get => _values;
        set { _values = value ?? new List<double>(); Invalidate(); }
    }

    /// <summary>
    /// 标签列表。
    /// </summary>
    [Category("数据")]
    [Description("棒图标签列表")]
    public List<string> Labels
    {
        get => _labels;
        set { _labels = value ?? new List<string>(); Invalidate(); }
    }

    /// <summary>
    /// 最小值。
    /// </summary>
    [Category("数据")]
    [DefaultValue(0.0)]
    public double Minimum
    {
        get => _minimum;
        set { _minimum = value; Invalidate(); }
    }

    /// <summary>
    /// 最大值。
    /// </summary>
    [Category("数据")]
    [DefaultValue(100.0)]
    public double Maximum
    {
        get => _maximum;
        set { _maximum = value; Invalidate(); }
    }

    /// <summary>
    /// 是否水平显示。
    /// </summary>
    [Category("外观")]
    [DefaultValue(true)]
    public bool IsHorizontal
    {
        get => _isHorizontal;
        set { _isHorizontal = value; Invalidate(); }
    }

    /// <summary>
    /// 是否显示数值。
    /// </summary>
    [Category("行为")]
    [DefaultValue(true)]
    public bool ShowValues
    {
        get => _showValues;
        set { _showValues = value; Invalidate(); }
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
    /// 是否显示网格线。
    /// </summary>
    [Category("行为")]
    [DefaultValue(true)]
    public bool ShowGrid
    {
        get => _showGrid;
        set { _showGrid = value; Invalidate(); }
    }

    /// <summary>
    /// 条形颜色。
    /// </summary>
    [Category("外观")]
    public Color BarColor
    {
        get => _barColor;
        set { _barColor = value; Invalidate(); }
    }

    /// <summary>
    /// 是否使用渐变。
    /// </summary>
    [Category("外观")]
    [DefaultValue(true)]
    public bool UseGradient
    {
        get => _useGradient;
        set { _useGradient = value; Invalidate(); }
    }

    /// <summary>
    /// 是否圆角条形。
    /// </summary>
    [Category("外观")]
    [DefaultValue(true)]
    public bool RoundedBars
    {
        get => _roundedBars;
        set { _roundedBars = value; Invalidate(); }
    }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化棒图。
    /// </summary>
    public BarChart()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        Size = new Size(400, 200);
    }

    #endregion

    #region 方法

    /// <summary>
    /// 设置数据。
    /// </summary>
    public void SetData(Dictionary<string, double> data)
    {
        _labels = new List<string>(data.Keys);
        _values = new List<double>(data.Values);
        Invalidate();
    }

    /// <summary>
    /// 添加数据项。
    /// </summary>
    public void AddItem(string label, double value)
    {
        _labels.Add(label);
        _values.Add(value);
        Invalidate();
    }

    /// <summary>
    /// 清空数据。
    /// </summary>
    public void Clear()
    {
        _labels.Clear();
        _values.Clear();
        Invalidate();
    }

    /// <summary>
    /// 更新指定索引的值。
    /// </summary>
    public void SetValue(int index, double value)
    {
        if (index >= 0 && index < _values.Count)
        {
            _values[index] = value;
            Invalidate();
        }
    }

    #endregion

    #region 绘制

    /// <inheritdoc />
    protected override void OnPaint(PaintEventArgs e)
    {
        try
        {
            base.OnPaint(e);

            var g = e.Graphics;
            if (g == null) return;

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            if (_isHorizontal)
                PaintHorizontal(g);
            else
                PaintVertical(g);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"BarChart OnPaint error: {ex.Message}");
        }
    }

    /// <summary>
    /// 绘制水平棒图。
    /// </summary>
    private void PaintHorizontal(Graphics g)
    {
        if (_values.Count == 0) return;

        var chartLeft = 60;
        var chartRight = ClientSize.Width - 10;
        var chartTop = 10;
        var chartBottom = ClientSize.Height - (_showLabels ? 30 : 10);
        var chartWidth = chartRight - chartLeft;
        var chartHeight = chartBottom - chartTop;

        var barHeight = (chartHeight - (_values.Count - 1) * 10) / _values.Count;
        if (barHeight < 10) barHeight = 10;

        // 绘制背景
        if (_showBackground)
        {
            using var bgBrush = new SolidBrush(Color.FromArgb(248, 249, 250));
            g.FillRectangle(bgBrush, chartLeft, chartTop, chartWidth, chartHeight);
        }

        // 绘制网格
        if (_showGrid)
            DrawHorizontalGrid(g, chartLeft, chartTop, chartWidth, chartHeight);

        // 绘制条形
        for (var i = 0; i < _values.Count; i++)
        {
            var y = chartTop + i * (barHeight + 10) + 5;
            var value = _values[i];
            var ratio = (value - _minimum) / (_maximum - _minimum);
            ratio = Math.Max(0, Math.Min(1, ratio));

            var barWidth = (float)(chartWidth * ratio);

            // 绘制标签
            if (_showLabels && i < _labels.Count)
            {
                using var labelFont = new Font("微软雅黑", 9f);
                using var labelBrush = new SolidBrush(_labelColor);
                g.DrawString(_labels[i], labelFont, labelBrush, 5, y + barHeight / 2 - 6);
            }

            // 绘制条形
            if (barWidth > 0)
            {
                DrawBar(g, chartLeft, y, barWidth, barHeight, i);
            }

            // 绘制数值
            if (_showValues)
            {
                using var valueFont = new Font("Arial", 8f, FontStyle.Bold);
                using var valueBrush = new SolidBrush(_labelColor);
                var text = value.ToString("F1");
                g.DrawString(text, valueFont, valueBrush, chartLeft + barWidth + 5, y + barHeight / 2 - 5);
            }
        }
    }

    /// <summary>
    /// 绘制垂直棒图。
    /// </summary>
    private void PaintVertical(Graphics g)
    {
        if (_values.Count == 0) return;

        var chartLeft = 40;
        var chartRight = ClientSize.Width - 10;
        var chartTop = 10;
        var chartBottom = ClientSize.Height - (_showLabels ? 40 : 10);
        var chartWidth = chartRight - chartLeft;
        var chartHeight = chartBottom - chartTop;

        var barWidth = (chartWidth - (_values.Count - 1) * 15) / _values.Count;
        if (barWidth < 15) barWidth = 15;

        // 绘制背景
        if (_showBackground)
        {
            using var bgBrush = new SolidBrush(Color.FromArgb(248, 249, 250));
            g.FillRectangle(bgBrush, chartLeft, chartTop, chartWidth, chartHeight);
        }

        // 绘制网格
        if (_showGrid)
            DrawVerticalGrid(g, chartLeft, chartTop, chartWidth, chartHeight);

        // 绘制条形
        for (var i = 0; i < _values.Count; i++)
        {
            var x = chartLeft + i * (barWidth + 15) + 7.5f;
            var value = _values[i];
            var ratio = (value - _minimum) / (_maximum - _minimum);
            ratio = Math.Max(0, Math.Min(1, ratio));

            var barHeight = (float)(chartHeight * ratio);
            var barY = chartBottom - barHeight;

            // 绘制条形
            if (barHeight > 0)
            {
                DrawVerticalBar(g, x, barY, barWidth, barHeight, i);
            }

            // 绘制标签
            if (_showLabels && i < _labels.Count)
            {
                using var labelFont = new Font("微软雅黑", 8f);
                using var labelBrush = new SolidBrush(_labelColor);
                var text = _labels[i].Length > 6 ? _labels[i].Substring(0, 6) : _labels[i];
                var textSize = g.MeasureString(text, labelFont);
                g.DrawString(text, labelFont, labelBrush, x + barWidth / 2 - textSize.Width / 2, chartBottom + 5);
            }

            // 绘制数值
            if (_showValues && barHeight > 15)
            {
                using var valueFont = new Font("Arial", 8f, FontStyle.Bold);
                using var valueBrush = new SolidBrush(Color.White);
                var text = value.ToString("F0");
                var textSize = g.MeasureString(text, valueFont);
                g.DrawString(text, valueFont, valueBrush, x + barWidth / 2 - textSize.Width / 2, barY + 3);
            }
        }
    }

    /// <summary>
    /// 绘制条形。
    /// </summary>
    private void DrawBar(Graphics g, float x, float y, float width, float height, int index)
    {
        var colorIndex = index % 2 == 0 ? _barColor : _barColor2;
        var rect = new RectangleF(x, y, width, height);

        if (_useGradient)
        {
            using var gradientBrush = new LinearGradientBrush(
                rect,
                Color.FromArgb(255, colorIndex),
                Color.FromArgb(180, colorIndex),
                LinearGradientMode.Horizontal);
            g.FillRectangle(gradientBrush, rect);
        }
        else
        {
            using var brush = new SolidBrush(colorIndex);
            g.FillRectangle(brush, rect);
        }

        // 顶部高光
        using var highlightBrush = new SolidBrush(Color.FromArgb(60, 255, 255, 255));
        g.FillRectangle(highlightBrush, x, y, width, height * 0.3f);
    }

    /// <summary>
    /// 绘制垂直条形。
    /// </summary>
    private void DrawVerticalBar(Graphics g, float x, float y, float width, float height, int index)
    {
        var colorIndex = index % 2 == 0 ? _barColor : _barColor2;
        var rect = new RectangleF(x, y, width, height);

        if (_useGradient)
        {
            using var gradientBrush = new LinearGradientBrush(
                rect,
                Color.FromArgb(180, colorIndex),
                colorIndex,
                LinearGradientMode.Vertical);
            g.FillRectangle(gradientBrush, rect);
        }
        else
        {
            using var brush = new SolidBrush(colorIndex);
            g.FillRectangle(brush, rect);
        }

        // 顶部高光
        using var highlightBrush = new SolidBrush(Color.FromArgb(60, 255, 255, 255));
        g.FillRectangle(highlightBrush, x, y, width, 5);
    }

    /// <summary>
    /// 绘制水平网格。
    /// </summary>
    private void DrawHorizontalGrid(Graphics g, float x, float y, float width, float height)
    {
        using var pen = new Pen(_gridColor, 1);

        // 垂直网格线
        var gridCount = 5;
        for (var i = 0; i <= gridCount; i++)
        {
            var lineX = x + width * i / gridCount;
            g.DrawLine(pen, lineX, y, lineX, y + height);

            // 刻度值
            using var font = new Font("Arial", 7f);
            using var brush = new SolidBrush(_labelColor);
            var value = _minimum + (_maximum - _minimum) * i / gridCount;
            var text = value.ToString("F0");
            g.DrawString(text, font, brush, lineX - 10, y + height + 3);
        }
    }

    /// <summary>
    /// 绘制垂直网格。
    /// </summary>
    private void DrawVerticalGrid(Graphics g, float x, float y, float width, float height)
    {
        using var pen = new Pen(_gridColor, 1);

        // 水平网格线
        var gridCount = 5;
        for (var i = 0; i <= gridCount; i++)
        {
            var lineY = y + height * i / gridCount;
            g.DrawLine(pen, x, lineY, x + width, lineY);

            // 刻度值
            using var font = new Font("Arial", 7f);
            using var brush = new SolidBrush(_labelColor);
            var value = _maximum - (_maximum - _minimum) * i / gridCount;
            var text = value.ToString("F0");
            g.DrawString(text, font, brush, 2, lineY - 5);
        }
    }

    #endregion

    #region 控件行为

    /// <inheritdoc />
    protected override Size DefaultSize => new Size(400, 200);

    #endregion
}
