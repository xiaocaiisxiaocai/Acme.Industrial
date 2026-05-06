using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Acme.Industrial.UI.Controls.Industrial;

/// <summary>
/// 仪表指针类型。
/// </summary>
public enum GaugeNeedleStyle
{
    /// <summary>指针式。</summary>
    Needle,

    /// <summary>扇形。</summary>
    Arc,

    /// <summary>填充。</summary>
    Fill
}

/// <summary>
/// 刻度类型。
/// </summary>
public enum GaugeScaleStyle
{
    /// <summary>线性。</summary>
    Linear,

    /// <summary>对数。</summary>
    Logarithmic,

    /// <summary>分段。</summary>
    Segmented
}

/// <summary>
/// 模拟仪表控件。
/// 支持圆形仪表盘，可配置多个区域和刻度。
/// </summary>
[DefaultProperty(nameof(Value))]
public class AnalogGauge : Control
{
    #region 属性

    private double _value = 0;
    private double _minimum = 0;
    private double _maximum = 100;
    private double _warningValue = 70;
    private double _dangerValue = 90;
    private int _majorTickCount = 11;
    private int _minorTickCount = 5;
    private int _startAngle = 135;
    private int _sweepAngle = 270;
    private Color _normalColor = Color.FromArgb(50, 180, 50);
    private Color _warningColor = Color.FromArgb(255, 200, 0);
    private Color _dangerColor = Color.FromArgb(220, 50, 50);
    private Color _needleColor = Color.FromArgb(40, 40, 40);
    private Color _dialColor = Color.FromArgb(250, 250, 250);
    private Color _scaleColor = Color.FromArgb(60, 60, 60);
    private Color _backgroundColor = Color.FromArgb(30, 30, 30);
    private bool _showNeedle = true;
    private bool _showDigitalValue = true;
    private bool _showMinMax = true;
    private bool _showZones = true;
    private bool _showGridLines = true;
    private GaugeNeedleStyle _needleStyle = GaugeNeedleStyle.Needle;
    private string _unit = "%";
    private int _decimalPlaces = 0;

