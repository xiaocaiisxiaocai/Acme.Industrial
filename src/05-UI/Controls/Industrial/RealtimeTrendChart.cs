using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Acme.Industrial.UI.Controls.Industrial;

/// <summary>
/// 曲线配置。
/// </summary>
public class CurveConfig
{
    /// <summary>
    /// 曲线名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 曲线颜色。
    /// </summary>
    public Color Color { get; set; } = Color.Lime;

    /// <summary>
    /// 是否可见。
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 线宽。
    /// </summary>
    public float LineWidth { get; set; } = 2f;

    /// <summary>
    /// Y轴最小值。
    /// </summary>
    public double MinValue { get; set; } = 0;

    /// <summary>
    /// Y轴最大值。
    /// </summary>
    public double MaxValue { get; set; } = 100;

    /// <summary>
    /// 是否自动调整Y轴范围。
    /// </summary>
    public bool AutoScale { get; set; } = true;
}

/// <summary>
/// 数据点。
/// </summary>
public class DataPoint
{
    /// <summary>
    /// 时间戳。
    /// </summary>
    public DateTime Time { get; set; }

    /// <summary>
    /// 数值。
    /// </summary>
    public double Value { get; set; }
}

/// <summary>
/// 实时趋势图控件。
/// 支持多条曲线实时显示、数据滚动、缩放等功能。
/// </summary>
[DefaultProperty(nameof(Caption))]
public class RealtimeTrendChart : Control
{
    #region 属性

    private string _caption = "实时趋势";
    private int _timeSpanSeconds = 60;
    private int _maxDataPoints = 600;
    private bool _showGrid = true;
    private bool _showLegend = true;
    private bool _showCaption = true;
    private Color _gridColor = Color.FromArgb(40, 40, 40);
    private Color _backgroundColor = Color.FromArgb(20, 20, 20);
    private Color _foregroundColor = Color.FromArgb(200, 200, 200);
    private bool _autoScroll = true;
    private bool _showTimeAxis = true;
    private bool _showValueAxis = true;
    private int _gridLinesX = 6;
    private int _gridLinesY = 5;
    private bool _allowZoom = true;
    private double _zoomLevel = 1.0;
    private DateTime _startTime = DateTime.Now;

