using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Acme.Industrial.UI.Controls.Industrial;

/// <summary>
/// 液位计控件。
/// 显示容器液位，支持多种样式。
/// </summary>
[DefaultProperty(nameof(Value))]
public class LevelIndicator : Control
{
    #region 属性

    private double _value = 50;
    private double _minimum = 0;
    private double _maximum = 100;
    private Color _liquidColor = Color.FromArgb(0, 150, 200);
    private Color _liquidColor2 = Color.FromArgb(0, 100, 180);
    private Color _tankColor = Color.FromArgb(180, 180, 190);
    private Color _tankBorderColor = Color.FromArgb(120, 120, 130);
    private Color _gridColor = Color.FromArgb(200, 200, 200);
    private bool _showGrid = true;
    private bool _showScale = true;
    private bool _showBubbles = false;
    private int _bubbleCount = 5;
    private int _bubbleSpeed = 2;
    private float _waveAmplitude = 3;
    private bool _isHorizontal = false;
    private int _segmentCount = 0;
    private int _warningLevel = 70;
    private int _dangerLevel = 90;
    private Color _warningColor = Color.FromArgb(255, 200, 0);
    private Color _dangerColor = Color.FromArgb(220, 50, 50);

    /// <summary>
    /// 当前液位值（百分比）。
    /// </summary>
    [Category("数据")]
    [Description("当前液位值（0-100）")]
    [DefaultValue(50.0)]
    public double Value
    {
        get => _value;
        set
        {
            var clamped = Math.Max(_minimum, Math.Min(_maximum, value));
            if (Math.Abs(_value - clamped) > 0.001)
            {
                _value = clamped;
                Invalidate();
                OnValueChanged(EventArgs.Empty);
            }
        }
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
    /// 液体颜色。
    /// </summary>
    [Category("外观")]
    public Color LiquidColor
    {
        get => _liquidColor;
        set { _liquidColor = value; Invalidate(); }
    }

    /// <summary>
    /// 是否显示网格。
    /// </summary>
    [Category("行为")]
    [DefaultValue(true)]
    public bool ShowGrid
    {
        get => _showGrid;
        set { _showGrid = value; Invalidate(); }
    }

    /// <summary>
    /// 是否显示刻度。
    /// </summary>
    [Category("行为")]
    [DefaultValue(true)]
    public bool ShowScale
    {
        get => _showScale;
        set { _showScale = value; Invalidate(); }
    }

    /// <summary>
    /// 是否显示气泡动画。
    /// </summary>
    [Category("行为")]
    [DefaultValue(false)]
    public bool ShowBubbles
    {
        get => _showBubbles;
        set { _showBubbles = value; }
    }

    /// <summary>
    /// 气泡数量。
    /// </summary>
    [Category("外观")]
    [DefaultValue(5)]
    public int BubbleCount
    {
        get => _bubbleCount;
        set { _bubbleCount = Math.Max(0, value); }
    }

    /// <summary>
    /// 波浪振幅。
    /// </summary>
    [Category("外观")]
    [DefaultValue(3)]
    public float WaveAmplitude
    {
        get => _waveAmplitude;
        set { _waveAmplitude = Math.Max(0, value); Invalidate(); }
    }

    /// <summary>
    /// 是否水平显示。
    /// </summary>
    [Category("外观")]
    [DefaultValue(false)]
    public bool IsHorizontal
    {
        get => _isHorizontal;
        set { _isHorizontal = value; Invalidate(); }
    }

    /// <summary>
    /// 分段数量（0表示连续）。
    /// </summary>
    [Category("外观")]
    [DefaultValue(0)]
    public int SegmentCount
    {
        get => _segmentCount;
        set { _segmentCount = Math.Max(0, value); Invalidate(); }
    }

    #endregion

    #region 字段

    private readonly List<Bubble> _bubbles = new();
    private readonly Random _random = new();
    private System.Windows.Forms.Timer? _animationTimer;

    private class Bubble
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Size { get; set; }
        public float Speed { get; set; }
        public float Wobble { get; set; }
    }

    #endregion

    #region 事件

    /// <summary>
    /// 值变更时触发。
    /// </summary>
    public event EventHandler? ValueChanged;

    /// <summary>
    /// 触发值变更事件。
    /// </summary>
    protected virtual void OnValueChanged(EventArgs e)
    {
        ValueChanged?.Invoke(this, e);
    }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化液位计。
    /// </summary>
    public LevelIndicator()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        Size = new Size(100, 200);

        _animationTimer = new System.Windows.Forms.Timer();
        _animationTimer.Interval = 50;
        _animationTimer.Tick += AnimationTimer_Tick;
        _animationTimer.Start();

