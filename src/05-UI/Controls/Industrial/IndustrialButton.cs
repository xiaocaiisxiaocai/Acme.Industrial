using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Acme.Industrial.UI.Controls.Industrial;

/// <summary>
/// 工业按钮状态。
/// </summary>
public enum IndustrialButtonState
{
    /// <summary>正常。</summary>
    Normal,

    /// <summary>按下。</summary>
    Pressed,

    /// <summary>禁用。</summary>
    Disabled,

    /// <summary>选中（切换模式）。</summary>
    Checked
}

/// <summary>
/// 工业按钮样式。
/// </summary>
public enum IndustrialButtonStyle
{
    /// <summary>标准样式。</summary>
    Standard,

    /// <summary>凸起样式（3D效果）。</summary>
    Raised,

    /// <summary>扁平样式。</summary>
    Flat,

    /// <summary>圆形按钮。</summary>
    Circular,

    /// <summary>急停按钮样式。</summary>
    Emergency
}

/// <summary>
/// 工业按钮控件。
/// 支持多种样式、状态颜色和快捷键提示。
/// </summary>
[DefaultProperty(nameof(Text))]
[DefaultEvent(nameof(Click))]
public class IndustrialButton : Control
{
    #region 属性

    private IndustrialButtonState _buttonState = IndustrialButtonState.Normal;
    private IndustrialButtonStyle _buttonStyle = IndustrialButtonStyle.Raised;
    private bool _isToggle = false;
    private bool _isChecked = false;
    private Color _normalColor = Color.FromArgb(66, 66, 66);
    private Color _hoverColor = Color.FromArgb(80, 80, 80);
    private Color _pressedColor = Color.FromArgb(40, 40, 40);
    private Color _disabledColor = Color.FromArgb(120, 120, 120);
    private Color _checkedColor = Color.FromArgb(0, 120, 215);
    private string _shortcutKey = string.Empty;
    private bool _showShortcut = true;
    private string _statusText = string.Empty;