    /// <summary>
    /// 获取或设置标题。
    /// </summary>
    [Category("外观")]
    [Description("图表标题")]
    [DefaultValue("实时趋势")]
    public string Caption
    {
        get => _caption;
        set
        {
            if (_caption != value)
            {
                _caption = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置时间跨度（秒）。
    /// </summary>
    [Category("数据")]
    [Description("显示的时间跨度（秒）")]
    [DefaultValue(60)]
    public int TimeSpanSeconds
    {
        get => _timeSpanSeconds;
        set
        {
            if (value < 1) value = 1;
            if (_timeSpanSeconds != value)
            {
                _timeSpanSeconds = value;
                _maxDataPoints = value * 10;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置是否显示网格。
    /// </summary>
    [Category("外观")]
    [Description("是否显示网格线")]
    [DefaultValue(true)]
    public bool ShowGrid
    {
        get => _showGrid;
        set
        {
            if (_showGrid != value)
            {
                _showGrid = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置是否显示图例。
    /// </summary>
    [Category("外观")]
    [Description("是否显示图例")]
    [DefaultValue(true)]
    public bool ShowLegend
    {
        get => _showLegend;
        set
        {
            if (_showLegend != value)
            {
                _showLegend = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置是否显示标题。
    /// </summary>
    [Category("外观")]
    [Description("是否显示标题")]
    [DefaultValue(true)]
    public bool ShowCaption
    {
        get => _showCaption;
        set
        {
            if (_showCaption != value)
            {
                _showCaption = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置是否自动滚动。
    /// </summary>
    [Category("行为")]
    [Description("是否自动滚动显示最新数据")]
    [DefaultValue(true)]
    public bool AutoScroll
    {
        get => _autoScroll;
        set
        {
            if (_autoScroll != value)
            {
                _autoScroll = value;
            }
        }
    }

    /// <summary>
    /// 获取或设置网格颜色。
    /// </summary>
    [Category("外观")]
    [Description("网格线颜色")]
    public Color GridColor
    {
        get => _gridColor;
        set
        {
            if (_gridColor != value)
            {
                _gridColor = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置缩放级别。
    /// </summary>
    [Category("行为")]
    [Description("缩放级别，1.0表示原始大小")]
    [DefaultValue(1.0)]
    public double ZoomLevel
    {
        get => _zoomLevel;
        set
        {
            if (value < 0.1) value = 0.1;
            if (value > 10) value = 10;
            if (_zoomLevel != value)
            {
                _zoomLevel = value;
                Invalidate();
            }
        }
    }

    #endregion

    #region 字段

    private readonly ConcurrentDictionary<string, List<DataPoint>> _curveData = new();
    private readonly ConcurrentDictionary<string, CurveConfig> _curveConfigs = new();
    private readonly object _lockObject = new();
    private System.Windows.Forms.Timer? _refreshTimer;
    private Rectangle _chartArea;
    private Rectangle _captionArea;
    private Rectangle _legendArea;
    private Rectangle _timeAxisArea;
    private Rectangle _valueAxisArea;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化实时趋势图。
    /// </summary>
    public RealtimeTrendChart()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        Size = new Size(500, 300);

        _refreshTimer = new System.Windows.Forms.Timer();
        _refreshTimer.Interval = 100;
        _refreshTimer.Tick += RefreshTimer_Tick;
        _refreshTimer.Start();
    }

    static RealtimeTrendChart()
    {
        // 默认曲线配置
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 添加曲线。
    /// </summary>
    public void AddCurve(string curveName, Color? color = null)
    {
        var config = new CurveConfig
        {
            Name = curveName,
            Color = color ?? GetNextColor()
        };

        _curveConfigs[curveName] = config;
        _curveData[curveName] = new List<DataPoint>();
    }

    /// <summary>
    /// 移除曲线。
    /// </summary>
    public void RemoveCurve(string curveName)
    {
        _curveConfigs.TryRemove(curveName, out _);
        _curveData.TryRemove(curveName, out _);
        Invalidate();
    }

    /// <summary>
    /// 添加数据点。
    /// </summary>
    public void AddDataPoint(string curveName, double value)
    {
        AddDataPoint(curveName, value, DateTime.Now);
    }

    /// <summary>
    /// 添加数据点。
    /// </summary>
    public void AddDataPoint(string curveName, double value, DateTime time)
    {
        if (!_curveData.ContainsKey(curveName))
        {
            AddCurve(curveName);
        }

        var point = new DataPoint { Time = time, Value = value };

        lock (_lockObject)
        {
            var data = _curveData[curveName];
            data.Add(point);

            // 限制数据点数量
            while (data.Count > _maxDataPoints)
            {
                data.RemoveAt(0);
            }
        }
    }

    /// <summary>
    /// 批量添加数据点。
    /// </summary>
    public void AddDataPoints(IEnumerable<(string curveName, double value)> points)
    {
        var now = DateTime.Now;
        foreach (var (curveName, value) in points)
        {
            AddDataPoint(curveName, value, now);
        }
    }

    /// <summary>
    /// 清空所有数据。
    /// </summary>
    public void ClearAll()
    {
        lock (_lockObject)
        {
            foreach (var key in _curveData.Keys)
            {
                _curveData[key].Clear();
            }
        }
        Invalidate();
    }

    /// <summary>
    /// 清空指定曲线的数据。
    /// </summary>
    public void ClearCurve(string curveName)
    {
        if (_curveData.TryGetValue(curveName, out var data))
        {
            lock (_lockObject)
            {
                data.Clear();
            }
            Invalidate();
        }
    }

    /// <summary>
    /// 获取曲线配置。
    /// </summary>
    public CurveConfig? GetCurveConfig(string curveName)
    {
        return _curveConfigs.TryGetValue(curveName, out var config) ? config : null;
    }

    /// <summary>
    /// 设置曲线配置。
    /// </summary>
    public void SetCurveConfig(string curveName, CurveConfig config)
    {
        _curveConfigs[curveName] = config;
        Invalidate();
    }

    /// <summary>
    /// 获取曲线当前值。
    /// </summary>
    public double? GetCurrentValue(string curveName)
    {
        if (_curveData.TryGetValue(curveName, out var data) && data.Count > 0)
        {
            return data[^1].Value;
        }
        return null;
    }

    /// <summary>
    /// 缩放。
    /// </summary>
    public void Zoom(double factor)
    {
        ZoomLevel *= factor;
    }

    /// <summary>
    /// 暂停刷新。
    /// </summary>
    public void Pause()
    {
        _refreshTimer?.Stop();
    }

    /// <summary>
    /// 恢复刷新。
    /// </summary>
    public void Resume()
    {
        _refreshTimer?.Start();
    }

    #endregion

    #region 事件

    /// <summary>
    /// 数据更新时触发。
    /// </summary>
    public event EventHandler? DataUpdated;

    /// <summary>
    /// 触发数据更新事件。
    /// </summary>
    protected virtual void OnDataUpdated()
    {
        DataUpdated?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region 内部方法

    private static int _colorIndex = 0;
    private static readonly Color[] DefaultColors =
    {
        Color.Lime, Color.Red, Color.Yellow, Color.Cyan, Color.Magenta, Color.Orange, Color.White, Color.Pink
    };

    private static Color GetNextColor()
    {
        var color = DefaultColors[_colorIndex % DefaultColors.Length];
        _colorIndex++;
        return color;
    }

    private void RefreshTimer_Tick(object? sender, EventArgs e)
    {
        if (_autoScroll)
        {
            Invalidate();
            OnDataUpdated();
        }
    }

    /// <summary>
    /// 计算图表区域。
    /// </summary>
    private void CalculateAreas()
    {
        var captionHeight = _showCaption ? 25 : 0;
        var legendHeight = _showLegend && _curveConfigs.Count > 0 ? 25 : 0;
        var timeAxisHeight = _showTimeAxis ? 25 : 0;
        var valueAxisWidth = _showValueAxis ? 50 : 0;

        _captionArea = new Rectangle(0, 0, ClientSize.Width, captionHeight);
        _legendArea = new Rectangle(0, captionHeight, ClientSize.Width, legendHeight);
        _chartArea = new Rectangle(valueAxisWidth, captionHeight + legendHeight,
            ClientSize.Width - valueAxisWidth, ClientSize.Height - captionHeight - legendHeight - timeAxisHeight);
        _timeAxisArea = new Rectangle(valueAxisWidth, _chartArea.Bottom,
            ClientSize.Width - valueAxisWidth, timeAxisHeight);
        _valueAxisArea = new Rectangle(0, _chartArea.Top, valueAxisWidth, _chartArea.Height);
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

            CalculateAreas();

            // 绘制背景
            using var bgBrush = new SolidBrush(_backgroundColor);
            g.FillRectangle(bgBrush, ClientRectangle);

            // 绘制标题
            if (_showCaption)
            {
                DrawCaption(g);
            }

            // 绘制图表区域背景
            using var chartBgBrush = new SolidBrush(Color.FromArgb(30, 30, 30));
            g.FillRectangle(chartBgBrush, _chartArea);

            // 绘制网格
            if (_showGrid)
            {
                DrawGrid(g);
            }

            // 绘制曲线
            DrawCurves(g);

            // 绘制坐标轴
            if (_showValueAxis)
            {
                DrawValueAxis(g);
            }

            if (_showTimeAxis)
            {
                DrawTimeAxis(g);
            }

            // 绘制图例
            if (_showLegend)
            {
                DrawLegend(g);
            }

            // 绘制边框
            using var borderPen = new Pen(Color.FromArgb(60, 60, 60), 1);
            g.DrawRectangle(borderPen, _chartArea.Left, _chartArea.Top, _chartArea.Width - 1, _chartArea.Height - 1);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RealtimeTrendChart OnPaint error: {ex.Message}");
        }
    }

    /// <summary>
    /// 绘制标题。
    /// </summary>
    private void DrawCaption(Graphics g)
    {
        using var font = new Font("微软雅黑", 10F, FontStyle.Bold);
        using var brush = new SolidBrush(_foregroundColor);
        var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString(_caption, font, brush, _captionArea, format);
    }

    /// <summary>
    /// 绘制网格。
    /// </summary>
    private void DrawGrid(Graphics g)
    {
        using var gridPen = new Pen(_gridColor, 1);

        // 垂直网格线
        for (var i = 0; i <= _gridLinesX; i++)
        {
            var x = _chartArea.Left + (float)_chartArea.Width * i / _gridLinesX;
            g.DrawLine(gridPen, x, _chartArea.Top, x, _chartArea.Bottom);
        }

        // 水平网格线
        for (var i = 0; i <= _gridLinesY; i++)
        {
            var y = _chartArea.Top + (float)_chartArea.Height * i / _gridLinesY;
            g.DrawLine(gridPen, _chartArea.Left, y, _chartArea.Right, y);
        }
    }

    /// <summary>
    /// 绘制曲线。
    /// </summary>
    private void DrawCurves(Graphics g)
    {
        lock (_lockObject)
        {
            var now = DateTime.Now;
            var visibleStartTime = now.AddSeconds(-_timeSpanSeconds * _zoomLevel);
            var effectiveTimeSpan = _timeSpanSeconds * _zoomLevel;

            foreach (var (curveName, config) in _curveConfigs)
            {
                if (!config.IsVisible) continue;
                if (!_curveData.TryGetValue(curveName, out var data) || data.Count == 0) continue;

                using var pen = new Pen(config.Color, config.LineWidth);
                pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                var points = new List<PointF>();
                foreach (var point in data)
                {
                    if (point.Time < visibleStartTime) continue;

                    var xRatio = (point.Time - visibleStartTime).TotalSeconds / effectiveTimeSpan;
                    var yRatio = (config.MaxValue - config.MinValue) == 0 ? 0.5 :
                        (point.Value - config.MinValue) / (config.MaxValue - config.MinValue);

                    xRatio = Math.Max(0, Math.Min(1, xRatio));
                    yRatio = Math.Max(0, Math.Min(1, yRatio));

                    var x = _chartArea.Left + xRatio * _chartArea.Width;
                    var y = _chartArea.Bottom - yRatio * _chartArea.Height;

                    points.Add(new PointF((float)x, (float)y));
                }

                if (points.Count > 1)
                {
                    using var path = new System.Drawing.Drawing2D.GraphicsPath();
                    path.AddLines(points.ToArray());
                    g.DrawPath(pen, path);
                }
            }
        }
    }

    /// <summary>
    /// 绘制数值轴。
    /// </summary>
    private void DrawValueAxis(Graphics g)
    {
        using var font = new Font("Consolas", 8F);
        using var brush = new SolidBrush(_foregroundColor);

        // 找到所有曲线的Y轴范围
        var minValue = double.MaxValue;
        var maxValue = double.MinValue;

        foreach (var (curveName, config) in _curveConfigs)
        {
            if (!config.IsVisible) continue;
            if (!config.AutoScale && config.MinValue != config.MaxValue)
            {
                minValue = Math.Min(minValue, config.MinValue);
                maxValue = Math.Max(maxValue, config.MaxValue);
            }
            else if (_curveData.TryGetValue(curveName, out var data))
            {
                foreach (var point in data)
                {
                    minValue = Math.Min(minValue, point.Value);
                    maxValue = Math.Max(maxValue, point.Value);
                }
            }
        }

        if (minValue == double.MaxValue) minValue = 0;
        if (maxValue == double.MinValue) maxValue = 100;
        if (minValue == maxValue) { minValue -= 10; maxValue += 10; }

        // 添加边距
        var range = maxValue - minValue;
        minValue -= range * 0.05;
        maxValue += range * 0.05;

        var format = new StringFormat
        {
            Alignment = StringAlignment.Far,
            LineAlignment = StringAlignment.Center
        };

        for (var i = 0; i <= _gridLinesY; i++)
        {
            var y = _chartArea.Top + (float)_chartArea.Height * i / _gridLinesY;
            var value = maxValue - (maxValue - minValue) * i / _gridLinesY;
            var label = value.ToString("F1");
            var rect = new RectangleF(_valueAxisArea.Left, y - 10, _valueAxisArea.Width - 5, 20);
            g.DrawString(label, font, brush, rect, format);
        }
    }

    /// <summary>
    /// 绘制时间轴。
    /// </summary>
    private void DrawTimeAxis(Graphics g)
    {
        using var font = new Font("Consolas", 8F);
        using var brush = new SolidBrush(_foregroundColor);

        var now = DateTime.Now;
        var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Near
        };

        for (var i = 0; i <= _gridLinesX; i++)
        {
            var x = _chartArea.Left + (float)_chartArea.Width * i / _gridLinesX;
            var time = now.AddSeconds(-_timeSpanSeconds * _zoomLevel * (1 - (double)i / _gridLinesX));
            var label = time.ToString("HH:mm:ss");
            var rect = new RectangleF(x - 30, _timeAxisArea.Top + 2, 60, _timeAxisArea.Height - 4);
            g.DrawString(label, font, brush, rect, format);
        }
    }

    /// <summary>
    /// 绘制图例。
    /// </summary>
    private void DrawLegend(Graphics g)
    {
        using var font = new Font("微软雅黑", 8F);
        var totalWidth = 0f;
        var itemHeight = 20f;
        var x = _chartArea.Left + 10f;

        foreach (var (curveName, config) in _curveConfigs)
        {
            var textWidth = g.MeasureString(curveName, font).Width + 25;

            if (totalWidth + textWidth > _chartArea.Width - 20)
            {
                x = _chartArea.Left + 10f;
                totalWidth = 0;
            }

            // 绘制颜色条
            using var colorBrush = new SolidBrush(config.Color);
            g.FillRectangle(colorBrush, x, _legendArea.Top + 5, 15, 10);

            // 绘制名称
            using var textBrush = new SolidBrush(_foregroundColor);
            g.DrawString(curveName, font, textBrush, x + 18, _legendArea.Top + 3);

            x += textWidth + 10;
            totalWidth += textWidth + 10;
        }
    }

    #endregion

    #region 控件行为

    /// <inheritdoc />
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);

        if (_allowZoom && Control.ModifierKeys == Keys.Control)
        {
            var factor = e.Delta > 0 ? 0.9 : 1.1;
            ZoomLevel *= factor;
        }
    }

    /// <inheritdoc />
    protected override Size DefaultSize => new Size(500, 300);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
        }
        base.Dispose(disposing);
    }

    #endregion
}