    /// <summary>
    /// 当前值。
    /// </summary>
    [Category("数据")]
    [Description("当前仪表值")]
    [DefaultValue(0.0)]
    public double Value
    {
        get => _value;
        set
        {
            var clampedValue = Math.Max(_minimum, Math.Min(_maximum, value));
            if (Math.Abs(_value - clampedValue) > 0.001)
            {
                _value = clampedValue;
                Invalidate();
                OnValueChanged(EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// 最小值。
    /// </summary>
    [Category("数据")]
    [Description("仪表最小值")]
    [DefaultValue(0.0)]
    public double Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            if (_value < _minimum) _value = _minimum;
            Invalidate();
        }
    }

    /// <summary>
    /// 最大值。
    /// </summary>
    [Category("数据")]
    [Description("仪表最大值")]
    [DefaultValue(100.0)]
    public double Maximum
    {
        get => _maximum;
        set
        {
            _maximum = value;
            if (_value > _maximum) _value = _maximum;
            Invalidate();
        }
    }

    /// <summary>
    /// 警告阈值。
    /// </summary>
    [Category("外观")]
    [Description("警告区域起始值")]
    [DefaultValue(70.0)]
    public double WarningValue
    {
        get => _warningValue;
        set
        {
            _warningValue = value;
            Invalidate();
        }
    }

    /// <summary>
    /// 危险阈值。
    /// </summary>
    [Category("外观")]
    [Description("危险区域起始值")]
    [DefaultValue(90.0)]
    public double DangerValue
    {
        get => _dangerValue;
        set
        {
            _dangerValue = value;
            Invalidate();
        }
    }

    /// <summary>
    /// 主刻度数量。
    /// </summary>
    [Category("外观")]
    [Description("主刻度数量")]
    [DefaultValue(11)]
    public int MajorTickCount
    {
        get => _majorTickCount;
        set
        {
            _majorTickCount = Math.Max(2, value);
            Invalidate();
        }
    }

    /// <summary>
    /// 次刻度数量。
    /// </summary>
    [Category("外观")]
    [Description("每个主刻度间的次刻度数")]
    [DefaultValue(5)]
    public int MinorTickCount
    {
        get => _minorTickCount;
        set
        {
            _minorTickCount = Math.Max(0, value);
            Invalidate();
        }
    }

    /// <summary>
    /// 起始角度。
    /// </summary>
    [Category("外观")]
    [Description("起始角度（度）")]
    [DefaultValue(135)]
    public int StartAngle
    {
        get => _startAngle;
        set
        {
            _startAngle = value;
            Invalidate();
        }
    }

    /// <summary>
    /// 扫描角度。
    /// </summary>
    [Category("外观")]
    [Description("扫描角度（度）")]
    [DefaultValue(270)]
    public int SweepAngle
    {
        get => _sweepAngle;
        set
        {
            _sweepAngle = Math.Max(1, Math.Min(360, value));
            Invalidate();
        }
    }

    /// <summary>
    /// 正常颜色。
    /// </summary>
    [Category("外观")]
    [Description("正常区域颜色")]
    public Color NormalColor
    {
        get => _normalColor;
        set { _normalColor = value; Invalidate(); }
    }

    /// <summary>
    /// 警告颜色。
    /// </summary>
    [Category("外观")]
    [Description("警告区域颜色")]
    public Color WarningColor
    {
        get => _warningColor;
        set { _warningColor = value; Invalidate(); }
    }

    /// <summary>
    /// 危险颜色。
    /// </summary>
    [Category("外观")]
    [Description("危险区域颜色")]
    public Color DangerColor
    {
        get => _dangerColor;
        set { _dangerColor = value; Invalidate(); }
    }

    /// <summary>
    /// 指针颜色。
    /// </summary>
    [Category("外观")]
    [Description("指针颜色")]
    public Color NeedleColor
    {
        get => _needleColor;
        set { _needleColor = value; Invalidate(); }
    }

    /// <summary>
    /// 表盘颜色。
    /// </summary>
    [Category("外观")]
    [Description("表盘背景颜色")]
    public Color DialColor
    {
        get => _dialColor;
        set { _dialColor = value; Invalidate(); }
    }

    /// <summary>
    /// 刻度颜色。
    /// </summary>
    [Category("外观")]
    [Description("刻度和数字颜色")]
    public Color ScaleColor
    {
        get => _scaleColor;
        set { _scaleColor = value; Invalidate(); }
    }

    /// <summary>
    /// 是否显示指针。
    /// </summary>
    [Category("行为")]
    [Description("是否显示指针")]
    [DefaultValue(true)]
    public bool ShowNeedle
    {
        get => _showNeedle;
        set { _showNeedle = value; Invalidate(); }
    }

    /// <summary>
    /// 是否显示数值。
    /// </summary>
    [Category("行为")]
    [Description("是否在中心显示数值")]
    [DefaultValue(true)]
    public bool ShowDigitalValue
    {
        get => _showDigitalValue;
        set { _showDigitalValue = value; Invalidate(); }
    }

    /// <summary>
    /// 是否显示最小最大值。
    /// </summary>
    [Category("行为")]
    [Description("是否显示最小最大值标签")]
    [DefaultValue(true)]
    public bool ShowMinMax
    {
        get => _showMinMax;
        set { _showMinMax = value; Invalidate(); }
    }

    /// <summary>
    /// 是否显示区域。
    /// </summary>
    [Category("行为")]
    [Description("是否显示红黄绿区域")]
    [DefaultValue(true)]
    public bool ShowZones
    {
        get => _showZones;
        set { _showZones = value; Invalidate(); }
    }

    /// <summary>
    /// 是否显示网格线。
    /// </summary>
    [Category("行为")]
    [Description("是否显示背景网格线")]
    [DefaultValue(true)]
    public bool ShowGridLines
    {
        get => _showGridLines;
        set { _showGridLines = value; Invalidate(); }
    }

    /// <summary>
    /// 指针样式。
    /// </summary>
    [Category("外观")]
    [Description("指针样式")]
    [DefaultValue(GaugeNeedleStyle.Needle)]
    public GaugeNeedleStyle NeedleStyle
    {
        get => _needleStyle;
        set { _needleStyle = value; Invalidate(); }
    }

    /// <summary>
    /// 单位文本。
    /// </summary>
    [Category("外观")]
    [Description("显示的单位，如 %, RPM, PSI")]
    [DefaultValue("%")]
    public string Unit
    {
        get => _unit;
        set { _unit = value ?? ""; Invalidate(); }
    }

    /// <summary>
    /// 小数位数。
    /// </summary>
    [Category("外观")]
    [Description("数值显示的小数位数")]
    [DefaultValue(0)]
    public int DecimalPlaces
    {
        get => _decimalPlaces;
        set { _decimalPlaces = Math.Max(0, value); Invalidate(); }
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
    /// 初始化模拟仪表。
    /// </summary>
    public AnalogGauge()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        Size = new Size(200, 200);
    }

    #endregion

    #region 方法

    /// <summary>
    /// 获取当前颜色（基于值）。
    /// </summary>
    public Color GetCurrentColor()
    {
        var ratio = (_value - _minimum) / (_maximum - _minimum);
        if (ratio >= (_dangerValue - _minimum) / (_maximum - _minimum))
            return _dangerColor;
        if (ratio >= (_warningValue - _minimum) / (_maximum - _minimum))
            return _warningColor;
        return _normalColor;
    }

    /// <summary>
    /// 绑定到数值。
    /// </summary>
    public void BindToValue(double value)
    {
        Value = value;
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

            var size = Math.Min(ClientSize.Width, ClientSize.Height);
            var centerX = ClientSize.Width / 2f;
            var centerY = ClientSize.Height / 2f;
            var radius = size / 2f - 15;
            var innerRadius = radius * 0.75f;

            // 绘制外圈
            DrawOuterRing(g, centerX, centerY, radius);

            // 绘制区域
            if (_showZones)
            {
                DrawZones(g, centerX, centerY, radius - 5, innerRadius);
            }

            // 绘制刻度
            DrawScale(g, centerX, centerY, radius, innerRadius);

            // 绘制指针
            if (_showNeedle)
            {
                DrawNeedle(g, centerX, centerY, innerRadius - 20);
            }

            // 绘制中心
            DrawCenter(g, centerX, centerY, innerRadius * 0.25f);

            // 绘制数值
            if (_showDigitalValue)
            {
                DrawDigitalValue(g, centerX, centerY + innerRadius * 0.3f);
            }

            // 绘制最小最大值
            if (_showMinMax)
            {
                DrawMinMax(g, centerX, centerY, radius);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AnalogGauge OnPaint error: {ex.Message}");
        }
    }

    /// <summary>
    /// 绘制外圈。
    /// </summary>
    private void DrawOuterRing(Graphics g, float cx, float cy, float radius)
    {
        // 外圈阴影
        using var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0));
        g.FillEllipse(shadowBrush, cx - radius + 3, cy - radius + 3, radius * 2, radius * 2);

        // 外圈渐变
        using var gradientBrush = new LinearGradientBrush(
            new RectangleF(cx - radius, cy - radius, radius * 2, radius * 2),
            Color.FromArgb(80, 80, 80),
            Color.FromArgb(40, 40, 40),
            LinearGradientMode.ForwardDiagonal);
        g.FillEllipse(gradientBrush, cx - radius, cy - radius, radius * 2, radius * 2);

        // 表盘背景
        using var dialBrush = new SolidBrush(_dialColor);
        g.FillEllipse(dialBrush, cx - radius + 5, cy - radius + 5, (radius - 5) * 2, (radius - 5) * 2);
    }

