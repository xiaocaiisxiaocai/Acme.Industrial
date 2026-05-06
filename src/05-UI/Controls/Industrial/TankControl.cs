using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace Acme.Industrial.UI.Controls.Industrial;

/// <summary>
/// 料仓控件。
/// 用于显示工业料仓/罐体的液位和状态。
/// </summary>
[DefaultProperty(nameof(Level))]
public class TankControl : Control
{
    #region 属性

    private double _level = 50;
    private double _minimum = 0;
    private double _maximum = 100;
    private Color _tankColor = Color.FromArgb(180, 180, 190);
    private Color _tankBorderColor = Color.FromArgb(120, 120, 130);
    private Color _liquidColor = Color.FromArgb(0, 150, 200);
    private bool _showLevelScale = true;
    private bool _showValue = true;
    private bool _showGrid = true;
    private bool _isHorizontal = false;
    private int _legHeight = 30;
    private bool _showWave = true;
    private float _waveAmplitude = 3;
    private int _segmentCount = 0;
    private string _tankName = "料仓";
    private bool _showTemperature = false;
    private double _temperature = 25;

    /// <summary>
    /// 当前液位（百分比）。
    /// </summary>
    [Category("数据")]
    [Description("当前液位百分比")]
    [DefaultValue(50.0)]
    public double Level
    {
        get => _level;
        set
        {
            _level = Math.Max(_minimum, Math.Min(_maximum, value));
            Invalidate();
            OnLevelChanged(EventArgs.Empty);
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
    /// 料仓颜色。
    /// </summary>
    [Category("外观")]
    public Color TankColor
    {
        get => _tankColor;
        set { _tankColor = value; Invalidate(); }
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
    /// 是否显示刻度。
    /// </summary>
    [Category("行为")]
    [DefaultValue(true)]
    public bool ShowLevelScale
    {
        get => _showLevelScale;
        set { _showLevelScale = value; Invalidate(); }
    }

    /// <summary>
    /// 是否显示数值。
    /// </summary>
    [Category("行为")]
    [DefaultValue(true)]
    public bool ShowValue
    {
        get => _showValue;
        set { _showValue = value; Invalidate(); }
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
    /// 支腿高度。
    /// </summary>
    [Category("外观")]
    [DefaultValue(30)]
    public int LegHeight
    {
        get => _legHeight;
        set { _legHeight = Math.Max(10, value); Invalidate(); }
    }

    /// <summary>
    /// 是否显示液面波浪。
    /// </summary>
    [Category("行为")]
    [DefaultValue(true)]
    public bool ShowWave
    {
        get => _showWave;
        set { _showWave = value; }
    }

    /// <summary>
    /// 波浪振幅。
    /// </summary>
    [Category("外观")]
    [DefaultValue(3)]
    public float WaveAmplitude
    {
        get => _waveAmplitude;
        set { _waveAmplitude = Math.Max(0, value); }
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

    /// <summary>
    /// 料仓名称。
    /// </summary>
    [Category("外观")]
    [DefaultValue("料仓")]
    public string TankName
    {
        get => _tankName;
        set { _tankName = value ?? ""; Invalidate(); }
    }

    /// <summary>
    /// 是否显示温度。
    /// </summary>
    [Category("行为")]
    [DefaultValue(false)]
    public bool ShowTemperature
    {
        get => _showTemperature;
        set { _showTemperature = value; Invalidate(); }
    }

    /// <summary>
    /// 当前温度。
    /// </summary>
    [Category("数据")]
    [DefaultValue(25.0)]
    public double Temperature
    {
        get => _temperature;
        set { _temperature = value; if (_showTemperature) Invalidate(); }
    }

    #endregion

    #region 事件

    /// <summary>
    /// 液位变更时触发。
    /// </summary>
    public event EventHandler? LevelChanged;

    /// <summary>
    /// 触发液位变更事件。
    /// </summary>
    protected virtual void OnLevelChanged(EventArgs e)
    {
        LevelChanged?.Invoke(this, e);
    }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化料仓控件。
    /// </summary>
    public TankControl()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        Size = new Size(150, 250);
    }

    #endregion

    #region 方法

    /// <summary>
    /// 获取当前液位颜色。
    /// </summary>
    public Color GetCurrentLiquidColor()
    {
        if (_level >= 90) return Color.FromArgb(220, 50, 50); // 高液位红色
        if (_level <= 10) return Color.FromArgb(255, 200, 0); // 低液位黄色
        return _liquidColor;
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

            if (_isHorizontal)
                PaintHorizontal(g);
            else
                PaintVertical(g);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"TankControl OnPaint error: {ex.Message}");
        }
    }

    /// <summary>
    /// 绘制垂直料仓。
    /// </summary>
    private void PaintVertical(Graphics g)
    {
        var width = ClientSize.Width - 80;
        var height = ClientSize.Height - _legHeight - 40;
        var x = 40;
        var y = 10;
        var bodyHeight = height - 30;

        // 绘制支腿
        DrawLegs(g, x, y + bodyHeight, width);

        // 绘制料仓主体
        DrawTankBody(g, x, y, width, bodyHeight);

        // 绘制液体
        DrawLiquid(g, x, y, width, bodyHeight);

        // 绘制网格
        if (_showGrid)
            DrawGrid(g, x, y, width, bodyHeight);

        // 绘制刻度
        if (_showLevelScale)
            DrawScale(g, x, y, bodyHeight);

        // 绘制标签
        DrawLabels(g, x, y, width, bodyHeight);
    }

    /// <summary>
    /// 绘制水平料仓。
    /// </summary>
    private void PaintHorizontal(Graphics g)
    {
        var height = ClientSize.Height - 60;
        var width = ClientSize.Width - 40;
        var x = 10;
        var y = 30;
        var bodyWidth = width - 60;

        // 绘制支腿
        DrawHorizontalLegs(g, x, y, bodyWidth, height);

        // 绘制料仓主体
        DrawHorizontalTankBody(g, x, y, bodyWidth, height);

        // 绘制液体
        DrawHorizontalLiquid(g, x, y, bodyWidth, height);

        // 绘制网格
        if (_showGrid)
            DrawHorizontalGrid(g, x, y, bodyWidth, height);

        // 绘制刻度
        if (_showLevelScale)
            DrawHorizontalScale(g, x, y, bodyWidth, height);
    }

    /// <summary>
    /// 绘制支腿。
    /// </summary>
    private void DrawLegs(Graphics g, float x, float y, float width)
    {
        using var legBrush = new SolidBrush(Color.FromArgb(100, 100, 110));
        using var legPen = new Pen(Color.FromArgb(70, 70, 80), 2);

        var legWidth = 8;
        var spacing = width / 4;

        for (var i = 0; i < 4; i++)
        {
            var legX = x + i * spacing - legWidth / 2;
            g.FillRectangle(legBrush, legX, y, legWidth, _legHeight);
            g.DrawRectangle(legPen, legX, y, legWidth, _legHeight);
        }
    }

    /// <summary>
    /// 绘制水平支腿。
    /// </summary>
    private void DrawHorizontalLegs(Graphics g, float x, float y, float width, float height)
    {
        using var legBrush = new SolidBrush(Color.FromArgb(100, 100, 110));
        using var legPen = new Pen(Color.FromArgb(70, 70, 80), 2);

        var legWidth = 8;
        var spacing = height / 4;

        for (var i = 0; i < 4; i++)
        {
            var legY = y + i * spacing - legWidth / 2;
            g.FillRectangle(legBrush, x, legY, _legHeight, legWidth);
            g.DrawRectangle(legPen, x, legY, _legHeight, legWidth);
        }
    }

    /// <summary>
    /// 绘制料仓主体。
    /// </summary>
    private void DrawTankBody(Graphics g, float x, float y, float width, float bodyHeight)
    {
        var cornerRadius = 15;

        // 主体路径
        using var bodyPath = CreateRoundedRectPath(x, y, width, bodyHeight, cornerRadius);

        // 渐变背景
        using var gradientBrush = new LinearGradientBrush(
            new RectangleF(x, y, width, bodyHeight),
            Color.FromArgb(200, 200, 210),
            Color.FromArgb(160, 160, 170),
            LinearGradientMode.Vertical);
        using var bodyBrush = new SolidBrush(_tankColor);
        g.FillPath(bodyBrush, bodyPath);

        // 边框
        using var borderPen = new Pen(_tankBorderColor, 3);
        g.DrawPath(borderPen, bodyPath);

        // 高光
        using var highlightBrush = new SolidBrush(Color.FromArgb(30, 255, 255, 255));
        var highlightPath = CreateRoundedRectPath(x + 5, y + 5, width * 0.15f, bodyHeight - 10, cornerRadius / 2);
        g.FillPath(highlightBrush, highlightPath);

        // 顶部封头
        DrawTankHead(g, x + width / 2, y, width, 30, true);

        // 底部锥形
        DrawTankCone(g, x, y + bodyHeight, width, 30, false);
    }

    /// <summary>
    /// 绘制水平料仓主体。
    /// </summary>
    private void DrawHorizontalTankBody(Graphics g, float x, float y, float width, float height)
    {
        using var bodyBrush = new SolidBrush(_tankColor);
        using var borderPen = new Pen(_tankBorderColor, 3);

        g.FillEllipse(bodyBrush, x, y, height, height);
        g.FillRectangle(bodyBrush, x + height / 2, y, width - height, height);
        g.FillEllipse(bodyBrush, x + width - height, y, height, height);

        g.DrawEllipse(borderPen, x, y, height, height);
        g.DrawLine(borderPen, x + height / 2, y, x + width - height / 2, y);
        g.DrawLine(borderPen, x + height / 2, y + height, x + width - height / 2, y + height);
        g.DrawEllipse(borderPen, x + width - height, y, height, height);
    }

    /// <summary>
    /// 绘制料仓封头/锥形底部。
    /// </summary>
    private void DrawTankHead(Graphics g, float cx, float y, float width, float height, bool isTop)
    {
        var direction = isTop ? -1 : 1;

        using var headBrush = new SolidBrush(Color.FromArgb(170, 170, 180));
        using var headPen = new Pen(Color.FromArgb(120, 120, 130), 2);

        var path = new GraphicsPath();
        path.AddEllipse(cx - width / 2, y, width, height * 2);

        var offsetY = isTop ? -height : height;
        g.FillEllipse(headBrush, cx - width / 2, y + offsetY, width, height);
        g.DrawEllipse(headPen, cx - width / 2, y + offsetY, width, height);
    }

    /// <summary>
    /// 绘制锥形底部。
    /// </summary>
    private void DrawTankCone(Graphics g, float x, float y, float width, float height, bool isTop)
    {
        var direction = isTop ? -1 : 1;

        using var coneBrush = new SolidBrush(Color.FromArgb(140, 140, 150));

        var path = new GraphicsPath();
        path.AddLine(x, y, x + width * 0.2f, y + height * direction);
        path.AddLine(x + width * 0.2f, y + height * direction, x + width * 0.8f, y + height * direction);
        path.AddLine(x + width * 0.8f, y + height * direction, x + width, y);
        path.CloseFigure();

        g.FillPath(coneBrush, path);
    }

    /// <summary>
    /// 绘制液体。
    /// </summary>
    private void DrawLiquid(Graphics g, float x, float y, float width, float bodyHeight)
    {
        var ratio = (_level - _minimum) / (_maximum - _minimum);
        ratio = Math.Max(0, Math.Min(1, ratio));

        var liquidHeight = (float)(bodyHeight * ratio);
        var liquidTop = y + bodyHeight - liquidHeight;
        var liquidColor = GetCurrentLiquidColor();

        // 液面波浪
        if (_showWave && liquidHeight > 0)
        {
            DrawWave(g, x, liquidTop, width, liquidHeight, liquidColor);
        }
        else if (liquidHeight > 0)
        {
            using var liquidBrush = new SolidBrush(liquidColor);
            g.FillRectangle(liquidBrush, x + 3, liquidTop, width - 6, liquidHeight);
        }
    }

    /// <summary>
    /// 绘制水平液体。
    /// </summary>
    private void DrawHorizontalLiquid(Graphics g, float x, float y, float width, float height)
    {
        var ratio = (_level - _minimum) / (_maximum - _minimum);
        ratio = Math.Max(0, Math.Min(1, ratio));

        var liquidWidth = (float)(width * ratio);
        var liquidColor = GetCurrentLiquidColor();

        using var liquidBrush = new SolidBrush(liquidColor);
        g.FillRectangle(liquidBrush, x, y + 3, liquidWidth, height - 6);
    }

    /// <summary>
    /// 绘制波浪效果。
    /// </summary>
    private void DrawWave(Graphics g, float x, float y, float width, float height, Color color)
    {
        var points = new List<PointF>();

        // 创建波浪线
        for (var i = 0; i <= width; i += 2)
        {
            var waveOffset = (float)Math.Sin((i + DateTime.Now.Millisecond / 5.0) * 0.1) * _waveAmplitude;
            points.Add(new PointF(x + i, y + waveOffset));
        }

        // 添加液面以下的部分
        points.Add(new PointF(x + width, y + height));
        points.Add(new PointF(x, y + height));

        // 创建波浪路径
        using var wavePath = new GraphicsPath();
        wavePath.AddPolygon(points.ToArray());

        // 液面渐变
        using var liquidBrush = new LinearGradientBrush(
            new RectangleF(x, y, width, height),
            Color.FromArgb(220, color),
            Color.FromArgb(180, color),
            LinearGradientMode.Vertical);

        g.SetClip(CreateRoundedRectPath(x + 3, y, width - 6, height, 10));
        g.FillPath(liquidBrush, wavePath);
        g.ResetClip();

        // 波浪线高光
        using var wavePen = new Pen(Color.FromArgb(100, 255, 255, 255), 2);
        g.DrawLines(wavePen, points.Take((int)(width / 2 + 1)).ToArray());
    }

    /// <summary>
    /// 绘制网格。
    /// </summary>
    private void DrawGrid(Graphics g, float x, float y, float width, float height)
    {
        using var gridPen = new Pen(Color.FromArgb(100, 180, 180, 180), 1);

        // 水平线
        for (var i = 1; i < 10; i++)
        {
            var lineY = y + (float)(height * i / 10);
            g.DrawLine(gridPen, x + 3, lineY, x + width - 3, lineY);
        }
    }

    /// <summary>
    /// 绘制水平网格。
    /// </summary>
    private void DrawHorizontalGrid(Graphics g, float x, float y, float width, float height)
    {
        using var gridPen = new Pen(Color.FromArgb(100, 180, 180, 180), 1);

        for (var i = 1; i < 10; i++)
        {
            var lineX = x + width * i / 10;
            g.DrawLine(gridPen, lineX, y + 3, lineX, y + height - 3);
        }
    }

    /// <summary>
    /// 绘制刻度。
    /// </summary>
    private void DrawScale(Graphics g, float x, float y, float height)
    {
        using var font = new Font("Arial", 8f);
        using var pen = new Pen(Color.FromArgb(80, 80, 80), 1);
        using var brush = new SolidBrush(Color.FromArgb(80, 80, 80));

        for (var i = 0; i <= 10; i++)
        {
            var labelY = y + height * (10 - i) / 10;
            var label = $"{i * 10}%";

            g.DrawLine(pen, x - 12, labelY, x - 5, labelY);
            g.DrawString(label, font, brush, x - 40, labelY - 5);
        }
    }

    /// <summary>
    /// 绘制水平刻度。
    /// </summary>
    private void DrawHorizontalScale(Graphics g, float x, float y, float width, float height)
    {
        using var font = new Font("Arial", 8f);
        using var pen = new Pen(Color.FromArgb(80, 80, 80), 1);
        using var brush = new SolidBrush(Color.FromArgb(80, 80, 80));

        for (var i = 0; i <= 10; i++)
        {
            var labelX = x + width * i / 10;
            var label = $"{i * 10}%";

            g.DrawLine(pen, labelX, y + height + 5, labelX, y + height + 12);
            g.DrawString(label, font, brush, labelX - 10, y + height + 14);
        }
    }

    /// <summary>
    /// 绘制标签。
    /// </summary>
    private void DrawLabels(Graphics g, float x, float y, float width, float bodyHeight)
    {
        // 料仓名称
        using var nameFont = new Font("微软雅黑", 10f, FontStyle.Bold);
        using var nameBrush = new SolidBrush(Color.FromArgb(60, 60, 60));
        var nameSize = g.MeasureString(_tankName, nameFont);
        g.DrawString(_tankName, nameFont, nameBrush,
            x + (width - nameSize.Width) / 2, y - nameSize.Height - 5);

        // 液位数值
        if (_showValue)
        {
            using var valueFont = new Font("Arial", 16f, FontStyle.Bold);
            using var valueBrush = new SolidBrush(Color.FromArgb(50, 50, 50));
            var valueText = $"{_level:F1}%";
            var valueSize = g.MeasureString(valueText, valueFont);
            g.DrawString(valueText, valueFont, valueBrush,
                x + (width - valueSize.Width) / 2, y + bodyHeight / 2 - valueSize.Height / 2);
        }

        // 温度
        if (_showTemperature)
        {
            using var tempFont = new Font("Arial", 9f);
            using var tempBrush = new SolidBrush(Color.FromArgb(150, 100, 50));
            var tempText = $"T: {_temperature:F1}°C";
            g.DrawString(tempText, tempFont, tempBrush, x + width - 60, y + 10);
        }
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

    protected override Size DefaultSize => new Size(150, 250);

    #endregion
}
