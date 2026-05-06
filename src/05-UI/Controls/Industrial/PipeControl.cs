using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Acme.Industrial.UI.Controls.Industrial;

/// <summary>
/// 管道方向。
/// </summary>
[Flags]
public enum PipeDirection
{
    None = 0,
    Top = 1,
    Bottom = 2,
    Left = 4,
    Right = 8,
    TopLeft = 16,
    TopRight = 32,
    BottomLeft = 64,
    BottomRight = 128
}

/// <summary>
/// 管道类型。
/// </summary>
public enum PipeStyle
{
    /// <summary>实心管道。</summary>
    Solid,

    /// <summary>带箭头（流向）。</summary>
    Flow,

    /// <summary>虚线管道。</summary>
    Dashed,

    /// <summary>双管道。</summary>
    Double
}

/// <summary>
/// 管道控件。
/// 用于显示工业管道，支持多种样式和方向。
/// </summary>
[DefaultProperty(nameof(Direction))]
public class PipeControl : Control
{
    /// <summary>
    /// 流向。
    /// </summary>
    public enum FlowDir
    {
        Forward,
        Backward
    }

    #region 属性

    private PipeDirection _direction = PipeDirection.Top | PipeDirection.Bottom;
    private PipeStyle _pipeStyle = PipeStyle.Solid;
    private Color _pipeColor = Color.FromArgb(150, 150, 160);
    private Color _pipeFillColor = Color.FromArgb(120, 120, 130);
    private Color _flowColor = Color.FromArgb(0, 150, 220);
    private int _pipeWidth = 20;
    private int _borderWidth = 3;
    private bool _showFlowArrow = false;
    private bool _isHorizontal = true;
    private bool _showValve = false;
    private bool _valveOpen = true;
    private bool _showPump = false;
    private bool _pumpRunning = false;
    private FlowDir _flowDirection = FlowDir.Forward;
    private int _cornerRadius = 10;

    /// <summary>
    /// 管道连接方向。
    /// </summary>
    [Category("工业控件")]
    [Description("管道连接方向")]
    [DefaultValue(PipeDirection.Top | PipeDirection.Bottom)]
    public PipeDirection Direction
    {
        get => _direction;
        set { _direction = value; Invalidate(); }
    }

    /// <summary>
    /// 管道样式。
    /// </summary>
    [Category("外观")]
    [Description("管道显示样式")]
    [DefaultValue(PipeStyle.Solid)]
    public PipeStyle PipeStyle
    {
        get => _pipeStyle;
        set { _pipeStyle = value; Invalidate(); }
    }

    /// <summary>
    /// 管道颜色。
    /// </summary>
    [Category("外观")]
    public Color PipeColor
    {
        get => _pipeColor;
        set { _pipeColor = value; Invalidate(); }
    }

    /// <summary>
    /// 管道填充颜色。
    /// </summary>
    [Category("外观")]
    public Color PipeFillColor
    {
        get => _pipeFillColor;
        set { _pipeFillColor = value; Invalidate(); }
    }

    /// <summary>
    /// 流向颜色。
    /// </summary>
    [Category("外观")]
    public Color FlowColor
    {
        get => _flowColor;
        set { _flowColor = value; Invalidate(); }
    }

    /// <summary>
    /// 管道宽度。
    /// </summary>
    [Category("外观")]
    [DefaultValue(20)]
    public int PipeWidth
    {
        get => _pipeWidth;
        set { _pipeWidth = Math.Max(5, value); Invalidate(); }
    }

    /// <summary>
    /// 是否显示流向箭头。
    /// </summary>
    [Category("行为")]
    [DefaultValue(false)]
    public bool ShowFlowArrow
    {
        get => _showFlowArrow;
        set { _showFlowArrow = value; Invalidate(); }
    }

    /// <summary>
    /// 是否水平显示。
    /// </summary>
    [Category("外观")]
    [DefaultValue(true)]
    public bool IsHorizontal
    {
        get => _isHorizontal;
        set { _isHorizontal = value; Invalidate(); }
    }

    /// <summary>
    /// 是否显示阀门。
    /// </summary>
    [Category("工业控件")]
    [DefaultValue(false)]
    public bool ShowValve
    {
        get => _showValve;
        set { _showValve = value; Invalidate(); }
    }

    /// <summary>
    /// 阀门是否打开。
    /// </summary>
    [Category("工业控件")]
    [DefaultValue(true)]
    public bool ValveOpen
    {
        get => _valveOpen;
        set { _valveOpen = value; Invalidate(); }
    }

