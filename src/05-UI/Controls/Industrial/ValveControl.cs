using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Acme.Industrial.UI.Controls.Industrial;

/// <summary>
/// 阀门类型。
/// </summary>
public enum ValveType
{
    /// <summary>球阀。</summary>
    Ball,

    /// <summary>闸阀。</summary>
    Gate,

    /// <summary>截止阀。</summary>
    Globe,

    /// <summary>蝶阀。</summary>
    Butterfly
}

/// <summary>
/// 阀门状态。
/// </summary>
public enum ValveState
{
    /// <summary>关闭。</summary>
    Closed,

    /// <summary>打开。</summary>
    Open,

    /// <summary>正在打开。</summary>
    Opening,

    /// <summary>正在关闭。</summary>
    Closing,

    /// <summary>故障。</summary>
    Fault
}

/// <summary>
/// 阀门控件。
/// 用于显示和控制工业阀门。
/// </summary>
[DefaultProperty(nameof(State))]
public class ValveControl : Control
{
    #region 属性

    private ValveType _valveType = ValveType.Ball;
    private ValveState _state = ValveState.Closed;
    private double _openPosition = 0;
    private Color _openColor = Color.FromArgb(50, 180, 50);
    private Color _closedColor = Color.FromArgb(180, 180, 180);
    private Color _faultColor = Color.FromArgb(220, 50, 50);
    private Color _bodyColor = Color.FromArgb(100, 100, 110);
    private Color _pipeColor = Color.FromArgb(150, 150, 160);
    private bool _showPipe = true;
    private bool _showLabel = true;
    private string _labelText = "阀门";
    private bool _isAnimated = true;
    private bool _showValue = true;

    /// <summary>
    /// 阀门类型。
    /// </summary>
    [Category("工业控件")]
    [Description("阀门类型")]
    [DefaultValue(ValveType.Ball)]
    public ValveType ValveType
    {
        get => _valveType;
        set { _valveType = value; Invalidate(); }
    }

    /// <summary>
    /// 阀门状态。
    /// </summary>
    [Category("工业控件")]
    [Description("阀门当前状态")]
    [DefaultValue(ValveState.Closed)]
    public ValveState State
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
    /// 打开位置（0-100%）。
    /// </summary>
    [Category("工业控件")]
    [Description("阀门打开位置百分比")]
    [DefaultValue(0.0)]
    public double OpenPosition
    {
        get => _openPosition;
        set
        {
            _openPosition = Math.Max(0, Math.Min(100, value));
            Invalidate();
        }
    }

    /// <summary>
    /// 打开状态颜色。
    /// </summary>
    [Category("外观")]
    public Color OpenColor
    {
        get => _openColor;
        set { _openColor = value; Invalidate(); }
    }

