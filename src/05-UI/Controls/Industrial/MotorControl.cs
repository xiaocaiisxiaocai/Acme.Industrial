using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Acme.Industrial.UI.Controls.Industrial;

/// <summary>
/// 电机状态。
/// </summary>
public enum MotorState
{
    /// <summary>停止。</summary>
    Stopped,

    /// <summary>运行中。</summary>
    Running,

    /// <summary>正在启动。</summary>
    Starting,

    /// <summary>正在停止。</summary>
    Stopping,

    /// <summary>故障。</summary>
    Fault
}

/// <summary>
/// 电机旋转方向。
/// </summary>
public enum MotorDirection
{
    /// <summary>正转。</summary>
    Forward,

    /// <summary>反转。</summary>
    Reverse,

    /// <summary>停止。</summary>
    None
}

/// <summary>
/// 电机控件。
/// 用于显示和控制工业电机。
/// </summary>
[DefaultProperty(nameof(State))]
public class MotorControl : Control
{
    #region 属性

    private MotorState _state = MotorState.Stopped;
    private MotorDirection _direction = MotorDirection.None;
    private double _speed = 0;
    private double _current = 0;
    private Color _runningColor = Color.FromArgb(50, 180, 50);
    private Color _stoppedColor = Color.FromArgb(180, 180, 180);
    private Color _faultColor = Color.FromArgb(220, 50, 50);
    private Color _startingColor = Color.FromArgb(255, 200, 0);
    private bool _showDirection = true;
    private bool _showSpeed = true;
    private bool _showCurrent = true;
    private bool _showLabel = true;
    private string _labelText = "电机";
    private int _rotationAngle = 0;
    private bool _isAnimated = true;
    private int _animationSpeed = 5;

    /// <summary>
    /// 电机状态。
    /// </summary>
    [Category("工业控件")]
    [Description("电机当前状态")]
    [DefaultValue(MotorState.Stopped)]
    public MotorState State
    {
        get => _state;
        set
        {
            _state = value;
            Invalidate();
            OnStateChanged(EventArgs.Empty);
        }
    }

    /// <summary>
    /// 旋转方向。
    /// </summary>
    [Category("工业控件")]
    [Description("电机旋转方向")]
    [DefaultValue(MotorDirection.None)]
    public MotorDirection Direction
    {
        get => _direction;
        set { _direction = value; Invalidate(); }
    }

    /// <summary>
    /// 当前速度（百分比）。
    /// </summary>
    [Category("工业控件")]
    [Description("电机速度百分比")]
    [DefaultValue(0.0)]
    public double Speed
    {
        get => _speed;
        set
        {
            _speed = Math.Max(0, Math.Min(100, value));
            Invalidate();
        }
    }

    /// <summary>
    /// 当前电流。
    /// </summary>
    [Category("工业控件")]
    [Description("电机当前电流值")]
    [DefaultValue(0.0)]
    public double Current
    {
        get => _current;
        set { _current = value; Invalidate(); }
    }

    /// <summary>
    /// 运行状态颜色。
    /// </summary>
    [Category("外观")]
    public Color RunningColor
    {
        get => _runningColor;
        set { _runningColor = value; Invalidate(); }
    }

    /// <summary>
    /// 停止状态颜色。
    /// </summary>
    [Category("外观")]
    public Color StoppedColor
    {
        get => _stoppedColor;
        set { _stoppedColor = value; Invalidate(); }
    }

    /// <summary>
    /// 故障状态颜色。
    /// </summary>
    [Category("外观")]
    public Color FaultColor
    {
        get => _faultColor;
        set { _faultColor = value; Invalidate(); }
    }

    /// <summary>
    /// 是否显示方向指示。
    /// </summary>
    [Category("行为")]
    [DefaultValue(true)]
    public bool ShowDirection
    {
        get => _showDirection;
        set { _showDirection = value; Invalidate(); }
    }

    /// <summary>
    /// 是否显示速度。
    /// </summary>
    [Category("行为")]
    [DefaultValue(true)]
    public bool ShowSpeed
    {
        get => _showSpeed;
        set { _showSpeed = value; Invalidate(); }
    }

    /// <summary>
    /// 是否显示标签。
    /// </summary>
    [Category("行为")]
    [DefaultValue(true)]
    public bool ShowLabel
    {
        get => _showLabel;
        set { _showLabel = value; Invalidate(); }
    }

