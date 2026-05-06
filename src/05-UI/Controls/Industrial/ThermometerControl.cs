using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Acme.Industrial.UI.Controls.Industrial;

/// <summary>
/// 温度计类型。
/// </summary>
public enum ThermometerStyle
{
    /// <summary>垂直。</summary>
    Vertical,

    /// <summary>水平。</summary>
    Horizontal
}

/// <summary>
/// 温度计控件。
/// 显示温度值，支持警告和危险温度区域。
/// </summary>
[DefaultProperty(nameof(Value))]
public class ThermometerControl : Control
{
    #region 属性

    private double _value = 20;
    private double _minimum = -20;
    private double _maximum = 120;
    private double _warningValue = 60;
    private double _dangerValue = 80;
    private double _targetValue = double.NaN;
    private Color _normalColor = Color.FromArgb(50, 150, 220);
    private Color _warningColor = Color.FromArgb(255, 200, 0);
    private Color _dangerColor = Color.FromArgb(220, 50, 50);
    private Color _tubeColor = Color.FromArgb(200, 220, 240);
    private Color _backgroundColor = Color.FromArgb(240, 240, 240);
    private ThermometerStyle _style = ThermometerStyle.Vertical;
    private bool _showBulb = true;
    private bool _showScale = true;
    private bool _showZones = true;
    private bool _showTarget = false;
    private int _majorTickCount = 8;
    private int _minorTickCount = 4;
    private int _tubeWidth = 30;
    private int _bulbRadius = 25;

    /// <summary>
    /// 当前温度值。
    /// </summary>
    [Category("数据")]
    [Description("当前温度值")]
    [DefaultValue(20.0)]
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
    [Description("温度最小值")]
    [DefaultValue(-20.0)]
    public double Minimum
    {
        get => _minimum;
        set { _minimum = value; Invalidate(); }
    }

    /// <summary>
    /// 最大值。
    /// </summary>
    [Category("数据")]
    [Description("温度最大值")]
    [DefaultValue(120.0)]
    public double Maximum
    {
        get => _maximum;
        set { _maximum = value; Invalidate(); }
    }

    /// <summary>
    /// 警告温度阈值。
    /// </summary>
    [Category("外观")]
    [Description("警告温度阈值")]
    [DefaultValue(60.0)]
    public double WarningValue
    {
        get => _warningValue;
        set { _warningValue = value; Invalidate(); }
    }

    /// <summary>
    /// 危险温度阈值。
    /// </summary>
    [Category("外观")]
    [Description("危险温度阈值")]
    [DefaultValue(80.0)]
    public double DangerValue
    {
        get => _dangerValue;
        set { _dangerValue = value; Invalidate(); }
    }

    /// <summary>
    /// 目标温度。
    /// </summary>
    [Category("数据")]
    [Description("目标温度标记")]
    [DefaultValue(double.NaN)]
    public double TargetValue
    {
        get => _targetValue;
        set
        {
            _targetValue = value;
            _showTarget = !double.IsNaN(value);
            Invalidate();
        }
    }

    /// <summary>
    /// 正常温度颜色。
    /// </summary>
    [Category("外观")]
    public Color NormalColor
    {
        get => _normalColor;
        set { _normalColor = value; Invalidate(); }
    }

    /// <summary>
    /// 警告温度颜色。
    /// </summary>
    [Category("外观")]
    public Color WarningColor
    {
        get => _warningColor;
        set { _warningColor = value; Invalidate(); }
    }

    /// <summary>
    /// 危险温度颜色。
    /// </summary>
    [Category("外观")]
    public Color DangerColor
    {
        get => _dangerColor;
        set { _dangerColor = value; Invalidate(); }
    }

    /// <summary>
    /// 显示样式。
    /// </summary>
    [Category("外观")]
    [DefaultValue(ThermometerStyle.Vertical)]
    public ThermometerStyle Style
    {
        get => _style;
        set { _style = value; Invalidate(); }
    }

    /// <summary>
    /// 是否显示球泡。
    /// </summary>
    [Category("行为")]
    [DefaultValue(true)]
    public bool ShowBulb
    {
        get => _showBulb;
        set { _showBulb = value; Invalidate(); }
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
    /// 是否显示温度区域。
    /// </summary>
    [Category("行为")]
    [DefaultValue(true)]
    public bool ShowZones
    {
        get => _showZones;
        set { _showZones = value; Invalidate(); }
    }

    /// <summary>
    /// 主刻度数量。
    /// </summary>
    [Category("外观")]
    [DefaultValue(8)]
    public int MajorTickCount
    {
        get => _majorTickCount;
        set { _majorTickCount = Math.Max(2, value); Invalidate(); }
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
    /// 初始化温度计控件。
    /// </summary>
    public ThermometerControl()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        Size = new Size(80, 200);
    }

    #endregion

    #region 方法

    /// <summary>
    /// 获取当前温度对应的颜色。
    /// </summary>
    public Color GetCurrentColor()
    {
        if (_value >= _dangerValue) return _dangerColor;
        if (_value >= _warningValue) return _warningColor;
        return _normalColor;
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

            if (_style == ThermometerStyle.Vertical)
                PaintVertical(g);
            else
                PaintHorizontal(g);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ThermometerControl OnPaint error: {ex.Message}");
        }
    }