    /// <summary>
    /// 关闭状态颜色。
    /// </summary>
    [Category("外观")]
    public Color ClosedColor
    {
        get => _closedColor;
        set { _closedColor = value; Invalidate(); }
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
    /// 是否显示管道。
    /// </summary>
    [Category("行为")]
    [DefaultValue(true)]
    public bool ShowPipe
    {
        get => _showPipe;
        set { _showPipe = value; Invalidate(); }
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
    [DefaultValue("阀门")]
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

    ///summary>
    /// 是否显示位置值。
    /// </summary>
    [Category("行为")]
    [DefaultValue(true)]
    public bool ShowValue
    {
        get => _showValue;
        set { _showValue = value; Invalidate(); }
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
    public event EventHandler? ValveClicked;

    /// <summary>
    /// 触发状态变更事件。
    /// </summary>
    protected virtual void OnStateChanged(EventArgs e)
    {
        StateChanged?.Invoke(this, e);
    }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化阀门控件。
    /// </summary>
    public ValveControl()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        Size = new Size(80, 100);
        Cursor = Cursors.Hand;
    }

    #endregion

    #region 方法

    /// <summary>
    /// 获取当前颜色。
    /// </summary>
    public Color GetCurrentColor()
    {
        return _state switch
        {
            ValveState.Open => _openColor,
            ValveState.Opening => Color.FromArgb(180, _openColor),
            ValveState.Closing => Color.FromArgb(180, _closedColor),
            ValveState.Fault => _faultColor,
            _ => _closedColor
        };
    }

    /// <summary>
    /// 打开阀门。
    /// </summary>
    public void Open()
    {
        State = ValveState.Opening;
        // 模拟动画
        if (_isAnimated)
        {
            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 50;
            var progress = 0;
            timer.Tick += (s, e) =>
            {
                progress += 10;
                OpenPosition = progress;
                if (progress >= 100)
                {
                    timer.Stop();
                    timer.Dispose();
                    State = ValveState.Open;
                }
            };
            timer.Start();
        }
        else
        {
            OpenPosition = 100;
            State = ValveState.Open;
        }
    }

    /// <summary>
    /// 关闭阀门。
    /// </summary>
    public void Close()
    {
        State = ValveState.Closing;
        if (_isAnimated)
        {
            var timer = new System.Windows.Forms.Timer();
            timer.Interval = 50;
            var progress = 100;
            timer.Tick += (s, e) =>
            {
                progress -= 10;
                OpenPosition = progress;
                if (progress <= 0)
                {
                    timer.Stop();
                    timer.Dispose();
                    State = ValveState.Closed;
                }
            };
            timer.Start();
        }
        else
        {
            OpenPosition = 0;
            State = ValveState.Closed;
        }
    }

    /// <summary>
    /// 切换阀门状态。
    /// </summary>
    public void Toggle()
    {
        if (_state == ValveState.Open || _state == ValveState.Opening)
            Close();
        else if (_state == ValveState.Closed || _state == ValveState.Closing)
            Open();
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

            switch (_valveType)
            {
                case ValveType.Ball:
                    DrawBallValve(g);
                    break;
                case ValveType.Gate:
                    DrawGateValve(g);
                    break;
                case ValveType.Globe:
                    DrawGlobeValve(g);
                    break;
                case ValveType.Butterfly:
                    DrawButterflyValve(g);
                    break;
            }

            // 绘制标签
            if (_showLabel)
            {
                DrawLabel(g);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ValveControl OnPaint error: {ex.Message}");
        }
    }

    /// <summary>
    /// 绘制球阀。
    /// </summary>
    private void DrawBallValve(Graphics g)
    {
        var centerX = ClientSize.Width / 2f;
        var centerY = ClientSize.Height / 2f;
        var bodySize = Math.Min(ClientSize.Width * 0.6f, ClientSize.Height * 0.4f);

        // 管道
        if (_showPipe)
        {
            DrawPipe(g, centerX);
        }

        // 阀体
        var bodyRect = new RectangleF(centerX - bodySize / 2, centerY - bodySize / 2, bodySize, bodySize);

        // 阀体阴影
        using var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0));
        g.FillEllipse(shadowBrush, bodyRect.X + 2, bodyRect.Y + 2, bodyRect.Width, bodyRect.Height);

        // 阀体渐变
        using var gradientBrush = new LinearGradientBrush(
            bodyRect,
            Color.FromArgb(150, 150, 160),
            Color.FromArgb(80, 80, 90),
            LinearGradientMode.ForwardDiagonal);
        using var bodyPen = new Pen(Color.FromArgb(60, 60, 70), 2);
        g.FillEllipse(gradientBrush, bodyRect);
        g.DrawEllipse(bodyPen, bodyRect);

        // 球阀手柄
        var rotation = (float)(_openPosition / 100.0 * 90);
        var handlex1 = centerX;
        var handley1 = centerY;
        var handlex2 = centerX + bodySize * 0.4f * (float)Math.Cos((rotation - 90) * Math.PI / 180);
        var handley2 = centerY + bodySize * 0.4f * (float)Math.Sin((rotation - 90) * Math.PI / 180);

        using var handlePen = new Pen(GetCurrentColor(), 6);
        handlePen.StartCap = LineCap.Round;
        handlePen.EndCap = LineCap.Round;
        g.DrawLine(handlePen, handlex1, handley1, handlex2, handley2);

        // 中心指示
        using var centerBrush = new SolidBrush(GetCurrentColor());
        g.FillEllipse(centerBrush, centerX - 5, centerY - 5, 10, 10);
    }