    /// <summary>
    /// 绘制区域（红黄绿）。
    /// </summary>
    private void DrawZones(Graphics g, float cx, float cy, float outerR, float innerR)
    {
        var startRad = _startAngle * Math.PI / 180;
        var sweepRad = _sweepAngle * Math.PI / 180;
        var totalRange = _maximum - _minimum;

        // 绿色区域 (正常)
        var greenStart = 0.0;
        var greenEnd = (_warningValue - _minimum) / totalRange;
        DrawArcZone(g, cx, cy, outerR, innerR, startRad + greenStart * sweepRad, (greenEnd - greenStart) * sweepRad, _normalColor);

        // 黄色区域 (警告)
        var yellowStart = greenEnd;
        var yellowEnd = (_dangerValue - _minimum) / totalRange;
        DrawArcZone(g, cx, cy, outerR, innerR, startRad + yellowStart * sweepRad, (yellowEnd - yellowStart) * sweepRad, _warningColor);

        // 红色区域 (危险)
        var redStart = yellowEnd;
        var redEnd = 1.0;
        DrawArcZone(g, cx, cy, outerR, innerR, startRad + redStart * sweepRad, (redEnd - redStart) * sweepRad, _dangerColor);
    }

    /// <summary>
    /// 绘制圆弧区域。
    /// </summary>
    private void DrawArcZone(Graphics g, float cx, float cy, float outerR, float innerR, double startRad, double sweepRad, Color color)
    {
        if (sweepRad <= 0) return;

        using var brush = new SolidBrush(Color.FromArgb(60, color));
        var path = new GraphicsPath();

        var points = 30;
        var step = sweepRad / points;

        // 外弧
        for (var i = 0; i <= points; i++)
        {
            var angle = startRad + i * step;
            var px = cx + outerR * (float)Math.Cos(angle);
            var py = cy + outerR * (float)Math.Sin(angle);
            if (i == 0) path.AddLine(px, py, px + 0.1f, py);
            else path.AddLine(px, py, px + 0.1f, py);
        }

        // 内弧（反向）
        for (var i = points; i >= 0; i--)
        {
            var angle = startRad + i * step;
            var px = cx + innerR * (float)Math.Cos(angle);
            var py = cy + innerR * (float)Math.Sin(angle);
            path.AddLine(px, py, px + 0.1f, py);
        }

        path.CloseFigure();
        g.FillPath(brush, path);
    }