    /// <summary>
    /// 是否显示泵。
    /// </summary>
    [Category("工业控件")]
    [DefaultValue(false)]
    public bool ShowPump
    {
        get => _showPump;
        set { _showPump = value; Invalidate(); }
    }

    /// <summary>
    /// 泵是否运行。
    /// </summary>
    [Category("工业控件")]
    [DefaultValue(false)]
    public bool PumpRunning
    {
        get => _pumpRunning;
        set { _pumpRunning = value; Invalidate(); }
    }

    /// <summary>
    /// 流向。
    /// </summary>
    [Category("行为")]
    [DefaultValue(FlowDir.Forward)]
    public FlowDir FlowDirection
    {
        get => _flowDirection;
        set { _flowDirection = value; Invalidate(); }
    }

    /// <summary>
    /// 圆角半径。
    /// </summary>
    [Category("外观")]
    [DefaultValue(10)]
    public int CornerRadius
    {
        get => _cornerRadius;
        set { _cornerRadius = Math.Max(0, value); Invalidate(); }
    }

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化管道控件。
    /// </summary>
    public PipeControl()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        Size = new Size(150, 60);
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

            // 绘制管道
            DrawPipe(g);

            // 绘制阀门
            if (_showValve)
            {
                DrawValve(g);
            }

            // 绘制泵
            if (_showPump)
            {
                DrawPump(g);
            }