    /// <summary>
    /// 绘制闸阀。
    /// </summary>
    private void DrawGateValve(Graphics g)
    {
        var centerX = ClientSize.Width / 2f;
        var centerY = ClientSize.Height / 2f;
        var valveWidth = ClientSize.Width * 0.5f;
        var valveHeight = ClientSize.Height * 0.3f;

        if (_showPipe)
        {
            DrawPipe(g, centerX);
        }

        // 阀体
        var bodyRect = new RectangleF(centerX - valveWidth / 2, centerY - valveHeight / 2, valveWidth, valveHeight);

        using var bodyBrush = new SolidBrush(_bodyColor);
        using var bodyPen = new Pen(Color.FromArgb(50, 50, 60), 1);
        g.FillRectangle(bodyBrush, bodyRect);
        g.DrawRectangle(bodyPen, bodyRect);

        // 闸板（上下移动）
        var gateHeight = valveHeight * 0.8f;
        var gateY = centerY - valveHeight / 2 + (1 - (float)_openPosition / 100f) * (valveHeight - gateHeight);
        using var gateBrush = new SolidBrush(GetCurrentColor());
        g.FillRectangle(gateBrush, centerX - valveWidth * 0.3f, gateY, valveWidth * 0.6f, gateHeight);

        // 手轮
        var wheelY = centerY - valveHeight / 2 - 15;
        using var wheelBrush = new SolidBrush(Color.FromArgb(60, 60, 70));
        using var wheelPen = new Pen(Color.FromArgb(40, 40, 50), 2);
        g.FillEllipse(wheelBrush, centerX - 12, wheelY, 24, 24);
        g.DrawEllipse(wheelPen, centerX - 12, wheelY, 24, 24);

        // 手轮辐条
        g.DrawLine(wheelPen, centerX, wheelY + 6, centerX, wheelY + 18);
        g.DrawLine(wheelPen, centerX - 10, wheelY + 12, centerX + 10, wheelY + 12);
    }

    /// <summary>
    /// 绘制截止阀。
    /// </summary>
    private void DrawGlobeValve(Graphics g)
    {
        var centerX = ClientSize.Width / 2f;
        var centerY = ClientSize.Height / 2f;
        var valveSize = Math.Min(ClientSize.Width * 0.5f, ClientSize.Height * 0.35f);

        if (_showPipe)
        {
            DrawPipe(g, centerX);
        }

        // 阀体
        using var bodyBrush = new SolidBrush(_bodyColor);
        using var bodyPen = new Pen(Color.FromArgb(50, 50, 60), 1);

        // 中间圆形部分
        var midRect = new RectangleF(centerX - valveSize / 2, centerY - valveSize / 2, valveSize, valveSize);
        g.FillEllipse(bodyBrush, midRect);
        g.DrawEllipse(bodyPen, midRect);

        // 顶部和底部
        var topRect = new RectangleF(centerX - valveSize / 3, centerY - valveSize, valveSize * 2 / 3, valveSize / 2);
        var bottomRect = new RectangleF(centerX - valveSize / 3, centerY + valveSize / 2, valveSize * 2 / 3, valveSize / 2);
        g.FillRectangle(bodyBrush, topRect);
        g.FillRectangle(bodyBrush, bottomRect);
        g.DrawRectangle(bodyPen, topRect);
        g.DrawRectangle(bodyPen, bottomRect);

        // 阀瓣
        var discY = centerY - valveSize / 2 + (float)(_openPosition / 100.0 * valveSize * 0.4);
        using var discBrush = new SolidBrush(GetCurrentColor());
        g.FillEllipse(discBrush, centerX - valveSize / 4, discY, valveSize / 2, valveSize / 3);

        // 手轮
        var wheelY = centerY - valveSize - 10;
        using var wheelBrush = new SolidBrush(Color.FromArgb(60, 60, 70));
        using var wheelPen = new Pen(Color.FromArgb(40, 40, 50), 2);
        g.FillEllipse(wheelBrush, centerX - 10, wheelY, 20, 20);
        g.DrawEllipse(wheelPen, centerX - 10, wheelY, 20, 20);
    }