    /// <summary>
    /// 绘制刻度。
    /// </summary>
    private void DrawScale(Graphics g, float cx, float cy, float outerR, float innerR)
    {
        using var pen = new Pen(_scaleColor, 1.5f);
        using var font = new Font("Arial", Math.Max(7, outerR * 0.08f), FontStyle.Regular);
        using var boldFont = new Font("Arial", Math.Max(8, outerR * 0.09f), FontStyle.Bold);

        var startRad = _startAngle * Math.PI / 180;
        var sweepRad = _sweepAngle * Math.PI / 180;
        var totalRange = _maximum - _minimum;

        // 主刻度
        for (var i = 0; i < _majorTickCount; i++)
        {
            var ratio = (double)i / (_majorTickCount - 1);
            var angle = startRad + ratio * sweepRad;
            var value = _minimum + ratio * totalRange;

            var cos = (float)Math.Cos(angle);
            var sin = (float)Math.Sin(angle);

            var outerX = cx + outerR * cos;
            var outerY = cy + outerR * sin;
            var innerX = cx + (outerR - 15) * cos;
            var innerY = cy + (outerR - 15) * sin;

            pen.Width = 2f;
            g.DrawLine(pen, outerX, outerY, innerX, innerY);

            // 数字
            var textRadius = outerR - 28;
            var textX = cx + textRadius * cos;
            var textY = cy + textRadius * sin;

            var text = value.ToString($"F{_decimalPlaces}");
            var textSize = g.MeasureString(text, i == 0 || i == _majorTickCount - 1 ? boldFont : font);
            g.DrawString(text, i == 0 || i == _majorTickCount - 1 ? boldFont : font,
                new SolidBrush(_scaleColor),
                textX - textSize.Width / 2,
                textY - textSize.Height / 2);

            // 次刻度
            if (i < _majorTickCount - 1 && _minorTickCount > 0)
            {
                var nextRatio = (double)(i + 1) / (_majorTickCount - 1);
                var minorStep = (nextRatio - ratio) / (_minorTickCount + 1);

                for (var j = 1; j <= _minorTickCount; j++)
                {
                    var minorRatio = ratio + j * minorStep;
                    var minorAngle = startRad + minorRatio * sweepRad;
                    var minorCos = (float)Math.Cos(minorAngle);
                    var minorSin = (float)Math.Sin(minorAngle);

                    var minorOuterX = cx + outerR * minorCos;
                    var minorOuterY = cy + outerR * minorSin;
                    var minorInnerX = cx + (outerR - 8) * minorCos;
                    var minorInnerY = cy + (outerR - 8) * minorSin;

                    pen.Width = 1f;
                    g.DrawLine(pen, minorOuterX, minorOuterY, minorInnerX, minorInnerY);
                }
            }
        }
    }

