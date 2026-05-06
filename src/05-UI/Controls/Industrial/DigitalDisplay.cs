using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Acme.Industrial.UI.Controls.Industrial;

/// <summary>
/// 数显控件的显示模式。
/// </summary>
public enum DigitalDisplayMode
{
    /// <summary>七段数码管。</summary>
    SevenSegment,

    /// <summary>十四段数码管（支持字母）。</summary>
    FourteenSegment,

    /// <summary>点阵显示。</summary>
    DotMatrix
}

/// <summary>
/// 数显控件的对齐方式。
/// </summary>
public enum DigitalAlignment
{
    /// <summary>左对齐。</summary>
    Left,

    /// <summary>右对齐。</summary>
    Right,

    /// <summary>居中对齐。</summary>
    Center
}

/// <summary>
/// 工业数显控件。
/// 支持多种显示模式，可显示数字、字母和符号。
/// </summary>
[DefaultProperty(nameof(Value))]
[DefaultBindingProperty(nameof(Value))]
public class DigitalDisplay : Control
{
    #region 属性

    private string _value = "0";
    private string _format = "F1";
    private DigitalDisplayMode _displayMode = DigitalDisplayMode.SevenSegment;
    private DigitalAlignment _alignment = DigitalAlignment.Right;
    private int _digitCount = 6;
    private int _decimalPlaces = 1;
    private Color _digitColor = Color.FromArgb(0, 255, 100);
    private Color _offColor = Color.FromArgb(30, 30, 30);
    private Color _backgroundColor = Color.FromArgb(20, 20, 20);
    private bool _showColon = false;
    private bool _showDecimalPoint = true;
    private bool _showLeadingZeros = false;