    /// <summary>
    /// 标签文本。
    /// </summary>
    [Category("外观")]
    [DefaultValue("电机")]
    public string LabelText
    {
        get => _labelText;
        set { _labelText = value ?? ""; Invalidate(); }
    }

    /// <summary>
    /// 是否启用动画。
    /// </summary>
    [Category("行为")]
    [DefaultValue(true)]
    public bool IsAnimated
    {
        get => _isAnimated;
        set { _isAnimated = value; }
    }

    /// <summary>
    /// 动画速度。
    /// </summary>
    [Category("行为")]
    [DefaultValue(5)]
    public int AnimationSpeed
    {
        get => _animationSpeed;
        set { _animationSpeed = Math.Max(1, Math.Min(20, value)); }
    }

    #endregion

    #region 事件

    /// <summary>
    /// 状态变更时触发。
    /// </summary>
    public event EventHandler? StateChanged;

    /// <summary>
    /// 点击时触发。
    /// </summary>
    public event EventHandler? MotorClicked;

    /// <summary>
    /// 触发状态变更事件。
    /// </summary>
    protected virtual void OnStateChanged(EventArgs e)
    {
        StateChanged?.Invoke(this, e);
    }

    #endregion

    #region 字段

    private System.Windows.Forms.Timer? _animationTimer;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化电机控件。
    /// </summary>
    public MotorControl()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        Size = new Size(100, 140);
        Cursor = Cursors.Hand;