    /// <summary>
    /// 绘制蝶阀。
    /// </summary>
    private void DrawButterflyValve(Graphics g)
    {
        var centerX = ClientSize.Width / 2f;
        var centerY = ClientSize.Height / 2f;
        var valveSize = Math.Min(ClientSize.Width * 0.7f, ClientSize.Height * 0.6f);

        if (_showPipe)
        {
            DrawPipe(g, centerX);
        }

        // 阀体外圈
        var outerRadius = valveSize / 2;
        using var bodyBrush = new SolidBrush(_bodyColor);
        using var bodyPen = new Pen(Color.FromArgb(50, 50, 60), 2);
        g.FillEllipse(bodyBrush, centerX - outerRadius, centerY - outerRadius, valveSize, valveSize);
        g.DrawEllipse(bodyPen, centerX - outerRadius, centerY - outerRadius, valveSize, valveSize);

        // 内圈
        var innerRadius = valveSize * 0.3f;
        using var innerBrush = new SolidBrush(Color.FromArgb(120, 120, 130));
        g.FillEllipse(innerBrush, centerX - innerRadius, centerY - innerRadius, innerRadius * 2, innerRadius * 2);

        // 蝶板
        var angle = (float)(_openPosition / 100.0 * 90);
        var halfWidth = outerRadius * 0.85f;
        var halfHeight = valveSize * 0.08f;

        var gstate = g.Save();
        g.TranslateTransform(centerX, centerY);
        g.RotateTransform(angle);

        using var discBrush = new SolidBrush(GetCurrentColor());
        var discRect = new RectangleF(-halfWidth, -halfHeight, halfWidth * 2, halfHeight * 2);
        g.FillRectangle(discBrush, discRect);

        // 蝶板边缘
        using var edgePen = new Pen(Color.FromArgb(50, GetCurrentColor()), 1);
        g.DrawRectangle(edgePen, discRect.X, discRect.Y, discRect.Width, discRect.Height);

        g.Restore(gstate);

        // 中心轴
        using var axisBrush = new SolidBrush(Color.FromArgb(40, 40, 40));
        g.FillEllipse(axisBrush, centerX - 4, centerY - 4, 8, 8);

        // 手柄
        var handleAngle = (float)(_openPosition / 100.0 * 90 - 90);
        var handleLength = innerRadius * 1.3f;
        var handleEndX = centerX + handleLength * (float)Math.Cos(handleAngle * Math.PI / 180);
        var handleEndY = centerY + handleLength * (float)Math.Sin(handleAngle * Math.PI / 180);

        using var handlePen = new Pen(Color.FromArgb(200, 200, 200), 4);
        handlePen.StartCap = LineCap.Round;
        handlePen.EndCap = LineCap.ArrowAnchor;
        g.DrawLine(handlePen, centerX, centerY, handleEndX, handleEndY);
    }

    /// <summary>
    /// 绘制管道。
    /// </summary>
    private void DrawPipe(Graphics g, float centerX)
    {
        using var pipeBrush = new SolidBrush(_pipeColor);
        using var pipePen = new Pen(_pipeColor, 8);

        // 上管道
        g.FillRectangle(pipeBrush, centerX - 4, 0, 8, ClientSize.Height / 2f - 10);
        // 下管道
        g.FillRectangle(pipeBrush, centerX - 4, ClientSize.Height / 2f + 10, 8, ClientSize.Height / 2f);

        // 管道边框
        using var borderPen = new Pen(Color.FromArgb(60, 60, 70), 1);
        g.DrawRectangle(borderPen, centerX - 4, 0, 8, ClientSize.Height / 2f - 10);
        g.DrawRectangle(borderPen, centerX - 4, ClientSize.Height / 2f + 10, 8, ClientSize.Height / 2f);
    }

    /// <summary>
    /// 绘制标签。
    /// </summary>
    private void DrawLabel(Graphics g)
    {
        using var font = new Font("微软雅黑", 8f);
        using var brush = new SolidBrush(ForeColor);

        var text = _showValue ? $"{_labelText} ({_openPosition:F0}%)" : _labelText;
        var textSize = g.MeasureString(text, font);

        g.DrawString(text, font, brush,
            (ClientSize.Width - textSize.Width) / 2,
            ClientSize.Height - textSize.Height - 2);

        // 状态指示
        var statusText = _state switch
        {
            ValveState.Open => "OPEN",
            ValveState.Closed => "CLOSE",
            ValveState.Opening => "OPENING",
            ValveState.Closing => "CLOSING",
            ValveState.Fault => "FAULT",
            _ => ""
        };

        if (!string.IsNullOrEmpty(statusText))
        {
            using var statusFont = new Font("Arial", 7f, FontStyle.Bold);
            using var statusBrush = new SolidBrush(GetCurrentColor());
            var statusSize = g.MeasureString(statusText, statusFont);
            g.DrawString(statusText, statusFont, statusBrush,
                (ClientSize.Width - statusSize.Width) / 2,
                ClientSize.Height - textSize.Height - statusSize.Height - 4);
        }
    }

    #endregion

    #region 控件行为

    /// <inheritdoc />
    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
        Toggle();
        ValveClicked?.Invoke(this, e);
    }

    /// <inheritdoc />
    protected override Size DefaultSize => new Size(80, 100);

    #endregion
}