    /// <summary>
    /// 绘制指针。
    /// </summary>
    private void DrawNeedle(Graphics g, float cx, float cy, float length)
    {
        var ratio = (_value - _minimum) / (_maximum - _minimum);
        var angle = (_startAngle + ratio * _sweepAngle) * Math.PI / 180;
        var cos = (float)Math.Cos(angle);
        var sin = (float)Math.Sin(angle);

        var needleColor = GetCurrentColor();

        switch (_needleStyle)
        {
            case GaugeNeedleStyle.Needle:
                DrawNeedleStyle(g, cx, cy, length, cos, sin, needleColor);
                break;
            case GaugeNeedleStyle.Arc:
                DrawArcStyle(g, cx, cy, length, ratio, needleColor);
                break;
            case GaugeNeedleStyle.Fill:
                DrawFillStyle(g, cx, cy, length, ratio, needleColor);
                break;
        }
    }

    /// <summary>
    /// 绘制传统指针。
    /// </summary>
    private void DrawNeedleStyle(Graphics g, float cx, float cy, float length, float cos, float sin, Color color)
    {
        var needleWidth = 8f;
        var baseWidth = 12f;

        // 指针三角形
        var path = new GraphicsPath();
        var tipX = cx + length * cos;
        var tipY = cy + length * sin;
        var perpX = -sin;
        var perpY = cos;

        path.AddLine(tipX, tipY,
            cx + baseWidth / 2 * perpX,
            cy + baseWidth / 2 * perpY);
        path.AddLine(cx + baseWidth / 2 * perpX,
            cy + baseWidth / 2 * perpY,
            cx - baseWidth / 2 * perpX,
            cy - baseWidth / 2 * perpY);
        path.AddLine(cx - baseWidth / 2 * perpX,
            cy - baseWidth / 2 * perpY,
            tipX, tipY);
        path.CloseFigure();

        // 指针阴影
        using (var shadowBrush = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
        {
            var shadowPath = new GraphicsPath();
            shadowPath.AddLine(tipX + 2, tipY + 2, cx + baseWidth / 2 * perpX + 2, cy + baseWidth / 2 * perpY + 2);
            shadowPath.AddLine(cx + baseWidth / 2 * perpX + 2, cy + baseWidth / 2 * perpY + 2, cx - baseWidth / 2 * perpX + 2, cy - baseWidth / 2 * perpY + 2);
            shadowPath.AddLine(cx - baseWidth / 2 * perpX + 2, cy - baseWidth / 2 * perpY + 2, tipX + 2, tipY + 2);
            shadowPath.CloseFigure();
            g.FillPath(shadowBrush, shadowPath);
        }

        using var brush = new SolidBrush(color);
        g.FillPath(brush, path);
    }

    /// <summary>
    /// 绘制扇形指针。
    /// </summary>
    private void DrawArcStyle(Graphics g, float cx, float cy, float length, double ratio, Color color)
    {
        var startRad = _startAngle * Math.PI / 180;
        var currentRad = startRad + ratio * _sweepAngle * Math.PI / 180;

        using var brush = new SolidBrush(Color.FromArgb(180, color));
        var path = new GraphicsPath();
        path.AddArc(cx - length, cy - length, length * 2, length * 2,
            (float)(_startAngle), (float)(ratio * _sweepAngle));
        path.AddArc(cx - length * 0.7f, cy - length * 0.7f, length * 1.4f, length * 1.4f,
            (float)(_startAngle + ratio * _sweepAngle), (float)(-ratio * _sweepAngle));
        path.CloseFigure();

        g.FillPath(brush, path);
    }

    /// <summary>
    /// 绘制填充指针。
    /// </summary>
    private void DrawFillStyle(Graphics g, float cx, float cy, float length, double ratio, Color color)
    {
        var startRad = _startAngle * Math.PI / 180;
        var currentRad = startRad + ratio * _sweepAngle * Math.PI / 180;

        using var brush = new SolidBrush(Color.FromArgb(100, color));
        var path = new GraphicsPath();
        path.AddLine(cx, cy, cx + length * (float)Math.Cos(startRad), cy + length * (float)Math.Sin(startRad));
        path.AddArc(cx - length, cy - length, length * 2, length * 2,
            (float)(_startAngle), (float)(ratio * _sweepAngle));
        path.AddLine(cx, cy,
            cx + length * (float)Math.Cos(currentRad),
            cy + length * (float)Math.Sin(currentRad));
        path.CloseFigure();

        g.FillPath(brush, path);
    }

    /// <summary>
    /// 绘制中心。
    /// </summary>
    private void DrawCenter(Graphics g, float cx, float cy, float radius)
    {
        // 中心阴影
        using var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0));
        g.FillEllipse(shadowBrush, cx - radius + 2, cy - radius + 2, radius * 2, radius * 2);