        InitializeBubbles();
    }

    private void InitializeBubbles()
    {
        _bubbles.Clear();
        for (var i = 0; i < _bubbleCount; i++)
        {
            _bubbles.Add(new Bubble
            {
                X = (float)_random.NextDouble(),
                Y = (float)_random.NextDouble(),
                Size = 3 + (float)_random.NextDouble() * 5,
                Speed = 0.5f + (float)_random.NextDouble() * 1.5f,
                Wobble = (float)_random.NextDouble() * 10
            });
        }
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        if (!_showBubbles || _bubbles.Count == 0) return;

        foreach (var bubble in _bubbles)
        {
            bubble.Y -= bubble.Speed * 0.02f;
            bubble.Wobble += 0.1f;

            if (bubble.Y < 0)
            {
                bubble.Y = 1;
                bubble.X = (float)_random.NextDouble();
            }
        }

        Invalidate();
    }

    #endregion

    #region 方法

    /// <summary>
    /// 获取当前液位颜色。
    /// </summary>
    public Color GetCurrentColor()
    {
        var ratio = (_value - _minimum) / (_maximum - _minimum) * 100;
        if (ratio >= _dangerLevel) return _dangerColor;
        if (ratio >= _warningLevel) return _warningColor;
        return _liquidColor;
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
            System.Diagnostics.Debug.WriteLine($"LevelIndicator OnPaint error: {ex.Message}");
        }
    }

    /// <summary>
    /// 绘制垂直液位计。
    /// </summary>
    private void PaintVertical(Graphics g)
    {
        var tankWidth = ClientSize.Width - 40;
        var tankHeight = ClientSize.Height - 20;
        var tankX = 20;
        var tankY = 10;
        var tankRadius = 10;

        // 绘制容器背景
        DrawTank(g, tankX, tankY, tankWidth, tankHeight, tankRadius);

        // 绘制网格
        if (_showGrid)
            DrawGrid(g, tankX, tankY, tankWidth, tankHeight);

        // 绘制液位
        DrawLiquid(g, tankX, tankY, tankWidth, tankHeight, tankRadius);

        // 绘制气泡
        if (_showBubbles)
            DrawBubbles(g, tankX, tankY, tankWidth, tankHeight);

        // 绘制刻度
        if (_showScale)
            DrawScale(g, tankX, tankY, tankWidth, tankHeight);

        // 绘制百分比
        DrawPercentage(g, tankX, tankY, tankWidth, tankHeight);
    }

    /// <summary>
    /// 绘制水平液位计。
    /// </summary>
    private void PaintHorizontal(Graphics g)
    {
        var tankHeight = ClientSize.Height - 40;
        var tankWidth = ClientSize.Width - 20;
        var tankX = 10;
        var tankY = 20;
        var tankRadius = 10;

        DrawTank(g, tankX, tankY, tankWidth, tankHeight, tankRadius);

        if (_showGrid)
            DrawGrid(g, tankX, tankY, tankWidth, tankHeight);

        DrawHorizontalLiquid(g, tankX, tankY, tankWidth, tankHeight, tankRadius);

        if (_showScale)
            DrawHorizontalScale(g, tankX, tankY, tankWidth, tankHeight);
    }

    /// <summary>
    /// 绘制容器。
    /// </summary>
    private void DrawTank(Graphics g, float x, float y, float width, float height, float radius)
    {
        // 容器路径
        using var tankPath = CreateRoundedRectPath(x, y, width, height, radius);

        // 渐变背景
        using var gradientBrush = new LinearGradientBrush(
            new RectangleF(x, y, width, height),
            Color.FromArgb(220, 220, 230),
            Color.FromArgb(180, 180, 190),
            LinearGradientMode.Vertical);
        g.FillPath(gradientBrush, tankPath);

        // 边框
        using var borderPen = new Pen(_tankBorderColor, 2);
        g.DrawPath(borderPen, tankPath);

        // 高光效果
        using var highlightBrush = new SolidBrush(Color.FromArgb(40, 255, 255, 255));
        var highlightPath = CreateRoundedRectPath(x + 3, y + 3, width * 0.3f, height - 6, radius / 2);
        g.FillPath(highlightBrush, highlightPath);
    }

    /// <summary>
    /// 绘制网格。
    /// </summary>
    private void DrawGrid(Graphics g, float x, float y, float width, float height)
    {
        using var pen = new Pen(_gridColor, 0.5f);

        // 水平线
        for (var i = 1; i < 10; i++)
        {
            var lineY = y + height * i / 10;
            g.DrawLine(pen, x + 2, lineY, x + width - 2, lineY);
        }

        // 垂直线
        for (var i = 1; i < 5; i++)
        {
            var lineX = x + width * i / 5;
            g.DrawLine(pen, lineX, y + 2, lineX, y + height - 2);
        }
    }

    /// <summary>
    /// 绘制液位。
    /// </summary>
    private void DrawLiquid(Graphics g, float x, float y, float width, float height, float radius)
    {
        var ratio = (_value - _minimum) / (_maximum - _minimum);
        ratio = Math.Max(0, Math.Min(1, ratio));

        var liquidHeight = height * (float)ratio;
        var liquidTop = y + height - liquidHeight;

        // 波浪效果
        var waveY = liquidTop;
        var points = new List<PointF>();

        for (var i = 0; i <= width; i += 2)
        {
            var waveOffset = (float)Math.Sin((i + DateTime.Now.Millisecond / 10.0) * 0.1) * _waveAmplitude;
            points.Add(new PointF(x + i, waveY + waveOffset));
        }

        // 添加液面以下的部分
        points.Add(new PointF(x + width, y + height));
        points.Add(new PointF(x, y + height));

        // 液面渐变
        using var liquidBrush = new LinearGradientBrush(
            new RectangleF(x, liquidTop, width, liquidHeight),
            GetCurrentColor(),
            Color.FromArgb(150, GetCurrentColor()),
            LinearGradientMode.Vertical);

        // 填充路径
        using var path = new GraphicsPath();
        path.AddPolygon(points.ToArray());
        path.AddRectangle(new RectangleF(x, waveY, width, y + height - waveY));

        g.SetClip(CreateRoundedRectPath(x + 2, y + 2, width - 4, height - 4, radius - 2));
        g.FillPath(liquidBrush, path);
        g.ResetClip();

        // 液面波浪线
        using var wavePen = new Pen(Color.FromArgb(100, 255, 255, 255), 2);
        g.DrawLines(wavePen, points.ToArray());
    }

    /// <summary>
    /// 绘制水平液位。
    /// </summary>
    private void DrawHorizontalLiquid(Graphics g, float x, float y, float width, float height, float radius)
    {
        var ratio = (_value - _minimum) / (_maximum - _minimum);
        ratio = Math.Max(0, Math.Min(1, ratio));

        var liquidWidth = width * (float)ratio;

        using var liquidBrush = new SolidBrush(GetCurrentColor());
        using var path = CreateRoundedRectPath(x + 2, y + 2, liquidWidth, height - 4, radius - 2);
        g.FillPath(liquidBrush, path);
    }

    /// <summary>
    /// 绘制气泡。
    /// </summary>
    private void DrawBubbles(Graphics g, float x, float y, float width, float height)
    {
        var liquidRatio = (_value - _minimum) / (_maximum - _minimum);
        if (liquidRatio <= 0) return;

        var liquidTop = y + height * (1 - (float)liquidRatio);

        using var bubbleBrush = new SolidBrush(Color.FromArgb(150, 255, 255, 255));

        foreach (var bubble in _bubbles)
        {
            var bubbleY = y + height - bubble.Y * height;
            if (bubbleY < liquidTop) continue;

            var wobbleX = (float)Math.Sin(bubble.Wobble) * 3;
            var bubbleX = x + bubble.X * width + wobbleX;

            g.FillEllipse(bubbleBrush, bubbleX - bubble.Size / 2, bubbleY - bubble.Size / 2, bubble.Size, bubble.Size);
        }
    }

    /// <summary>
    /// 绘制刻度。
    /// </summary>
    private void DrawScale(Graphics g, float x, float y, float width, float height)
    {
        using var font = new Font("Arial", 8f);
        using var pen = new Pen(Color.FromArgb(100, 100, 100), 1);
        using var brush = new SolidBrush(Color.FromArgb(100, 100, 100));

        for (var i = 0; i <= 10; i++)
        {
            var labelY = y + height * (10 - i) / 10;
            var label = $"{i * 10}%";

            g.DrawLine(pen, x - 15, labelY, x - 5, labelY);
            g.DrawString(label, font, brush, x - 45, labelY - 6);
        }
    }

    /// <summary>
    /// 绘制水平刻度。
    /// </summary>
    private void DrawHorizontalScale(Graphics g, float x, float y, float width, float height)
    {
        using var font = new Font("Arial", 8f);
        using var pen = new Pen(Color.FromArgb(100, 100, 100), 1);
        using var brush = new SolidBrush(Color.FromArgb(100, 100, 100));

        for (var i = 0; i <= 10; i++)
        {
            var labelX = x + width * i / 10;
            var label = $"{i * 10}%";

            g.DrawLine(pen, labelX, y + height + 5, labelX, y + height + 15);
            g.DrawString(label, font, brush, labelX - 10, y + height + 17);
        }
    }

    /// <summary>
    /// 绘制百分比。
    /// </summary>
    private void DrawPercentage(Graphics g, float x, float y, float width, float height)
    {
        using var font = new Font("Arial", 12f, FontStyle.Bold);
        using var brush = new SolidBrush(Color.FromArgb(50, 50, 50));
        var text = $"{_value:F1}%";
        var textSize = g.MeasureString(text, font);
        g.DrawString(text, font, brush, x + (width - textSize.Width) / 2, y + height / 2 - textSize.Height / 2);
    }

    /// <summary>
    /// 创建圆角矩形路径。
    /// </summary>
    private static GraphicsPath CreateRoundedRectPath(float x, float y, float width, float height, float radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;

        path.AddArc(x, y, diameter, diameter, 180, 90);
        path.AddArc(x + width - diameter, y, diameter, diameter, 270, 90);
        path.AddArc(x + width - diameter, y + height - diameter, diameter, diameter, 0, 90);
        path.AddArc(x, y + height - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }

    #endregion

    #region 控件行为

    /// <inheritdoc />
    protected override Size DefaultSize => new Size(100, 200);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _animationTimer?.Stop();
            _animationTimer?.Dispose();
        }
        base.Dispose(disposing);
    }

    #endregion
}
