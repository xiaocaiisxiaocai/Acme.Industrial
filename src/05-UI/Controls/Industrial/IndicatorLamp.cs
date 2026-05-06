using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Acme.Industrial.UI.Controls.Industrial;

/// <summary>
/// 指示灯状态。
/// </summary>
public enum LampState
{
    /// <summary>关闭。</summary>
    Off,

    /// <summary>点亮。</summary>
    On,

    /// <summary>闪烁。</summary>
    Flashing
}

/// <summary>
/// 指示灯颜色方案。
/// </summary>
public enum LampColorScheme
{
    /// <summary>红色 - 通常用于报警。</summary>
    Red,

    /// <summary>绿色 - 通常用于正常运行。</summary>
    Green,

    /// <summary>黄色 - 通常用于警告。</summary>
    Yellow,

    /// <summary>蓝色 - 通常用于信息。</summary>
    Blue,

    /// <summary>白色。</summary>
    White,

    /// <summary>灰色。</summary>
    Gray
}

/// <summary>
/// 工业指示灯控件。
/// 支持开/关/闪烁三种状态，颜色可配置。
/// </summary>
[DefaultProperty(nameof(State))]
[DefaultBindingProperty(nameof(State))]
public class IndicatorLamp : Control
{
    #region 属性

    private LampState _state = LampState.Off;
    private LampColorScheme _colorScheme = LampColorScheme.Green;
    private bool _showBorder = true;
    private int _flashInterval = 500;
    private bool _useGradient = true;