        // 中心圆
        using var gradientBrush = new LinearGradientBrush(
            new RectangleF(cx - radius, cy - radius, radius * 2, radius * 2),
            Color.FromArgb(60, 60, 60),
            Color.FromArgb(30, 30, 30),
            LinearGradientMode.BackwardDiagonal);
        g.FillEllipse(gradientBrush, cx - radius, cy - radius, radius * 2, radius * 2);

        // 中心点
        using var centerBrush = new SolidBrush(Color.FromArgb(50, 50, 50));
        g.FillEllipse(centerBrush, cx - radius * 0.5f, cy - radius * 0.5f, radius, radius);
    }

    /// <summary>
    /// 绘制数值。
    /// </summary>
    private void DrawDigitalValue(Graphics g, float cx, float cy)
    {
        var text = $"{_value.ToString($"F{_decimalPlaces}")} {_unit}";
        using var font = new Font("Arial", Math.Max(10, ClientSize.Width * 0.1f), FontStyle.Bold);
        using var brush = new SolidBrush(GetCurrentColor());
        var textSize = g.MeasureString(text, font);
        g.DrawString(text, font, brush, cx - textSize.Width / 2, cy - textSize.Height / 2);
    }

    /// <summary>
    /// 绘制最小最大值。
    /// </summary>
    private void DrawMinMax(Graphics g, float cx, float cy, float radius)
    {
        using var font = new Font("Arial", Math.Max(6, radius * 0.06f));
        using var brush = new SolidBrush(Color.FromArgb(120, 120, 120));

        var text = _minimum.ToString("F0");
        var textSize = g.MeasureString(text, font);
        var minAngle = _startAngle * Math.PI / 180;
        var minRadius = radius - 45;
        g.DrawString(text, font, brush,
            cx + minRadius * (float)Math.Cos(minAngle) - textSize.Width / 2,
            cy + minRadius * (float)Math.Sin(minAngle) - textSize.Height / 2);

        text = _maximum.ToString("F0");
        textSize = g.MeasureString(text, font);
        var maxAngle = (_startAngle + _sweepAngle) * Math.PI / 180;
        var maxRadius = radius - 45;
        g.DrawString(text, font, brush,
            cx + maxRadius * (float)Math.Cos(maxAngle) - textSize.Width / 2,
            cy + maxRadius * (float)Math.Sin(maxAngle) - textSize.Height / 2);
    }

    #endregion

    #region 控件行为

    /// <inheritdoc />
    protected override Size DefaultSize => new Size(200, 200);

    #endregion
}
