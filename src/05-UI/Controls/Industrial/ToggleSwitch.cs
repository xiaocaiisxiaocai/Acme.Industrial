using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Acme.Industrial.UI.Controls.Industrial;

/// <summary>
/// 开关状态。
/// </summary>
public enum ToggleState
{
    /// <summary>关闭。</summary>
    Off,

    /// <summary>打开。</summary>
    On
}

/// <summary>
/// 开关样式。
/// </summary>
public enum ToggleStyle
{
    /// <summary>标准样式。</summary>
    Standard,

    /// <summary>药丸样式。</summary>
    Pill,

    /// <summary>工业样式（带状态指示）。</summary>
    Industrial
}

/// <summary>
/// 工业开关控件。
/// 支持多种样式、状态颜色和动画效果。
/// </summary>
[DefaultProperty(nameof(IsOn))]
[DefaultBindingProperty(nameof(IsOn))]
public class ToggleSwitch : Control
{
    #region 属性

    private ToggleState _state = ToggleState.Off;
    private ToggleStyle _style = ToggleStyle.Pill;
    private Color _onColor = Color.FromArgb(0, 120, 215);
    private Color _offColor = Color.FromArgb(180, 180, 180);
    private Color _thumbColor = Color.White;
    private int _thumbSize = 20;
    private int _padding = 3;
    private bool _showStateText = true;
    private string _onText = "ON";
    private string _offText = "OFF";