    /// <summary>
    /// 获取或设置指示灯状态。
    /// </summary>
    [Category("工业控件")]
    [Description("指示灯状态：Off-关闭，On-点亮，Flashing-闪烁")]
    [DefaultValue(LampState.Off)]
    public LampState State
    {
        get => _state;
        set
        {
            if (_state != value)
            {
                _state = value;
                UpdateTimer();
                Invalidate();
                OnStateChanged(EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// 获取或设置颜色方案。
    /// </summary>
    [Category("工业控件")]
    [Description("指示灯颜色方案")]
    [DefaultValue(LampColorScheme.Green)]
    public LampColorScheme ColorScheme
    {
        get => _colorScheme;
        set
        {
            if (_colorScheme != value)
            {
                _colorScheme = value;
                Invalidate();
                RaiseColorSchemeChanged(EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// 获取或设置是否显示边框。
    /// </summary>
    [Category("外观")]
    [Description("是否显示边框")]
    [DefaultValue(true)]
    public bool ShowBorder
    {
        get => _showBorder;
        set
        {
            if (_showBorder != value)
            {
                _showBorder = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置闪烁间隔（毫秒）。
    /// </summary>
    [Category("工业控件")]
    [Description("闪烁状态的时间间隔（毫秒）")]
    [DefaultValue(500)]
    [Browsable(true)]
    public int FlashInterval
    {
        get => _flashInterval;
        set
        {
            if (value < 100) value = 100;
            if (value > 5000) value = 5000;
            if (_flashInterval != value)
            {
                _flashInterval = value;
                if (_timer != null)
                {
                    _timer.Interval = _flashInterval;
                }
            }
        }
    }

    /// <summary>
    /// 获取或设置是否使用渐变效果。
    /// </summary>
    [Category("外观")]
    [Description("是否使用渐变效果")]
    [DefaultValue(true)]
    public bool UseGradient
    {
        get => _useGradient;
        set
        {
            if (_useGradient != value)
            {
                _useGradient = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置标签文本。
    /// </summary>
    [Category("外观")]
    [Description("指示灯旁边的标签文本")]
    [DefaultValue("")]
    public string LabelText { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置标签位置。
    /// </summary>
    [Category("布局")]
    [Description("标签相对于指示灯的位置")]
    [DefaultValue(ContentAlignment.BottomCenter)]
    public ContentAlignment LabelPosition { get; set; } = ContentAlignment.BottomCenter;

    #endregion

    #region 字段

    private System.Windows.Forms.Timer? _timer;
    private bool _flashVisible = true;
    private Color _onColor;
    private Color _onColorLight;
    private Color _offColor;

    #endregion

    #region 事件

    /// <summary>
    /// 状态变更时触发。
    /// </summary>
    public event EventHandler? StateChanged;

    /// <summary>
    /// 颜色方案变更时触发。
    /// </summary>
    public event EventHandler? ColorSchemeChanged;

    /// <summary>
    /// 触发状态变更事件。
    /// </summary>
    protected virtual void OnStateChanged(EventArgs e)
    {
        StateChanged?.Invoke(this, e);
    }

    /// <summary>
    /// 触发颜色方案变更事件。
    /// </summary>
    protected virtual void RaiseColorSchemeChanged(EventArgs e)
    {
        ColorSchemeChanged?.Invoke(this, e);
    }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化指示灯控件。
    /// </summary>
    public IndicatorLamp()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        Size = new Size(40, 60);
        UpdateColors();
    }

    static IndicatorLamp()
    {
        // 确保控件样式支持透明背景
    }

    #endregion

    #region 方法

    /// <summary>
    /// 更新颜色配置。
    /// </summary>
    private void UpdateColors()
    {
        (_onColor, _onColorLight, _offColor) = _colorScheme switch
        {
            LampColorScheme.Red => (Color.FromArgb(220, 50, 50), Color.FromArgb(255, 120, 120), Color.FromArgb(100, 60, 60)),
            LampColorScheme.Green => (Color.FromArgb(50, 180, 50), Color.FromArgb(120, 255, 120), Color.FromArgb(40, 80, 40)),
            LampColorScheme.Yellow => (Color.FromArgb(220, 200, 50), Color.FromArgb(255, 240, 120), Color.FromArgb(100, 90, 40)),
            LampColorScheme.Blue => (Color.FromArgb(50, 120, 220), Color.FromArgb(120, 180, 255), Color.FromArgb(40, 60, 100)),
            LampColorScheme.White => (Color.FromArgb(240, 240, 240), Color.FromArgb(255, 255, 255), Color.FromArgb(180, 180, 180)),
            LampColorScheme.Gray => (Color.FromArgb(120, 120, 120), Color.FromArgb(180, 180, 180), Color.FromArgb(80, 80, 80)),
            _ => (Color.FromArgb(50, 180, 50), Color.FromArgb(120, 255, 120), Color.FromArgb(40, 80, 40))
        };
    }

    /// <summary>
    /// 更新闪烁定时器。
    /// </summary>
    private void UpdateTimer()
    {
        if (_state == LampState.Flashing)
        {
            if (_timer == null)
            {
                _timer = new System.Windows.Forms.Timer();
                _timer.Tick += Timer_Tick;
                _timer.Interval = _flashInterval;
            }
            _timer.Start();
            _flashVisible = true;
        }
        else
        {
            _timer?.Stop();
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        _flashVisible = !_flashVisible;
        Invalidate();
    }

    /// <summary>
    /// 设置为报警状态（红色闪烁）。
    /// </summary>
    public void SetAlarm()
    {
        ColorScheme = LampColorScheme.Red;
        State = LampState.Flashing;
    }

    /// <summary>
    /// 设置为运行状态（绿色点亮）。
    /// </summary>
    public void SetRunning()
    {
        ColorScheme = LampColorScheme.Green;
        State = LampState.On;
    }

    /// <summary>
    /// 设置为停止状态（灰色关闭）。
    /// </summary>
    public void SetStopped()
    {
        ColorScheme = LampColorScheme.Gray;
        State = LampState.Off;
    }

    /// <summary>
    /// 设置为警告状态（黄色闪烁）。
    /// </summary>
    public void SetWarning()
    {
        ColorScheme = LampColorScheme.Yellow;
        State = LampState.Flashing;
    }

    /// <summary>
    /// 绑定到布尔值。
    /// </summary>
    public void BindToBoolean(bool value, bool invert = false)
    {
        State = (value ^ invert) ? LampState.On : LampState.Off;
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

            var lampSize = Math.Min(ClientSize.Width - 4, ClientSize.Height - 20);
            var lampRect = new Rectangle(
                (ClientSize.Width - lampSize) / 2,
                2,
                lampSize,
                lampSize);

            // 确定绘制颜色
            var currentColor = _state switch
            {
                LampState.Off => _offColor,
                LampState.On => _onColor,
                LampState.Flashing => _flashVisible ? _onColor : _offColor,
                _ => _offColor
            };

            // 绘制发光效果
            if (_state != LampState.Off && currentColor != _offColor)
            {
                using var glowBrush = new SolidBrush(Color.FromArgb(80, currentColor));
                var glowRect = new Rectangle(
                    lampRect.X - 4,
                    lampRect.Y - 4,
                    lampRect.Width + 8,
                    lampRect.Height + 8);
                g.FillEllipse(glowBrush, glowRect);
            }

            // 绘制灯体
            if (_useGradient)
            {
                using var gradientBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    lampRect,
                    _state == LampState.Off ? _offColor : _onColorLight,
                    currentColor,
                    System.Drawing.Drawing2D.LinearGradientMode.ForwardDiagonal);
                g.FillEllipse(gradientBrush, lampRect);
            }
            else
            {
                using var solidBrush = new SolidBrush(currentColor);
                g.FillEllipse(solidBrush, lampRect);
            }

            // 绘制高光
            if (_state != LampState.Off)
            {
                using var highlightBrush = new SolidBrush(Color.FromArgb(150, 255, 255, 255));
                var highlightRect = new Rectangle(
                    lampRect.X + lampSize / 6,
                    lampRect.Y + lampSize / 6,
                    lampSize / 3,
                    lampSize / 3);
                g.FillEllipse(highlightBrush, highlightRect);
            }

            // 绘制边框
            if (_showBorder)
            {
                using var borderPen = new Pen(Color.FromArgb(80, 80, 80), 2);
                g.DrawEllipse(borderPen, lampRect);
            }

            // 绘制标签
            if (!string.IsNullOrEmpty(LabelText))
            {
                using var fontBrush = new SolidBrush(ForeColor);
                var format = new StringFormat
                {
                    Alignment = LabelPosition switch
                    {
                        ContentAlignment.TopCenter => StringAlignment.Center,
                        ContentAlignment.BottomCenter => StringAlignment.Center,
                        ContentAlignment.MiddleLeft => StringAlignment.Near,
                        ContentAlignment.MiddleRight => StringAlignment.Far,
                        _ => StringAlignment.Center
                    },
                    LineAlignment = LabelPosition switch
                    {
                        ContentAlignment.TopCenter => StringAlignment.Near,
                        ContentAlignment.BottomCenter => StringAlignment.Far,
                        _ => StringAlignment.Center
                    }
                };

                var labelRect = new Rectangle(0, lampSize + 4, ClientSize.Width, ClientSize.Height - lampSize - 6);
                g.DrawString(LabelText, Font, fontBrush, labelRect, format);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"IndicatorLamp OnPaint error: {ex.Message}");
        }
    }

    #endregion

    #region 控件行为

    /// <inheritdoc />
    protected override Size DefaultSize => new Size(40, 60);

    /// <summary>
    /// 当颜色方案变更时更新颜色。
    /// </summary>
    protected void OnColorSchemeValueChanged()
    {
        UpdateColors();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer?.Stop();
            _timer?.Dispose();
        }
        base.Dispose(disposing);
    }

    #endregion
}