    /// <summary>
    /// 获取或设置显示值。
    /// </summary>
    [Category("数据")]
    [Description("显示的数值")]
    [DefaultValue("0")]
    public string Value
    {
        get => _value;
        set
        {
            var newValue = value ?? "0";
            if (_value != newValue)
            {
                _value = newValue;
                Invalidate();
                OnValueChanged(EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// 获取或设置数值。
    /// </summary>
    [Category("数据")]
    [Description("显示的数值")]
    [DefaultValue(0.0)]
    public double NumericValue
    {
        get => double.TryParse(_value, out var result) ? result : 0;
        set => Value = value.ToString(_format);
    }

    /// <summary>
    /// 获取或设置格式化字符串。
    /// </summary>
    [Category("数据")]
    [Description("数值格式化字符串，如 F1 表示保留1位小数")]
    [DefaultValue("F1")]
    public string Format
    {
        get => _format;
        set
        {
            if (_format != value)
            {
                _format = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置显示模式。
    /// </summary>
    [Category("外观")]
    [Description("显示模式：SevenSegment-七段，FourteenSegment-十四段，DotMatrix-点阵")]
    [DefaultValue(DigitalDisplayMode.SevenSegment)]
    public DigitalDisplayMode DisplayMode
    {
        get => _displayMode;
        set
        {
            if (_displayMode != value)
            {
                _displayMode = value;
                Invalidate();
                OnDisplayModeChanged(EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// 获取或设置对齐方式。
    /// </summary>
    [Category("布局")]
    [Description("数字对齐方式")]
    [DefaultValue(DigitalAlignment.Right)]
    public DigitalAlignment Alignment
    {
        get => _alignment;
        set
        {
            if (_alignment != value)
            {
                _alignment = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置数字位数。
    /// </summary>
    [Category("外观")]
    [Description("显示的位数")]
    [DefaultValue(6)]
    public int DigitCount
    {
        get => _digitCount;
        set
        {
            if (value < 1) value = 1;
            if (value > 20) value = 20;
            if (_digitCount != value)
            {
                _digitCount = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置小数位数。
    /// </summary>
    [Category("外观")]
    [Description("小数位数")]
    [DefaultValue(1)]
    public int DecimalPlaces
    {
        get => _decimalPlaces;
        set
        {
            if (value < 0) value = 0;
            if (value > _digitCount - 1) value = _digitCount - 1;
            if (_decimalPlaces != value)
            {
                _decimalPlaces = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置数字颜色。
    /// </summary>
    [Category("外观")]
    [Description("数字颜色")]
    [DefaultValue(typeof(Color), "0, 255, 100")]
    public Color DigitColor
    {
        get => _digitColor;
        set
        {
            if (_digitColor != value)
            {
                _digitColor = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置未点亮段颜色。
    /// </summary>
    [Category("外观")]
    [Description("未点亮段的颜色")]
    [DefaultValue(typeof(Color), "30, 30, 30")]
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
    /// 获取或设置背景颜色。
    /// </summary>
    [Category("外观")]
    [Description("背景颜色")]
    [DefaultValue(typeof(Color), "20, 20, 20")]
    public Color BackgroundColor2
    {
        get => _backgroundColor;
        set
        {
            if (_backgroundColor != value)
            {
                _backgroundColor = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置是否显示冒号（用于时钟）。
    /// </summary>
    [Category("外观")]
    [Description("是否显示冒号分隔符")]
    [DefaultValue(false)]
    public bool ShowColon
    {
        get => _showColon;
        set
        {
            if (_showColon != value)
            {
                _showColon = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置是否显示小数点。
    /// </summary>
    [Category("外观")]
    [Description("是否显示小数点")]
    [DefaultValue(true)]
    public bool ShowDecimalPoint
    {
        get => _showDecimalPoint;
        set
        {
            if (_showDecimalPoint != value)
            {
                _showDecimalPoint = value;
                Invalidate();
            }
        }
    }

    /// <summary>
    /// 获取或设置是否显示前导零。
    /// </summary>
    [Category("外观")]
    [Description("是否显示前导零")]
    [DefaultValue(false)]
    public bool ShowLeadingZeros
    {
        get => _showLeadingZeros;
        set
        {
            if (_showLeadingZeros != value)
            {
                _showLeadingZeros = value;
                Invalidate();
            }
        }
    }

    #endregion

    #region 事件

    /// <summary>
    /// 值变更时触发。
    /// </summary>
    public event EventHandler? ValueChanged;

    /// <summary>
    /// 显示模式变更时触发。
    /// </summary>
    public event EventHandler? DisplayModeChanged;

    /// <summary>
    /// 触发值变更事件。
    /// </summary>
    protected virtual void OnValueChanged(EventArgs e)
    {
        ValueChanged?.Invoke(this, e);
    }

    /// <summary>
    /// 触发显示模式变更事件。
    /// </summary>
    protected virtual void OnDisplayModeChanged(EventArgs e)
    {
        DisplayModeChanged?.Invoke(this, e);
    }

    #endregion

    #region 七段码定义

    // 七段码位定义: a,b,c,d,e,f,g
    //    a
    //  f   b
    //    g
    //  e   c
    //    d

    private static readonly Dictionary<char, bool[]> SevenSegmentMap = new()
    {
        ['0'] = new[] { true, true, true, true, true, true, false },  // ABCDEF
        ['1'] = new[] { false, true, true, false, false, false, false }, // BC
        ['2'] = new[] { true, true, false, true, true, false, true },   // ABGED
        ['3'] = new[] { true, true, true, true, false, false, true },    // ABCD G
        ['4'] = new[] { false, true, true, false, false, true, true },  // BCFG
        ['5'] = new[] { true, false, true, true, false, true, true },   // A CD F G
        ['6'] = new[] { true, false, true, true, true, true, true },    // A CDEFG
        ['7'] = new[] { true, true, true, false, false, false, false },  // ABC
        ['8'] = new[] { true, true, true, true, true, true, true },     // ABCDEFG
        ['9'] = new[] { true, true, true, true, false, true, true },    // ABCDFG
        ['A'] = new[] { true, true, true, false, true, true, true },    // ABCEFG
        ['B'] = new[] { false, false, true, true, true, true, true },    //  CDEFG
        ['C'] = new[] { true, false, false, true, true, true, false },  // A  EF
        ['D'] = new[] { false, true, true, true, true, false, true },    // BCDEG
        ['E'] = new[] { true, false, false, true, true, true, true },    // A  EFG
        ['F'] = new[] { true, false, false, false, true, true, true },    // A  EFG
        ['G'] = new[] { true, false, true, true, true, true, false },   // A CD EF
        ['H'] = new[] { false, true, true, false, true, true, true },    // BC EF G
        ['I'] = new[] { false, false, false, true, false, false, false }, //   D
        ['J'] = new[] { false, true, true, true, true, false, false },   //  BCDE
        ['K'] = new[] { false, false, false, false, true, true, true },    //   EFG
        ['L'] = new[] { false, false, false, true, true, true, false },  //   DEF
        ['M'] = new[] { false, true, true, false, true, true, false },  // BC EF
        ['N'] = new[] { false, false, true, false, true, false, true },  //  CE G
        ['O'] = new[] { true, true, true, true, true, true, false },   // ABCDEF
        ['P'] = new[] { true, true, false, false, true, true, true },   // AB EFG
        ['Q'] = new[] { true, true, true, false, false, true, true },  // ABCD  G
        ['R'] = new[] { true, true, false, false, true, true, true },   // AB EFGR
        ['S'] = new[] { true, false, true, true, false, true, true },  // A CD F G
        ['T'] = new[] { false, false, false, true, true, false, true }, //   DE G
        ['U'] = new[] { false, true, true, true, true, true, false },  //  BCDEF
        ['V'] = new[] { false, false, false, false, true, false, true }, //   E G
        ['W'] = new[] { false, false, false, false, true, false, true }, //   E G
        ['X'] = new[] { false, false, false, false, false, false, true }, //     G
        ['Y'] = new[] { false, true, true, true, false, true, true },  // BC F G
        ['Z'] = new[] { true, true, false, true, true, false, true },  // ABGED
        ['-'] = new[] { false, false, false, false, false, false, true }, //     G
        ['.'] = new[] { false, false, false, false, true, false, false }, //      E
        [':'] = new[] { false, false, false, false, false, false, false }, // 特殊处理
        [' '] = new[] { false, false, false, false, false, false, false },
    };

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化数显控件。
    /// </summary>
    public DigitalDisplay()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        Size = new Size(200, 60);
        BackColor = _backgroundColor;
    }

    #endregion

    #region 方法

    /// <summary>
    /// 显示整数值。
    /// </summary>
    public void ShowInteger(int value)
    {
        Value = value.ToString();
    }

    /// <summary>
    /// 显示浮点值。
    /// </summary>
    public void ShowFloat(double value, int decimals = -1)
    {
        var format = decimals >= 0 ? $"F{decimals}" : _format;
        Value = value.ToString(format);
    }

    /// <summary>
    /// 清空显示。
    /// </summary>
    public void Clear()
    {
        Value = new string(' ', _digitCount);
    }

    /// <summary>
    /// 显示错误状态（显示 "Err"）。
    /// </summary>
    public void ShowError()
    {
        Value = "Err";
    }

    /// <summary>
    /// 显示超限状态（显示 "----"）。
    /// </summary>
    public void ShowOverflow()
    {
        Value = new string('-', _digitCount);
    }

    /// <summary>
    /// 绑定到数值。
    /// </summary>
    public void BindToValue(double value)
    {
        NumericValue = value;
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
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // 绘制背景
            using var bgBrush = new SolidBrush(_backgroundColor);
            g.FillRectangle(bgBrush, ClientRectangle);

            // 尺寸检查，防止计算错误
            if (ClientSize.Width < 20 || ClientSize.Height < 10)
                return;

            // 计算每个数字的宽度
            var colonWidth = _showColon ? 10 : 0;
            var totalDigits = _digitCount + (_showColon ? 1 : 0);
            var digitWidth = (ClientSize.Width - colonWidth) / totalDigits;

            // 确保 segmentWidth 不会太小
            var segmentWidth = Math.Max(1, digitWidth * 0.15f);
            var segmentHeight = Math.Max(1, ClientSize.Height * 0.6f);
            var digitSpacing = digitWidth;

            // 格式化显示值
            var displayValue = FormatDisplayValue();
            var startX = GetStartX(displayValue, digitWidth, colonWidth);

            var x = startX;
            var digitIndex = 0;

            foreach (var c in displayValue)
            {
                if (c == ':' && _showColon)
                {
                    DrawColon(g, x + digitWidth / 2, ClientSize.Height / 2, segmentWidth * 2);
                    x += digitSpacing;
                    continue;
                }

                if (digitIndex >= _digitCount)
                    break;

                if (_displayMode == DigitalDisplayMode.SevenSegment)
                {
                    DrawSevenSegment(g, c, x, ClientSize.Height, digitWidth, segmentHeight, segmentWidth);
                }
                else
                {
                    DrawSevenSegment(g, c, x, ClientSize.Height, digitWidth, segmentHeight, segmentWidth);
                }

                // 绘制小数点
                if (_showDecimalPoint && digitIndex < displayValue.Length)
                {
                    var nextIndex = digitIndex + 1;
                    if (nextIndex < displayValue.Length && displayValue[nextIndex] == '.')
                    {
                        DrawDecimalPoint(g, x + digitWidth - segmentWidth, ClientSize.Height, segmentWidth);
                    }
                }

                x += digitSpacing;
                digitIndex++;
            }

            // 绘制边框
            using var borderPen = new Pen(Color.FromArgb(60, 60, 60), 2);
            g.DrawRectangle(borderPen, 1, 1, ClientSize.Width - 2, ClientSize.Height - 2);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DigitalDisplay OnPaint error: {ex.Message}");
        }
    }

    /// <summary>
    /// 格式化显示值。
    /// </summary>
    private string FormatDisplayValue()
    {
        var result = _value;

        if (!_showLeadingZeros && double.TryParse(_value, out var num))
        {
            result = num.ToString(_format);
        }

        // 限制长度
        if (result.Length > _digitCount)
        {
            result = result.Substring(0, _digitCount);
        }

        return result;
    }

    /// <summary>
    /// 获取起始X坐标。
    /// </summary>
    private int GetStartX(string displayValue, int digitWidth, int colonWidth)
    {
        var result = _alignment switch
        {
            DigitalAlignment.Right => ClientSize.Width - displayValue.Length * digitWidth - colonWidth,
            DigitalAlignment.Center => (ClientSize.Width - displayValue.Length * digitWidth - colonWidth) / 2,
            _ => 5
        };
        return Math.Max(0, result);
    }

    /// <summary>
    /// 绘制七段码。
    /// </summary>
    private void DrawSevenSegment(Graphics g, char c, float x, int height, float digitWidth, float segmentHeight, float segmentWidth)
    {
        var centerX = x + digitWidth / 2;
        var centerY = height / 2;
        var halfHeight = segmentHeight / 2;
        var halfWidth = Math.Max(1, digitWidth / 2 - segmentWidth);

        // 获取段状态
        if (!SevenSegmentMap.TryGetValue(char.ToUpper(c), out var segments))
        {
            segments = SevenSegmentMap[' '];
        }

        using var onBrush = new SolidBrush(_digitColor);
        using var offBrush = new SolidBrush(_offColor);

        // a: 顶部横段
        DrawHorizontalSegment(g, centerX - halfWidth, centerY - halfHeight - segmentWidth, 2 * halfWidth, segmentWidth,
            segments.Length > 0 && segments[0], onBrush, offBrush);

        // b: 右上竖段
        DrawVerticalSegment(g, centerX + halfWidth, centerY - halfHeight, segmentWidth, halfHeight,
            segments.Length > 1 && segments[1], onBrush, offBrush);

        // c: 右下竖段
        DrawVerticalSegment(g, centerX + halfWidth, centerY + segmentWidth, segmentWidth, halfHeight,
            segments.Length > 2 && segments[2], onBrush, offBrush);

        // d: 底部横段
        DrawHorizontalSegment(g, centerX - halfWidth, centerY + halfHeight, 2 * halfWidth, segmentWidth,
            segments.Length > 3 && segments[3], onBrush, offBrush);

        // e: 左下竖段
        DrawVerticalSegment(g, centerX - halfWidth - segmentWidth, centerY + segmentWidth, segmentWidth, halfHeight,
            segments.Length > 4 && segments[4], onBrush, offBrush);

        // f: 左上竖段
        DrawVerticalSegment(g, centerX - halfWidth - segmentWidth, centerY - halfHeight, segmentWidth, halfHeight,
            segments.Length > 5 && segments[5], onBrush, offBrush);

        // g: 中间横段
        DrawHorizontalSegment(g, centerX - halfWidth, centerY - segmentWidth / 2, 2 * halfWidth, segmentWidth,
            segments.Length > 6 && segments[6], onBrush, offBrush);
    }

    /// <summary>
    /// 绘制水平段。
    /// </summary>
    private void DrawHorizontalSegment(Graphics g, float x, float y, float width, float height, bool isOn, SolidBrush onBrush, SolidBrush offBrush)
    {
        if (width <= 0 || height <= 0) return;
        if (float.IsNaN(x) || float.IsNaN(y) || float.IsNaN(width) || float.IsNaN(height)) return;
        if (float.IsInfinity(x) || float.IsInfinity(y) || float.IsInfinity(width) || float.IsInfinity(height)) return;
        if (!isOn)
        {
            g.FillRectangle(offBrush, x, y, width, height);
        }
        else
        {
            g.FillRectangle(onBrush, x, y, width, height);
        }
    }

    /// <summary>
    /// 绘制垂直段。
    /// </summary>
    private void DrawVerticalSegment(Graphics g, float x, float y, float width, float height, bool isOn, SolidBrush onBrush, SolidBrush offBrush)
    {
        if (width <= 0 || height <= 0) return;
        if (float.IsNaN(x) || float.IsNaN(y) || float.IsNaN(width) || float.IsNaN(height)) return;
        if (float.IsInfinity(x) || float.IsInfinity(y) || float.IsInfinity(width) || float.IsInfinity(height)) return;
        if (!isOn)
        {
            g.FillRectangle(offBrush, x, y, width, height);
        }
        else
        {
            g.FillRectangle(onBrush, x, y, width, height);
        }
    }

    /// <summary>
    /// 绘制冒号。
    /// </summary>
    private void DrawColon(Graphics g, float x, float y, float size)
    {
        using var brush = new SolidBrush(_digitColor);
        g.FillEllipse(brush, x - size / 4, y - size / 3, size / 2, size / 3);
        g.FillEllipse(brush, x - size / 4, y + size / 6, size / 2, size / 3);
    }

    /// <summary>
    /// 绘制小数点。
    /// </summary>
    private void DrawDecimalPoint(Graphics g, float x, float height, float size)
    {
        using var brush = new SolidBrush(_digitColor);
        g.FillEllipse(brush, x, height - size * 1.5f, size, size);
    }

    #endregion

    #region 控件行为

    /// <inheritdoc />
    protected override Size DefaultSize => new Size(200, 60);

    /// <inheritdoc />
    protected override void OnBackColorChanged(EventArgs e)
    {
        base.OnBackColorChanged(e);
        _backgroundColor = BackColor;
    }

    #endregion
}