        _animationTimer = new System.Windows.Forms.Timer();
        _animationTimer.Interval = 50;
        _animationTimer.Tick += AnimationTimer_Tick;
    }

    private void AnimationTimer_Tick(object? sender, EventArgs e)
    {
        if (_state == MotorState.Running || _state == MotorState.Starting)
        {
            var increment = _animationSpeed;
            if (_direction == MotorDirection.Reverse) increment = -increment;
            _rotationAngle = (_rotationAngle + increment) % 360;
            Invalidate();
        }
    }

    #endregion

    #region 方法

    /// <summary>
    /// 获取当前状态颜色。
    /// </summary>
    public Color GetCurrentColor()
    {
        return _state switch
        {
            MotorState.Running => _runningColor,
            MotorState.Starting => _startingColor,
            MotorState.Stopping => Color.FromArgb(180, _stoppedColor),
            MotorState.Fault => _faultColor,
            _ => _stoppedColor
        };
    }

    /// <summary>
    /// 启动电机。
    /// </summary>
    public void Start(MotorDirection direction = MotorDirection.Forward)
    {
        Direction = direction;
        State = MotorState.Starting;
        _animationTimer?.Start();

        if (!_isAnimated)
        {
            State = MotorState.Running;
        }
    }

    /// <summary>
    /// 停止电机。
    /// </summary>
    public void Stop()
    {
        State = MotorState.Stopping;
        _animationTimer?.Stop();

        if (!_isAnimated)
        {
            State = MotorState.Stopped;
            Direction = MotorDirection.None;
        }
    }

    /// <summary>
    /// 切换运行/停止状态。
    /// </summary>
    public void Toggle()
    {
        if (_state == MotorState.Running || _state == MotorState.Starting)
            Stop();
        else
            Start(Direction == MotorDirection.None ? MotorDirection.Forward : Direction);
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

            var centerX = ClientSize.Width / 2f;
            var centerY = ClientSize.Height * 0.4f;
            var motorSize = Math.Min(ClientSize.Width * 0.8f, ClientSize.Height * 0.5f);

            // 绘制电机外壳
            DrawMotorBody(g, centerX, centerY, motorSize);

            // 绘制风扇叶片
            if (_state == MotorState.Running || _state == MotorState.Starting)
            {
                DrawFan(g, centerX, centerY, motorSize * 0.4f);
            }
            else
            {
                DrawStaticFan(g, centerX, centerY, motorSize * 0.4f);
            }

            // 绘制方向指示器
            if (_showDirection && (_state == MotorState.Running || _state == MotorState.Starting))
            {
                DrawDirectionIndicator(g, centerX, centerY, motorSize);
            }

            // 绘制标签和状态
            if (_showLabel)
            {
                DrawLabel(g);
            }

            // 绘制速度
            if (_showSpeed)
            {
                DrawSpeed(g, centerX, centerY, motorSize);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MotorControl OnPaint error: {ex.Message}");
        }
    }

    /// <summary>
    /// 绘制电机外壳。
    /// </summary>
    private void DrawMotorBody(Graphics g, float cx, float cy, float size)
    {
        var bodyWidth = size * 1.2f;
        var bodyHeight = size;
        var bodyRect = new RectangleF(cx - bodyWidth / 2, cy - bodyHeight / 2, bodyWidth, bodyHeight);

        // 阴影
        using var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0));
        g.FillRectangle(shadowBrush, bodyRect.X + 3, bodyRect.Y + 3, bodyWidth, bodyHeight);

        // 主体渐变
        using var gradientBrush = new LinearGradientBrush(
            bodyRect,
            Color.FromArgb(200, 200, 210),
            Color.FromArgb(140, 140, 150),
            LinearGradientMode.Vertical);
        using var bodyBrush = new SolidBrush(Color.FromArgb(170, 170, 180));
        g.FillRectangle(bodyBrush, bodyRect);

        // 边框
        using var borderPen = new Pen(Color.FromArgb(80, 80, 90), 2);
        g.DrawRectangle(borderPen, bodyRect.X, bodyRect.Y, bodyRect.Width, bodyRect.Height);

        // 顶部散热片
        var finHeight = 3;
        for (var i = 0; i < 5; i++)
        {
            var finY = bodyRect.Y + 5 + i * (finHeight + 2);
            using var finBrush = new SolidBrush(Color.FromArgb(120, 120, 130));
            g.FillRectangle(finBrush, bodyRect.X + 5, finY, bodyRect.Width - 10, finHeight);
        }

        // 状态指示灯
        var indicatorY = bodyRect.Y + bodyRect.Height - 15;
        DrawStatusIndicator(g, bodyRect.X + 10, indicatorY, 10);

        // 接线盒
        var boxWidth = bodyRect.Width * 0.4f;
        var boxHeight = bodyRect.Height * 0.25f;
        var boxRect = new RectangleF(
            bodyRect.X + bodyRect.Width - boxWidth - 5,
            bodyRect.Y + bodyRect.Height - boxHeight - 5,
            boxWidth, boxHeight);
        using var boxBrush = new SolidBrush(Color.FromArgb(100, 100, 110));
        g.FillRectangle(boxBrush, boxRect);
        using var boxPen = new Pen(Color.FromArgb(60, 60, 70), 1);
        g.DrawRectangle(boxPen, boxRect.X, boxRect.Y, boxRect.Width, boxRect.Height);

        // 接线端子
        for (var i = 0; i < 3; i++)
        {
            var termX = boxRect.X + 5 + i * (boxWidth - 10) / 2;
            using var termBrush = new SolidBrush(Color.FromArgb(220, 180, 50));
            g.FillEllipse(termBrush, termX, boxRect.Y + 5, 6, 6);
        }
    }

    /// <summary>
    /// 绘制状态指示灯。
    /// </summary>
    private void DrawStatusIndicator(Graphics g, float x, float y, float size)
    {
        var color = GetCurrentColor();

        // 发光效果
        if (_state == MotorState.Running)
        {
            using var glowBrush = new SolidBrush(Color.FromArgb(50, color));
            g.FillEllipse(glowBrush, x - 2, y - 2, size + 4, size + 4);
        }

        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, x, y, size, size);

        // 高光
        using var highlightBrush = new SolidBrush(Color.FromArgb(100, 255, 255, 255));
        g.FillEllipse(highlightBrush, x + 2, y + 2, size * 0.4f, size * 0.4f);
    }

    /// <summary>
    /// 绘制风扇（旋转）。
    /// </summary>
    private void DrawFan(Graphics g, float cx, float cy, float radius)
    {
        var gstate = g.Save();
        g.TranslateTransform(cx, cy);
        g.RotateTransform(_rotationAngle);

        // 风扇叶片
        using var fanBrush = new SolidBrush(Color.FromArgb(180, 180, 190));

        for (var i = 0; i < 6; i++)
        {
            g.RotateTransform(60);
            var bladeRect = new RectangleF(-radius * 0.3f, 0, radius * 0.6f, radius);
            g.FillEllipse(fanBrush, bladeRect);
        }

        g.Restore(gstate);

        // 中心轴
        using var axisBrush = new SolidBrush(Color.FromArgb(80, 80, 80));
        g.FillEllipse(axisBrush, cx - 6, cy - 6, 12, 12);
        using var axisCenterBrush = new SolidBrush(Color.FromArgb(40, 40, 40));
        g.FillEllipse(axisCenterBrush, cx - 3, cy - 3, 6, 6);
    }

    /// <summary>
    /// 绘制静态风扇。
    /// </summary>
    private void DrawStaticFan(Graphics g, float cx, float cy, float radius)
    {
        using var fanBrush = new SolidBrush(Color.FromArgb(150, 150, 160));

        // 简化的静态风扇
        for (var i = 0; i < 6; i++)
        {
            var angle = i * 60 * Math.PI / 180;
            var bladeX = cx + radius * 0.5f * (float)Math.Cos(angle);
            var bladeY = cy + radius * 0.5f * (float)Math.Sin(angle);
            g.FillEllipse(fanBrush, bladeX - 5, bladeY - 5, 10, 10);
        }

        using var axisBrush = new SolidBrush(Color.FromArgb(80, 80, 80));
        g.FillEllipse(axisBrush, cx - 6, cy - 6, 12, 12);
    }

    /// <summary>
    /// 绘制方向指示器。
    /// </summary>
    private void DrawDirectionIndicator(Graphics g, float cx, float cy, float size)
    {
        var arrowY = cy + size / 2 + 15;
        var arrowSize = 8;

        using var arrowPen = new Pen(GetCurrentColor(), 2);
        arrowPen.StartCap = LineCap.Round;
        arrowPen.EndCap = LineCap.ArrowAnchor;

        if (_direction == MotorDirection.Forward)
        {
            // 向上箭头
            g.DrawLine(arrowPen, cx, arrowY + arrowSize, cx, arrowY - arrowSize);
            g.DrawLine(arrowPen, cx - arrowSize, arrowY, cx, arrowY - arrowSize);
            g.DrawLine(arrowPen, cx + arrowSize, arrowY, cx, arrowY - arrowSize);
        }
        else if (_direction == MotorDirection.Reverse)
        {
            // 向下箭头
            g.DrawLine(arrowPen, cx, arrowY - arrowSize, cx, arrowY + arrowSize);
            g.DrawLine(arrowPen, cx - arrowSize, arrowY, cx, arrowY + arrowSize);
            g.DrawLine(arrowPen, cx + arrowSize, arrowY, cx, arrowY + arrowSize);
        }
    }

    /// <summary>
    /// 绘制标签。
    /// </summary>
    private void DrawLabel(Graphics g)
    {
        using var font = new Font("微软雅黑", 9f, FontStyle.Bold);
        using var brush = new SolidBrush(ForeColor);

        var text = _labelText;
        var textSize = g.MeasureString(text, font);
        g.DrawString(text, font, brush,
            (ClientSize.Width - textSize.Width) / 2,
            ClientSize.Height - 35);

        // 状态文本
        var statusText = _state switch
        {
            MotorState.Running => "运行",
            MotorState.Starting => "启动中",
            MotorState.Stopping => "停止中",
            MotorState.Stopped => "停止",
            MotorState.Fault => "故障",
            _ => ""
        };

        using var statusFont = new Font("微软雅黑", 8f);
        using var statusBrush = new SolidBrush(GetCurrentColor());
        var statusSize = g.MeasureString(statusText, statusFont);
        g.DrawString(statusText, statusFont, statusBrush,
            (ClientSize.Width - statusSize.Width) / 2,
            ClientSize.Height - 18);
    }

    /// <summary>
    /// 绘制速度。
    /// </summary>
    private void DrawSpeed(Graphics g, float cx, float cy, float size)
    {
        var speedY = cy + size / 2 + 5;

        using var font = new Font("Arial", 8f);
        using var brush = new SolidBrush(Color.FromArgb(100, 100, 100));
        var text = $"{(int)_speed}%";
        var textSize = g.MeasureString(text, font);
        g.DrawString(text, font, brush,
            (ClientSize.Width - textSize.Width) / 2,
            speedY);
    }

    #endregion

    #region 控件行为

    /// <inheritdoc />
    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        Toggle();
        MotorClicked?.Invoke(this, e);
    }

    /// <inheritdoc />
    protected override Size DefaultSize => new Size(100, 140);

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