    /// <summary>
    /// 绘制垂直温度计。
    /// </summary>
    private void PaintVertical(Graphics g)
    {
        var totalWidth = ClientSize.Width;
        var totalHeight = ClientSize.Height;

        // 中心X坐标
        var centerX = totalWidth / 2f;

        // 刻度宽度
        var scaleWidth = 30;
        var scaleX = totalWidth - scaleWidth - 5;

        // 管子区域
        var tubeX = centerX - _tubeWidth / 2f;
        var bulbY = totalHeight - _bulbRadius - 5;
        var tubeTop = _bulbRadius + 5;
        var tubeHeight = bulbY - tubeTop;

        // 绘制外壳轮廓
        DrawThermometerShell(g, centerX, tubeTop, tubeX, bulbY, tubeHeight);

        // 绘制温度区域背景
        DrawTemperatureBackground(g, tubeX, tubeTop, bulbY, tubeHeight);

        // 绘制温度液柱
        DrawTemperatureLiquid(g, tubeX, tubeTop, bulbY, tubeHeight);

        // 绘制球泡
        if (_showBulb)
        {
            DrawBulb(g, centerX, bulbY);
        }

        // 绘制刻度
        if (_showScale)
        {
            DrawScale(g, scaleX, tubeTop, tubeHeight);
        }

        // 绘制数值
        DrawValue(g, centerX, bulbY);
    }

    /// <summary>
    /// 绘制水平温度计。
    /// </summary>
    private void PaintHorizontal(Graphics g)
    {
        var totalWidth = ClientSize.Width;
        var totalHeight = ClientSize.Height;

        var centerY = totalHeight / 2f;

        // 管子区域
        var tubeY = centerY - _tubeWidth / 2f;
        var bulbX = totalWidth - _bulbRadius - 5;
        var tubeLeft = _bulbRadius + 5;
        var tubeWidth = bulbX - tubeLeft;

        // 绘制外壳轮廓
        DrawHorizontalShell(g, centerY, tubeLeft, tubeY, bulbX, tubeWidth);

        // 绘制温度区域背景
        DrawHorizontalTemperatureBackground(g, tubeLeft, tubeY, bulbX);

        // 绘制温度液柱
        DrawHorizontalTemperatureLiquid(g, tubeLeft, tubeY, bulbX, tubeWidth);

        // 绘制球泡
        if (_showBulb)
        {
            DrawHorizontalBulb(g, bulbX, centerY);
        }

        // 绘制刻度
        if (_showScale)
        {
            DrawHorizontalScale(g, tubeLeft, tubeY - 15, tubeWidth);
        }
    }