    /// <summary>
    /// 获取或设置是否为打开状态。
    /// </summary>
    [Category("工业控件")]
    [Description("是否为打开状态")]
    [DefaultValue(false)]
    public bool IsOn
    {
        get => _state == ToggleState.On;
        set
        {
            var newState = value ? ToggleState.On : ToggleState.Off;
            if (_state != newState)
            {
                _state = newState;
                Invalidate();
                OnCheckedChanged(EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// 获取或设置开关状态。
    /// </summary>
    [Category("工业控件")]
    [Description("开关状态")]
    [DefaultValue(ToggleState.Off)]
    public ToggleState State
    {
        get => _state;
        set
        {
            if (_state != value)
            {
                _state = value;
                Invalidate();
                OnCheckedChanged(EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// 获取或设置开关样式。
    /// </summary>
    [Category("工业控件")]
    [Description("开关样式：Standard-标准，Pill-药丸，Industrial-工业")]
    [DefaultValue(ToggleStyle.Pill)]
    public ToggleStyle Style
    {
        get => _style;
        set
        {
            if (_style != value)
            {
                _style = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置打开状态颜色。
    /// </summary>
    [Category("外观")]
    [Description("打开状态的颜色")]
    public Color OnColor
    {
        get => _onColor;
        set
        {
            if (_onColor != value)
            {
                _onColor = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置关闭状态颜色。
    /// </summary>
    [Category("外观")]
    [Description("关闭状态的颜色")]
    public Color OffColor
    {
        get => _offColor;
        set
        {
            if (_offColor != value)
            {
                _offColor = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置滑块颜色。
    /// </summary>
    [Category("外观")]
    [Description("滑块颜色")]
    public Color ThumbColor
    {
        get => _thumbColor;
        set
        {
            if (_thumbColor != value)
            {
                _thumbColor = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置是否显示状态文本。
    /// </summary>
    [Category("外观")]
    [Description("是否显示ON/OFF文本")]
    [DefaultValue(true)]
    public bool ShowStateText
    {
        get => _showStateText;
        set
        {
            if (_showStateText != value)
            {
                _showStateText = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置打开状态文本。
    /// </summary>
    [Category("工业控件")]
    [Description("打开状态显示的文本")]
    [DefaultValue("ON")]
    public string OnText
    {
        get => _onText;
        set
        {
            if (_onText != value)
            {
                _onText = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置关闭状态文本。
    /// </summary>
    [Category("工业控件")]
    [Description("关闭状态显示的文本")]
    [DefaultValue("OFF")]
    public string OffText
    {
        get => _offText;
        set
        {
            if (_offText != value)
            {
                _offText = value;
                Invalidate();
            }
        }
    }

    #endregion

    #region 事件

    /// <summary>
    /// 状态变更时触发。
    /// </summary>
    public event EventHandler? CheckedChanged;

    /// <summary>
    /// 触发状态变更事件。
    /// </summary>
    protected virtual void OnCheckedChanged(EventArgs e)
    {
        CheckedChanged?.Invoke(this, e);
    }

    #endregion

    #region 字段

    private bool _isMouseDown = false;
    private RectangleF _thumbRect;
    private RectangleF _trackRect;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化开关控件。
    /// </summary>
    public ToggleSwitch()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        Size = new Size(50, 26);
        Cursor = Cursors.Hand;
    }

    #endregion

    #region 方法

    /// <summary>
    /// 切换状态。
    /// </summary>
    public void Toggle()
    {
        IsOn = !IsOn;
    }

    /// <summary>
    /// 绑定到布尔值。
    /// </summary>
    public void BindToBoolean(bool value, bool invert = false)
    {
        IsOn = value ^ invert;
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

            var trackHeight = ClientSize.Height - 2;
            var trackWidth = ClientSize.Width - 2;

            _trackRect = new RectangleF(1, 1, trackWidth, trackHeight);

            switch (_style)
            {
                case ToggleStyle.Standard:
                    PaintStandard(g);
                    break;
                case ToggleStyle.Pill:
                    PaintPill(g);
                    break;
                case ToggleStyle.Industrial:
                    PaintIndustrial(g);
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ToggleSwitch OnPaint error: {ex.Message}");
        }
    }

    /// <summary>
    /// 绘制标准样式。
    /// </summary>
    private void PaintStandard(Graphics g)
    {
        var currentColor = _state == ToggleState.On ? _onColor : _offColor;

        // 绘制轨道
        using var trackBrush = new SolidBrush(currentColor);
        g.FillRectangle(trackBrush, _trackRect);

        // 绘制边框
        using var borderPen = new Pen(Color.FromArgb(80, 80, 80), 1);
        g.DrawRectangle(borderPen, _trackRect.X, _trackRect.Y, _trackRect.Width - 1, _trackRect.Height - 1);

        // 计算滑块位置
        var thumbX = _state == ToggleState.On
            ? ClientSize.Width - _thumbSize - _padding - 1
            : _padding + 1;

        _thumbRect = new RectangleF(thumbX, _padding, _thumbSize, ClientSize.Height - _padding * 2 - 2);

        // 绘制滑块
        using var thumbBrush = new SolidBrush(_thumbColor);
        g.FillRectangle(thumbBrush, _thumbRect);

        // 绘制滑块阴影
        using var shadowPen = new Pen(Color.FromArgb(40, 0, 0, 0), 1);
        g.DrawRectangle(shadowPen, _thumbRect.X, _thumbRect.Y, _thumbRect.Width - 1, _thumbRect.Height - 1);

        // 绘制状态文本
        if (_showStateText)
        {
            using var font = new Font("Arial", 7F, FontStyle.Bold);
            using var textBrush = new SolidBrush(Color.FromArgb(180, 255, 255, 255));

            var text = _state == ToggleState.On ? _onText : _offText;
            var textSize = g.MeasureString(text, font);
            var textX = _state == ToggleState.On ? 5 : ClientSize.Width - textSize.Width - 5;
            g.DrawString(text, font, textBrush, textX, (ClientSize.Height - textSize.Height) / 2);
        }
    }

    /// <summary>
    /// 绘制药丸样式。
    /// </summary>
    private void PaintPill(Graphics g)
    {
        var currentColor = _state == ToggleState.On ? _onColor : _offColor;

        // 绘制圆角轨道
        var trackPath = CreateRoundedRectanglePath(_trackRect, (int)(_trackRect.Height / 2));
        using var trackBrush = new SolidBrush(currentColor);
        g.FillPath(trackBrush, trackPath);

        // 计算滑块
        var thumbX = _state == ToggleState.On
            ? ClientSize.Width - _thumbSize - _padding * 2
            : _padding;

        _thumbRect = new RectangleF(thumbX, _padding, _thumbSize, ClientSize.Height - _padding * 2);

        // 绘制滑块阴影
        using var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0));
        var shadowRect = new RectangleF(_thumbRect.X + 1, _thumbRect.Y + 2, _thumbRect.Width, _thumbRect.Height);
        g.FillEllipse(shadowBrush, shadowRect);

        // 绘制滑块
        using var thumbBrush = new SolidBrush(_thumbColor);
        g.FillEllipse(thumbBrush, _thumbRect);

        // 绘制滑块高光
        using var highlightBrush = new SolidBrush(Color.FromArgb(60, 255, 255, 255));
        var highlightRect = new RectangleF(
            _thumbRect.X + 2,
            _thumbRect.Y + 2,
            _thumbRect.Width / 2,
            _thumbRect.Height / 3);
        g.FillEllipse(highlightBrush, highlightRect);

        // 绘制状态文本
        if (_showStateText)
        {
            using var font = new Font("Arial", 7F, FontStyle.Bold);
            using var textBrush = new SolidBrush(Color.FromArgb(180, 255, 255, 255));

            var text = _state == ToggleState.On ? _onText : _offText;
            var textX = _state == ToggleState.On ? 5 : ClientSize.Width - 30;
            g.DrawString(text, font, textBrush, textX, (ClientSize.Height - 10) / 2);
        }
    }

    /// <summary>
    /// 绘制工业样式。
    /// </summary>
    private void PaintIndustrial(Graphics g)
    {
        var currentColor = _state == ToggleState.On ? _onColor : _offColor;

        // 绘制外边框
        using var outerPen = new Pen(Color.FromArgb(60, 60, 60), 2);
        g.DrawRectangle(outerPen, 1, 1, ClientSize.Width - 2, ClientSize.Height - 2);

        // 绘制轨道
        var innerRect = new RectangleF(4, 4, ClientSize.Width - 8, ClientSize.Height - 8);
        using var trackBrush = new SolidBrush(Color.FromArgb(40, 40, 40));
        g.FillRectangle(trackBrush, innerRect);

        // 绘制状态指示灯
        var indicatorSize = 6;
        var indicatorX = _state == ToggleState.On ? ClientSize.Width - 12 : 6;
        using var indicatorBrush = new SolidBrush(currentColor);
        g.FillEllipse(indicatorBrush, indicatorX, (ClientSize.Height - indicatorSize) / 2, indicatorSize, indicatorSize);

        // 计算滑块位置
        var thumbX = _state == ToggleState.On
            ? ClientSize.Width - _thumbSize - 10
            : 6;

        _thumbRect = new RectangleF(thumbX, 3, _thumbSize, ClientSize.Height - 6);

        // 绘制滑块（带3D效果）
        var thumbPath = CreateRoundedRectanglePath(_thumbRect, 3);
        using var thumbBrush = new SolidBrush(_thumbColor);
        g.FillPath(thumbBrush, thumbPath);

        // 滑块顶部高光
        using var topLightPen = new Pen(Color.FromArgb(80, 255, 255, 255), 1);
        g.DrawPath(topLightPen, thumbPath);

        // 滑块阴影
        using var shadowPen = new Pen(Color.FromArgb(40, 0, 0, 0), 1);
        var shadowPath = CreateRoundedRectanglePath(
            new RectangleF(_thumbRect.X + 1, _thumbRect.Y + 1, _thumbRect.Width - 2, _thumbRect.Height - 2), 2);
        g.DrawPath(shadowPen, shadowPath);

        // 绘制状态文本
        if (_showStateText)
        {
            using var font = new Font("Arial", 6F, FontStyle.Bold);
            var textColor = _state == ToggleState.On ? _onColor : _offColor;
            using var textBrush = new SolidBrush(textColor);

            var text = _state == ToggleState.On ? _onText : _offText;
            var textSize = g.MeasureString(text, font);
            var textX = _state == ToggleState.On ? 8 : ClientSize.Width - textSize.Width - 8;
            g.DrawString(text, font, textBrush, textX, 2);
        }
    }

    /// <summary>
    /// 创建圆角矩形路径。
    /// </summary>
    private static GraphicsPath CreateRoundedRectanglePath(RectangleF rect, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;

        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }

    #endregion

    #region 控件行为

    /// <inheritdoc />
    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        base.OnMouseDown(mevent);
        _isMouseDown = true;
    }

    /// <inheritdoc />
    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        base.OnMouseUp(mevent);
        _isMouseDown = false;
        Toggle();
    }

    /// <inheritdoc />
    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        Toggle();
    }

    /// <inheritdoc />
    protected override Size DefaultSize => new Size(50, 26);

    #endregion
}