    /// <summary>
    /// 获取或设置按钮状态。
    /// </summary>
    [Category("工业控件")]
    [Description("按钮当前状态")]
    [DefaultValue(IndustrialButtonState.Normal)]
    public IndustrialButtonState ButtonState
    {
        get => _buttonState;
        set
        {
            if (_buttonState != value)
            {
                _buttonState = value;
                Invalidate();
                OnButtonStateChanged(EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// 获取或设置按钮样式。
    /// </summary>
    [Category("工业控件")]
    [Description("按钮样式：Standard-标准，Raised-凸起，Flat-扁平，Circular-圆形，Emergency-急停")]
    [DefaultValue(IndustrialButtonStyle.Raised)]
    public IndustrialButtonStyle ButtonStyle
    {
        get => _buttonStyle;
        set
        {
            if (_buttonStyle != value)
            {
                _buttonStyle = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置是否为切换按钮。
    /// </summary>
    [Category("工业控件")]
    [Description("是否为切换按钮（点击切换状态）")]
    [DefaultValue(false)]
    public bool IsToggle
    {
        get => _isToggle;
        set
        {
            if (_isToggle != value)
            {
                _isToggle = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置是否为选中状态（切换模式下有效）。
    /// </summary>
    [Category("工业控件")]
    [Description("是否为选中状态")]
    [DefaultValue(false)]
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                Invalidate();
                OnCheckedChanged(EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// 获取或设置正常状态颜色。
    /// </summary>
    [Category("外观")]
    [Description("正常状态背景颜色")]
    public Color NormalColor
    {
        get => _normalColor;
        set
        {
            if (_normalColor != value)
            {
                _normalColor = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置悬停状态颜色。
    /// </summary>
    [Category("外观")]
    [Description("悬停状态背景颜色")]
    public Color HoverColor
    {
        get => _hoverColor;
        set
        {
            if (_hoverColor != value)
            {
                _hoverColor = value;
            }
        }
    }

    /// <summary>
    /// 获取或设置按下状态颜色。
    /// </summary>
    [Category("外观")]
    [Description("按下状态背景颜色")]
    public Color PressedColor
    {
        get => _pressedColor;
        set
        {
            if (_pressedColor != value)
            {
                _pressedColor = value;
            }
        }
    }

    /// <summary>
    /// 获取或设置禁用状态颜色。
    /// </summary>
    [Category("外观")]
    [Description("禁用状态背景颜色")]
    public Color DisabledColor
    {
        get => _disabledColor;
        set
        {
            if (_disabledColor != value)
            {
                _disabledColor = value;
            }
        }
    }

    /// <summary>
    /// 获取或设置选中状态颜色。
    /// </summary>
    [Category("外观")]
    [Description("选中状态背景颜色")]
    public Color CheckedColor
    {
        get => _checkedColor;
        set
        {
            if (_checkedColor != value)
            {
                _checkedColor = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置快捷键文本。
    /// </summary>
    [Category("工业控件")]
    [Description("显示在按钮上的快捷键文本，如 Ctrl+S")]
    [DefaultValue("")]
    public string ShortcutKey
    {
        get => _shortcutKey;
        set
        {
            if (_shortcutKey != value)
            {
                _shortcutKey = value;
                if (_showShortcut) Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置是否显示快捷键。
    /// </summary>
    [Category("工业控件")]
    [Description("是否显示快捷键提示")]
    [DefaultValue(true)]
    public bool ShowShortcut
    {
        get => _showShortcut;
        set
        {
            if (_showShortcut != value)
            {
                _showShortcut = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置状态文本（显示在按钮下方）。
    /// </summary>
    [Category("工业控件")]
    [Description("按钮下方显示的状态文本")]
    [DefaultValue("")]
    public string StatusText
    {
        get => _statusText;
        set
        {
            if (_statusText != value)
            {
                _statusText = value;
                Invalidate();
            }
        }
    }

    #endregion

    #region 事件

    /// <summary>
    /// 按钮状态变更时触发。
    /// </summary>
    public event EventHandler? ButtonStateChanged;

    /// <summary>
    /// 选中状态变更时触发。
    /// </summary>
    public event EventHandler? CheckedChanged;

    /// <summary>
    /// 触发按钮状态变更事件。
    /// </summary>
    protected virtual void OnButtonStateChanged(EventArgs e)
    {
        ButtonStateChanged?.Invoke(this, e);
    }

    /// <summary>
    /// 触发选中状态变更事件。
    /// </summary>
    protected virtual void OnCheckedChanged(EventArgs e)
    {
        CheckedChanged?.Invoke(this, e);
    }

    #endregion

    #region 字段

    private bool _isMouseDown = false;
    private bool _isMouseOver = false;
    private bool _isSpaceKeyDown = false;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化工业按钮。
    /// </summary>
    public IndustrialButton()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer |
                  ControlStyles.AllPaintingInWmPaint |
                  ControlStyles.Selectable, true);
        Size = new Size(120, 40);
        TabStop = true;
        Cursor = Cursors.Hand;
    }

    #endregion

    #region 方法

    /// <summary>
    /// 获取当前背景颜色。
    /// </summary>
    private Color GetCurrentColor()
    {
        if (!Enabled)
            return _disabledColor;

        if (_isToggle && _isChecked)
            return _checkedColor;

        if (_isMouseDown)
            return _pressedColor;

        if (_isMouseOver)
            return _hoverColor;

        return _normalColor;
    }

    /// <summary>
    /// 设置快捷键。
    /// </summary>
    public void SetShortcut(Keys key)
    {
        var modifier = key & Keys.Modifiers;
        var keyCode = key & Keys.KeyCode;

        _shortcutKey = string.Empty;
        if (modifier.HasFlag(Keys.Control)) _shortcutKey += "Ctrl+";
        if (modifier.HasFlag(Keys.Alt)) _shortcutKey += "Alt+";
        if (modifier.HasFlag(Keys.Shift)) _shortcutKey += "Shift+";
        _shortcutKey += keyCode.ToString();

        if (_showShortcut) Invalidate();
    }

    /// <summary>
    /// 设置为运行按钮（绿色）。
    /// </summary>
    public void SetRunButton()
    {
        _normalColor = Color.FromArgb(34, 139, 34);
        _hoverColor = Color.FromArgb(50, 160, 50);
        _pressedColor = Color.FromArgb(20, 100, 20);
        Invalidate();
    }

    /// <summary>
    /// 设置为停止按钮（红色）。
    /// </summary>
    public void SetStopButton()
    {
        _normalColor = Color.FromArgb(178, 34, 34);
        _hoverColor = Color.FromArgb(200, 50, 50);
        _pressedColor = Color.FromArgb(120, 20, 20);
        Invalidate();
    }

    /// <summary>
    /// 设置为急停按钮。
    /// </summary>
    public void SetEmergencyButton()
    {
        ButtonStyle = IndustrialButtonStyle.Emergency;
        _normalColor = Color.FromArgb(220, 50, 50);
        _hoverColor = Color.FromArgb(255, 80, 80);
        _pressedColor = Color.FromArgb(180, 30, 30);
        Size = new Size(80, 80);
        Invalidate();
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

            var currentColor = GetCurrentColor();
            var textColor = Enabled ? Color.White : Color.FromArgb(180, 180, 180);

            switch (_buttonStyle)
            {
                case IndustrialButtonStyle.Standard:
                    PaintStandard(g, currentColor, textColor);
                    break;
                case IndustrialButtonStyle.Raised:
                    PaintRaised(g, currentColor, textColor);
                    break;
                case IndustrialButtonStyle.Flat:
                    PaintFlat(g, currentColor, textColor);
                    break;
                case IndustrialButtonStyle.Circular:
                    PaintCircular(g, currentColor, textColor);
                    break;
                case IndustrialButtonStyle.Emergency:
                    PaintEmergency(g, textColor);
                    break;
            }

            // 绘制状态文本
            if (!string.IsNullOrEmpty(_statusText))
            {
                using var statusFont = new Font("微软雅黑", 7F);
                using var statusBrush = new SolidBrush(Enabled ? Color.FromArgb(150, 150, 150) : Color.FromArgb(100, 100, 100));
                var statusSize = g.MeasureString(_statusText, statusFont);
                var statusX = (ClientSize.Width - statusSize.Width) / 2;
                var statusY = ClientSize.Height - statusSize.Height - 2;
                g.DrawString(_statusText, statusFont, statusBrush, statusX, statusY);
            }

            // 绘制焦点框
            if (Focused && ShowFocusCues)
            {
                using var focusPen = new Pen(Color.FromArgb(100, 255, 255, 255), 1);
                focusPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                g.DrawRectangle(focusPen, 3, 3, ClientSize.Width - 7, ClientSize.Height - 7);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"IndustrialButton OnPaint error: {ex.Message}");
        }
    }

    /// <summary>
    /// 绘制标准样式。
    /// </summary>
    private void PaintStandard(Graphics g, Color bgColor, Color textColor)
    {
        var rect = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);

        // 背景
        using var bgBrush = new SolidBrush(bgColor);
        g.FillRectangle(bgBrush, rect);

        // 边框
        var borderColor = _isMouseDown ? Color.FromArgb(30, 30, 30) : Color.FromArgb(80, 80, 80);
        using var borderPen = new Pen(borderColor, 1);
        g.DrawRectangle(borderPen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);

        DrawText(g, textColor);
    }

    /// <summary>
    /// 绘制凸起样式（3D效果）。
    /// </summary>
    private void PaintRaised(Graphics g, Color bgColor, Color textColor)
    {
        var rect = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
        var cornerRadius = 4;

        // 背景
        using var path = CreateRoundedRectanglePath(rect, cornerRadius);
        using var bgBrush = new SolidBrush(bgColor);
        g.FillPath(bgBrush, path);

        // 3D效果 - 高光
        if (!_isMouseDown && Enabled)
        {
            var highlightRect = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height / 2);
            using var highlightPath = CreateRoundedRectanglePath(highlightRect, cornerRadius);
            using var highlightBrush = new SolidBrush(Color.FromArgb(30, 255, 255, 255));
            g.FillPath(highlightBrush, highlightPath);
        }

        // 阴影
        using var shadowPen = new Pen(Color.FromArgb(60, 0, 0, 0), _isMouseDown ? 1 : 2);
        g.DrawPath(shadowPen, path);

        // 顶部高光线
        using var topLinePen = new Pen(Color.FromArgb(60, 255, 255, 255), 1);
        g.DrawLine(topLinePen, cornerRadius + 1, 1, ClientSize.Width - cornerRadius - 1, 1);

        DrawText(g, textColor);
    }

    /// <summary>
    /// 绘制扁平样式。
    /// </summary>
    private void PaintFlat(Graphics g, Color bgColor, Color textColor)
    {
        var rect = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);

        using var bgBrush = new SolidBrush(bgColor);
        g.FillRectangle(bgBrush, rect);

        if (_isMouseOver && !_isMouseDown && Enabled)
        {
            using var borderPen = new Pen(Color.FromArgb(100, 255, 255, 255), 1);
            g.DrawRectangle(borderPen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
        }

        DrawText(g, textColor);
    }

    /// <summary>
    /// 绘制圆形样式。
    /// </summary>
    private void PaintCircular(Graphics g, Color bgColor, Color textColor)
    {
        var diameter = Math.Min(ClientSize.Width, ClientSize.Height) - 4;
        var rect = new Rectangle(
            (ClientSize.Width - diameter) / 2,
            (ClientSize.Height - diameter) / 2,
            diameter,
            diameter);

        // 发光效果
        if (Enabled && (_isMouseOver || _isChecked))
        {
            using var glowBrush = new SolidBrush(Color.FromArgb(40, bgColor));
            g.FillEllipse(glowBrush, new Rectangle(rect.X - 4, rect.Y - 4, rect.Width + 8, rect.Height + 8));
        }

        // 背景
        using var bgBrush = new SolidBrush(bgColor);
        g.FillEllipse(bgBrush, rect);

        // 边框
        using var borderPen = new Pen(Color.FromArgb(40, 40, 40), 2);
        g.DrawEllipse(borderPen, rect);

        DrawText(g, textColor, true);
    }

    /// <summary>
    /// 绘制急停样式。
    /// </summary>
    private void PaintEmergency(Graphics g, Color textColor)
    {
        var diameter = Math.Min(ClientSize.Width, ClientSize.Height) - 4;
        var rect = new Rectangle(
            (ClientSize.Width - diameter) / 2,
            (ClientSize.Height - diameter) / 2,
            diameter,
            diameter);

        // 外部红色圆环
        using var outerBrush = new SolidBrush(Color.FromArgb(40, 0, 0));
        g.FillEllipse(outerBrush, new Rectangle(rect.X - 6, rect.Y - 6, rect.Width + 12, rect.Height + 12));

        // 背景
        var bgColor = GetCurrentColor();
        using var bgBrush = new SolidBrush(bgColor);
        g.FillEllipse(bgBrush, rect);

        // 内部凹陷效果
        using var innerPen = new Pen(Color.FromArgb(80, 0, 0), _isMouseDown ? 4 : 3);
        g.DrawEllipse(innerPen, rect.X + 8, rect.Y + 8, rect.Width - 16, rect.Height - 16);

        // 绘制STOP文字
        using var font = new Font("Arial", Math.Max(8, diameter / 5), FontStyle.Bold);
        using var textBrush = new SolidBrush(textColor);
        var text = "STOP";
        var textSize = g.MeasureString(text, font);
        var textX = rect.X + (rect.Width - textSize.Width) / 2;
        var textY = rect.Y + (rect.Height - textSize.Height) / 2;
        g.DrawString(text, font, textBrush, textX, textY);
    }

    /// <summary>
    /// 绘制文本。
    /// </summary>
    private void DrawText(Graphics g, Color textColor, bool centered = false)
    {
        var displayText = Text;
        if (_showShortcut && !string.IsNullOrEmpty(_shortcutKey))
        {
            displayText += $" ({_shortcutKey})";
        }

        using var font = new Font("微软雅黑", 9F, FontStyle.Regular);
        using var textBrush = new SolidBrush(textColor);
        var textSize = g.MeasureString(displayText, font);

        float textX, textY;
        if (centered)
        {
            textX = (ClientSize.Width - textSize.Width) / 2;
            textY = (ClientSize.Height - textSize.Height) / 2;
        }
        else
        {
            textX = (ClientSize.Width - textSize.Width) / 2;
            textY = (ClientSize.Height - textSize.Height) / 2 - 4;
        }

        g.DrawString(displayText, font, textBrush, textX, textY);
    }

    /// <summary>
    /// 创建圆角矩形路径。
    /// </summary>
    private static GraphicsPath CreateRoundedRectanglePath(Rectangle rect, int radius)
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
    protected override bool IsInputKey(Keys keyData)
    {
        if (keyData == Keys.Space) return true;
        return base.IsInputKey(keyData);
    }

    /// <inheritdoc />
    protected override void OnMouseDown(MouseEventArgs mevent)
    {
        base.OnMouseDown(mevent);
        _isMouseDown = true;
        ButtonState = IndustrialButtonState.Pressed;
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnMouseUp(MouseEventArgs mevent)
    {
        base.OnMouseUp(mevent);
        _isMouseDown = false;

        if (_isToggle)
        {
            _isChecked = !_isChecked;
            ButtonState = _isChecked ? IndustrialButtonState.Checked : IndustrialButtonState.Normal;
        }
        else
        {
            ButtonState = IndustrialButtonState.Normal;
        }

        Invalidate();

        // 触发点击事件
        OnClick(EventArgs.Empty);
    }

    /// <inheritdoc />
    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _isMouseOver = true;
        if (!_isMouseDown)
        {
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _isMouseOver = false;
        if (!_isMouseDown)
        {
            Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        _isMouseDown = false;
        _isSpaceKeyDown = false;
        Invalidate();
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
        {
            if (!_isSpaceKeyDown)
            {
                _isSpaceKeyDown = true;
                _isMouseDown = true;
                ButtonState = IndustrialButtonState.Pressed;
                Invalidate();
            }
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);

        if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
        {
            if (_isSpaceKeyDown)
            {
                _isSpaceKeyDown = false;
                _isMouseDown = false;

                if (_isToggle)
                {
                    _isChecked = !_isChecked;
                    ButtonState = _isChecked ? IndustrialButtonState.Checked : IndustrialButtonState.Normal;
                }
                else
                {
                    ButtonState = IndustrialButtonState.Normal;
                }

                Invalidate();
                OnClick(EventArgs.Empty);
            }
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override Size DefaultSize => new Size(120, 40);

    #endregion
}