    /// <summary>
    /// 绘制温度计外壳。
    /// </summary>
    private void DrawThermometerShell(Graphics g, float centerX, float tubeTop, float tubeX, float bulbY, float tubeHeight)
    {
        // 外壳路径
        using var shellPath = new GraphicsPath();
        var tubeRadius = _tubeWidth / 2f;
        var outerRadius = _bulbRadius + 3;

        // 管子部分
        shellPath.AddRectangle(new RectangleF(tubeX - 3, tubeTop, _tubeWidth + 6, tubeHeight));

        // 上方圆角
        shellPath.AddArc(tubeX - 3, tubeTop - tubeRadius - 3, tubeRadius * 2 + 6, tubeRadius * 2 + 6, 180, 180);

        // 球泡部分
        shellPath.AddEllipse(centerX - outerRadius, bulbY - outerRadius, outerRadius * 2, outerRadius * 2);

        // 阴影
        using var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0));
        g.FillPath(shadowBrush, shellPath);

        // 外壳渐变
        using var gradientBrush = new LinearGradientBrush(
            new RectangleF(tubeX - 3, tubeTop, _tubeWidth + 6, tubeHeight + outerRadius * 2),
            Color.FromArgb(200, 200, 210),
            Color.FromArgb(160, 160, 170),
            LinearGradientMode.Vertical);

        using var pen = new Pen(Color.FromArgb(100, 100, 110), 1);
        g.FillPath(gradientBrush, shellPath);
        g.DrawPath(pen, shellPath);

        // 管子内壁
        using var innerBrush = new SolidBrush(_tubeColor);
        var innerRect = new RectangleF(tubeX, tubeTop, _tubeWidth, tubeHeight);
        g.FillRectangle(innerBrush, innerRect);
        g.FillEllipse(innerBrush, centerX - tubeRadius, bulbY - tubeRadius, tubeRadius * 2, tubeRadius * 2);
    }

    /// <summary>
    /// 绘制水平外壳。
    /// </summary>
    private void DrawHorizontalShell(Graphics g, float centerY, float tubeLeft, float tubeY, float bulbX, float tubeWidth)
    {
        using var shellPath = new GraphicsPath();
        var tubeRadius = _tubeWidth / 2f;
        var outerRadius = _bulbRadius + 3;

        shellPath.AddRectangle(new RectangleF(tubeLeft, tubeY - 3, tubeWidth, _tubeWidth + 6));
        shellPath.AddArc(bulbX - outerRadius, centerY - outerRadius, outerRadius * 2, outerRadius * 2, 270, 180);

        using var gradientBrush = new LinearGradientBrush(
            new RectangleF(tubeLeft, tubeY - 3, tubeWidth + outerRadius * 2, _tubeWidth + 6),
            Color.FromArgb(200, 200, 210),
            Color.FromArgb(160, 160, 170),
            LinearGradientMode.Horizontal);

        g.FillPath(gradientBrush, shellPath);

        using var innerBrush = new SolidBrush(_tubeColor);
        g.FillRectangle(innerBrush, tubeLeft, tubeY, tubeWidth, _tubeWidth);
        g.FillEllipse(innerBrush, bulbX - outerRadius, centerY - tubeRadius, tubeRadius * 2, tubeRadius * 2);
    }

    /// <summary>
    /// 绘制温度区域背景。
    /// </summary>
    private void DrawTemperatureBackground(Graphics g, float tubeX, float tubeTop, float bulbY, float tubeHeight)
    {
        var totalRange = _maximum - _minimum;
        var tubeRadius = _tubeWidth / 2f;
        var innerRadius = tubeRadius - 4;

        // 绘制背景（灰色）
        using var bgBrush = new SolidBrush(Color.FromArgb(100, 180, 180, 180));
        var tubeLeft = tubeX + 2;
        var tubeRight = tubeX + _tubeWidth - 2;
        var centerX = tubeX + _tubeWidth / 2f;

        g.FillRectangle(bgBrush, tubeLeft, tubeTop, tubeRight - tubeLeft, bulbY - tubeTop);
        g.FillEllipse(bgBrush, centerX - innerRadius, bulbY - innerRadius, innerRadius * 2, innerRadius * 2);
    }

    /// <summary>
    /// 绘制水平温度背景。
    /// </summary>
    private void DrawHorizontalTemperatureBackground(Graphics g, float tubeLeft, float tubeY, float bulbX)
    {
        using var bgBrush = new SolidBrush(Color.FromArgb(100, 180, 180, 180));
        g.FillRectangle(bgBrush, tubeLeft, tubeY + 2, bulbX - tubeLeft - 4, _tubeWidth - 4);
    }

    /// <summary>
    /// 绘制温度液柱。
    /// </summary>
    private void DrawTemperatureLiquid(Graphics g, float tubeX, float tubeTop, float bulbY, float tubeHeight)
    {
        var ratio = (_value - _minimum) / (_maximum - _minimum);
        ratio = Math.Max(0, Math.Min(1, ratio));

        var liquidHeight = tubeTop + (float)(tubeHeight * (1 - ratio));
        var tubeRadius = _tubeWidth / 2f;
        var liquidRadius = tubeRadius - 4;
        var centerX = tubeX + _tubeWidth / 2f;

        var color = GetCurrentColor();

        // 液柱
        using var liquidBrush = new SolidBrush(color);
        g.FillRectangle(liquidBrush, tubeX + 2, liquidHeight, _tubeWidth - 4, bulbY - liquidHeight + 4);

        // 球泡
        using var bulbBrush = new SolidBrush(Color.FromArgb(230, color));
        g.FillEllipse(bulbBrush, centerX - liquidRadius - 1, bulbY - liquidRadius - 1, liquidRadius * 2 + 2, liquidRadius * 2 + 2);

        // 高光效果
        using var highlightBrush = new SolidBrush(Color.FromArgb(80, 255, 255, 255));
        g.FillEllipse(highlightBrush, centerX - liquidRadius * 0.6f, bulbY - liquidRadius * 0.6f, liquidRadius * 0.4f, liquidRadius * 0.4f);
    }

    /// <summary>
    /// 绘制水平液柱。
    /// </summary>
    private void DrawHorizontalTemperatureLiquid(Graphics g, float tubeLeft, float tubeY, float bulbX, float tubeWidth)
    {
        var ratio = (_value - _minimum) / (_maximum - _minimum);
        ratio = Math.Max(0, Math.Min(1, ratio));

        var liquidWidth = tubeLeft + (float)(tubeWidth * ratio);
        var tubeRadius = _tubeWidth / 2f;
        var centerY = tubeY + _tubeWidth / 2f;
        var liquidRadius = tubeRadius - 4;

        var color = GetCurrentColor();

        using var liquidBrush = new SolidBrush(color);
        g.FillRectangle(liquidBrush, tubeLeft + 2, tubeY + 2, liquidWidth - tubeLeft - 2, _tubeWidth - 4);
    }

    /// <summary>
    /// 绘制球泡。
    /// </summary>
    private void DrawBulb(Graphics g, float centerX, float bulbY)
    {
        var bulbRadius = _bulbRadius;
        var innerRadius = bulbRadius - 6;

        // 阴影
        using var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0));
        g.FillEllipse(shadowBrush, centerX - bulbRadius + 2, bulbY - bulbRadius + 2, bulbRadius * 2, bulbRadius * 2);

        // 高光
        using var highlightBrush = new SolidBrush(Color.FromArgb(50, 255, 255, 255));
        g.FillEllipse(highlightBrush, centerX - innerRadius * 0.7f, bulbY - innerRadius * 0.7f, innerRadius * 0.3f, innerRadius * 0.3f);
    }

    /// <summary>
    /// 绘制水平球泡。
    /// </summary>
    private void DrawHorizontalBulb(Graphics g, float bulbX, float centerY)
    {
        using var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0));
        g.FillEllipse(shadowBrush, bulbX - _bulbRadius + 2, centerY - _bulbRadius + 2, _bulbRadius * 2, _bulbRadius * 2);
    }

    /// <summary>
    /// 绘制刻度。
    /// </summary>
    private void DrawScale(Graphics g, float scaleX, float tubeTop, float tubeHeight)
    {
        using var font = new Font("Arial", 8f);
        using var pen = new Pen(Color.FromArgb(80, 80, 80), 1);
        using var brush = new SolidBrush(Color.FromArgb(80, 80, 80));

        var totalRange = _maximum - _minimum;

        for (var i = 0; i < _majorTickCount; i++)
        {
            var ratio = (double)i / (_majorTickCount - 1);
            var value = _minimum + ratio * totalRange;
            var y = tubeTop + (float)(tubeHeight * (1 - ratio));

            // 主刻度线
            g.DrawLine(pen, scaleX, y, scaleX + 8, y);

            // 标签
            var label = value.ToString("F0");
            g.DrawString(label, font, brush, scaleX + 10, y - 6);

            // 次刻度
            if (i < _majorTickCount - 1 && _minorTickCount > 0)
            {
                var nextRatio = (double)(i + 1) / (_majorTickCount - 1);
                var step = (nextRatio - ratio) / (_minorTickCount + 1);

                for (var j = 1; j <= _minorTickCount; j++)
                {
                    var minorRatio = ratio + j * step;
                    var minorY = tubeTop + (float)(tubeHeight * (1 - minorRatio));
                    g.DrawLine(pen, scaleX + 3, minorY, scaleX + 8, minorY);
                }
            }
        }
    }

    /// <summary>
    /// 绘制水平刻度。
    /// </summary>
    private void DrawHorizontalScale(Graphics g, float tubeLeft, float scaleY, float tubeWidth)
    {
        using var font = new Font("Arial", 8f);
        using var pen = new Pen(Color.FromArgb(80, 80, 80), 1);
        using var brush = new SolidBrush(Color.FromArgb(80, 80, 80));

        for (var i = 0; i < _majorTickCount; i++)
        {
            var ratio = (double)i / (_majorTickCount - 1);
            var x = tubeLeft + (float)(tubeWidth * ratio);

            g.DrawLine(pen, x, scaleY + _tubeWidth + 3, x, scaleY + _tubeWidth + 10);

            var label = (_minimum + ratio * (_maximum - _minimum)).ToString("F0");
            g.DrawString(label, font, brush, x - 8, scaleY + _tubeWidth + 12);
        }
    }

    /// <summary>
    /// 绘制数值。
    /// </summary>
    private void DrawValue(Graphics g, float centerX, float bulbY)
    {
        using var font = new Font("Arial", 10f, FontStyle.Bold);
        using var brush = new SolidBrush(GetCurrentColor());
        var text = $"{_value:F1}°";
        var textSize = g.MeasureString(text, font);
        g.DrawString(text, font, brush, centerX - textSize.Width / 2, bulbY - textSize.Height / 2);
    }

    #endregion

    #region 控件行为

    /// <inheritdoc />
    protected override Size DefaultSize => new Size(80, 200);

    #endregion
}