            // 绘制流向箭头
            if (_showFlowArrow)
            {
                DrawFlowArrow(g);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PipeControl OnPaint error: {ex.Message}");
        }
    }

    /// <summary>
    /// 绘制管道。
    /// </summary>
    private void DrawPipe(Graphics g)
    {
        var halfWidth = _pipeWidth / 2;
        var borderWidth = _borderWidth;

        // 根据样式选择画笔
        using var pipeBrush = new SolidBrush(_pipeFillColor);
        using var borderPen = new Pen(_pipeColor, borderWidth);
        using var flowPen = new Pen(_flowColor, _pipeWidth - borderWidth * 2);

        switch (_pipeStyle)
        {
            case PipeStyle.Solid:
                DrawSolidPipe(g, halfWidth, pipeBrush, borderPen);
                break;
            case PipeStyle.Flow:
                DrawFlowPipe(g, halfWidth, pipeBrush, borderPen, flowPen);
                break;
            case PipeStyle.Dashed:
                DrawDashedPipe(g, halfWidth, borderPen);
                break;
            case PipeStyle.Double:
                DrawDoublePipe(g, halfWidth, pipeBrush, borderPen);
                break;
        }
    }

    /// <summary>
    /// 绘制实心管道。
    /// </summary>
    private void DrawSolidPipe(Graphics g, int halfWidth, Brush pipeBrush, Pen borderPen)
    {
        if ((_direction & PipeDirection.Left) != 0)
        {
            g.FillRectangle(pipeBrush, 0, halfWidth, ClientSize.Width / 2, _pipeWidth);
            g.DrawRectangle(borderPen, 0, halfWidth, ClientSize.Width / 2, _pipeWidth);
        }

        if ((_direction & PipeDirection.Right) != 0)
        {
            g.FillRectangle(pipeBrush, ClientSize.Width / 2, halfWidth, ClientSize.Width / 2, _pipeWidth);
            g.DrawRectangle(borderPen, ClientSize.Width / 2, halfWidth, ClientSize.Width / 2, _pipeWidth);
        }

        if ((_direction & PipeDirection.Top) != 0)
        {
            g.FillRectangle(pipeBrush, halfWidth, 0, _pipeWidth, ClientSize.Height / 2);
            g.DrawRectangle(borderPen, halfWidth, 0, _pipeWidth, ClientSize.Height / 2);
        }

        if ((_direction & PipeDirection.Bottom) != 0)
        {
            g.FillRectangle(pipeBrush, halfWidth, ClientSize.Height / 2, _pipeWidth, ClientSize.Height / 2);
            g.DrawRectangle(borderPen, halfWidth, ClientSize.Height / 2, _pipeWidth, ClientSize.Height / 2);
        }

        // 绘制中心连接
        DrawCenterConnection(g, halfWidth, pipeBrush, borderPen);
    }

    /// <summary>
    /// 绘制流向管道。
    /// </summary>
    private void DrawFlowPipe(Graphics g, int halfWidth, Brush pipeBrush, Pen borderPen, Pen flowPen)
    {
        // 先画基础管道
        DrawSolidPipe(g, halfWidth, pipeBrush, borderPen);

        // 再画流向
        flowPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
        flowPen.DashPattern = new float[] { 5, 5 };

        var dashOffset = (DateTime.Now.Second % 10) * 2;
        flowPen.DashOffset = dashOffset;

        if (_isHorizontal)
        {
            g.DrawLine(flowPen, 0, ClientSize.Height / 2f, ClientSize.Width, ClientSize.Height / 2f);
        }
        else
        {
            g.DrawLine(flowPen, ClientSize.Width / 2f, 0, ClientSize.Width / 2f, ClientSize.Height);
        }
    }

    /// <summary>
    /// 绘制虚线管道。
    /// </summary>
    private void DrawDashedPipe(Graphics g, int halfWidth, Pen borderPen)
    {
        borderPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;

        if ((_direction & PipeDirection.Left) != 0)
            g.DrawLine(borderPen, 0, ClientSize.Height / 2f, ClientSize.Width / 2, ClientSize.Height / 2f);

        if ((_direction & PipeDirection.Right) != 0)
            g.DrawLine(borderPen, ClientSize.Width / 2, ClientSize.Height / 2f, ClientSize.Width, ClientSize.Height / 2f);

        if ((_direction & PipeDirection.Top) != 0)
            g.DrawLine(borderPen, ClientSize.Width / 2f, 0, ClientSize.Width / 2f, ClientSize.Height / 2);

        if ((_direction & PipeDirection.Bottom) != 0)
            g.DrawLine(borderPen, ClientSize.Width / 2f, ClientSize.Height / 2, ClientSize.Width / 2f, ClientSize.Height);
    }

    /// <summary>
    /// 绘制双管道。
    /// </summary>
    private void DrawDoublePipe(Graphics g, int halfWidth, Brush pipeBrush, Pen borderPen)
    {
        var offset = _pipeWidth / 4;

        using var pen1 = new Pen(_pipeColor, 2);
        using var brush1 = new SolidBrush(Color.FromArgb(100, _pipeFillColor));

        // 上/左管道
        if (_isHorizontal)
        {
            g.FillRectangle(brush1, 0, halfWidth - offset, ClientSize.Width, _pipeWidth / 2);
            g.DrawRectangle(pen1, 0, halfWidth - offset, ClientSize.Width, _pipeWidth / 2);

            using var brush2 = new SolidBrush(Color.FromArgb(150, _pipeFillColor));
            g.FillRectangle(brush2, 0, halfWidth + offset / 2, ClientSize.Width, _pipeWidth / 2);
            g.DrawRectangle(pen1, 0, halfWidth + offset / 2, ClientSize.Width, _pipeWidth / 2);
        }
        else
        {
            g.FillRectangle(brush1, halfWidth - offset, 0, _pipeWidth / 2, ClientSize.Height);
            g.DrawRectangle(pen1, halfWidth - offset, 0, _pipeWidth / 2, ClientSize.Height);

            using var brush2 = new SolidBrush(Color.FromArgb(150, _pipeFillColor));
            g.FillRectangle(brush2, halfWidth + offset / 2, 0, _pipeWidth / 2, ClientSize.Height);
            g.DrawRectangle(pen1, halfWidth + offset / 2, 0, _pipeWidth / 2, ClientSize.Height);
        }
    }

    /// <summary>
    /// 绘制中心连接。
    /// </summary>
    private void DrawCenterConnection(Graphics g, int halfWidth, Brush pipeBrush, Pen borderPen)
    {
        var centerX = ClientSize.Width / 2f;
        var centerY = ClientSize.Height / 2f;

        // 根据连接方向绘制中心
        if (_isHorizontal)
        {
            if ((_direction & (PipeDirection.Top | PipeDirection.Bottom)) != 0)
            {
                // 有垂直连接
                var rect = new RectangleF(centerX - halfWidth, centerY - halfWidth, _pipeWidth, _pipeWidth);
                g.FillRectangle(pipeBrush, rect);
                g.DrawRectangle(borderPen, rect.X, rect.Y, rect.Width, rect.Height);
            }
        }
        else
        {
            if ((_direction & (PipeDirection.Left | PipeDirection.Right)) != 0)
            {
                var rect = new RectangleF(centerX - halfWidth, centerY - halfWidth, _pipeWidth, _pipeWidth);
                g.FillRectangle(pipeBrush, rect);
                g.DrawRectangle(borderPen, rect.X, rect.Y, rect.Width, rect.Height);
            }
        }
    }

    /// <summary>
    /// 绘制阀门。
    /// </summary>
    private void DrawValve(Graphics g)
    {
        var centerX = ClientSize.Width / 2f;
        var centerY = ClientSize.Height / 2f;
        var valveSize = _pipeWidth * 1.5f;

        // 阀门主体
        var valveColor = _valveOpen ? Color.FromArgb(50, 180, 50) : Color.FromArgb(180, 180, 180);

        using var valveBrush = new SolidBrush(valveColor);
        using var borderPen = new Pen(Color.FromArgb(60, 60, 70), 2);

        // 十字形阀门
        g.FillRectangle(valveBrush, centerX - valveSize / 4, centerY - valveSize / 2, valveSize / 2, valveSize);
        g.FillRectangle(valveBrush, centerX - valveSize / 2, centerY - valveSize / 4, valveSize, valveSize / 2);

        g.DrawRectangle(borderPen, centerX - valveSize / 4, centerY - valveSize / 2, valveSize / 2, valveSize);
        g.DrawRectangle(borderPen, centerX - valveSize / 2, centerY - valveSize / 4, valveSize, valveSize / 2);

        // 手轮
        var wheelRadius = valveSize / 2.5f;
        using var wheelBrush = new SolidBrush(Color.FromArgb(80, 80, 90));
        g.FillEllipse(wheelBrush, centerX - wheelRadius, centerY - wheelRadius, wheelRadius * 2, wheelRadius * 2);
        g.DrawEllipse(borderPen, centerX - wheelRadius, centerY - wheelRadius, wheelRadius * 2, wheelRadius * 2);
    }

    /// <summary>
    /// 绘制泵。
    /// </summary>
    private void DrawPump(Graphics g)
    {
        var centerX = ClientSize.Width / 2f;
        var centerY = ClientSize.Height / 2f;
        var pumpSize = _pipeWidth * 2f;

        var pumpColor = _pumpRunning ? Color.FromArgb(50, 180, 50) : Color.FromArgb(180, 180, 180);

        using var pumpBrush = new SolidBrush(pumpColor);
        using var borderPen = new Pen(Color.FromArgb(60, 60, 70), 2);

        // 泵体（圆形）
        g.FillEllipse(pumpBrush, centerX - pumpSize / 2, centerY - pumpSize / 2, pumpSize, pumpSize);
        g.DrawEllipse(borderPen, centerX - pumpSize / 2, centerY - pumpSize / 2, pumpSize, pumpSize);

        // 三角形指示
        var triSize = pumpSize * 0.3f;
        var rotation = _pumpRunning && _flowDirection == FlowDir.Forward ? 0 : 180;

        var gstate = g.Save();
        g.TranslateTransform(centerX, centerY);
        g.RotateTransform(rotation);

        using var arrowBrush = new SolidBrush(Color.White);
        var arrowPath = new GraphicsPath();
        arrowPath.AddLine(0, -triSize / 2, triSize / 2, triSize / 2);
        arrowPath.AddLine(-triSize / 2, triSize / 2, 0, 0);
        arrowPath.AddLine(0, 0, -triSize / 2, triSize / 2);
        arrowPath.CloseFigure();
        g.FillPath(arrowBrush, arrowPath);

        g.Restore(gstate!);
    }

    /// <summary>
    /// 绘制流向箭头。
    /// </summary>
    private void DrawFlowArrow(Graphics g)
    {
        var centerX = ClientSize.Width / 2f;
        var centerY = ClientSize.Height / 2f;
        var arrowSize = _pipeWidth * 0.6f;

        using var arrowBrush = new SolidBrush(_flowColor);

        if (_flowDirection == FlowDir.Forward)
        {
            var arrowPath = new GraphicsPath();
            arrowPath.AddLine(new PointF(centerX + arrowSize / 2, centerY), new PointF(centerX - arrowSize / 2, centerY - arrowSize / 2));
            arrowPath.AddLine(new PointF(centerX - arrowSize / 2, centerY - arrowSize / 2), new PointF(centerX - arrowSize / 2, centerY + arrowSize / 2));
            arrowPath.CloseFigure();
            g.FillPath(arrowBrush, arrowPath);
        }
        else
        {
            var arrowPath = new GraphicsPath();
            arrowPath.AddLine(new PointF(centerX - arrowSize / 2, centerY), new PointF(centerX + arrowSize / 2, centerY - arrowSize / 2));
            arrowPath.AddLine(new PointF(centerX + arrowSize / 2, centerY - arrowSize / 2), new PointF(centerX + arrowSize / 2, centerY + arrowSize / 2));
            arrowPath.CloseFigure();
            g.FillPath(arrowBrush, arrowPath);
        }
    }

    #endregion

    #region 控件行为

    /// <inheritdoc />
    protected override Size DefaultSize => new Size(150, 60);

    #endregion
}
