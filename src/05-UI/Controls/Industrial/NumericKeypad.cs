using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Acme.Industrial.UI.Controls.Industrial;

/// <summary>
/// 数字键盘输入模式。
/// </summary>
public enum KeypadMode
{
    /// <summary>整数模式。</summary>
    Integer,

    /// <summary>小数模式。</summary>
    Decimal,

    /// <summary>带上下限的小数模式。</summary>
    Bounded
}

/// <summary>
/// 数字键盘控件。
/// 用于输入数值，支持多种模式和输入验证。
/// </summary>
public class NumericKeypad : Control
{
    #region 属性

    private string _value = "0";
    private string _displayValue = "0";
    private KeypadMode _mode = KeypadMode.Decimal;
    private double _minimum = 0;
    private double _maximum = 100;
    private int _decimalPlaces = 2;
    private bool _allowNegative = false;
    private bool _showMode = true;
    private Color _buttonColor = Color.FromArgb(60, 60, 65);
    private Color _buttonHoverColor = Color.FromArgb(80, 80, 85);
    private Color _buttonPressedColor = Color.FromArgb(45, 45, 50);
    private Color _displayColor = Color.FromArgb(30, 30, 35);
    private Color _displayTextColor = Color.Lime;
    private Font _displayFont = new Font("Consolas", 24, FontStyle.Bold);
    private Font _buttonFont = new Font("Arial", 14, FontStyle.Bold);
    private int _buttonSpacing = 5;
    private int _buttonSize = 60;

    /// <summary>
    /// 当前输入值。
    /// </summary>
    [Category("数据")]
    [Description("当前输入的数值")]
    public string Value
    {
        get => _value;
        set
        {
            _value = value ?? "0";
            _displayValue = _value;
            Invalidate();
            OnValueChanged(EventArgs.Empty);
        }
    }

    /// <summary>
    /// 数值（双精度）。
    /// </summary>
    [Category("数据")]
    [Description("当前输入的数值")]
    public double NumericValue
    {
        get => double.TryParse(_value, out var result) ? result : 0;
        set => Value = value.ToString($"F{_decimalPlaces}");
    }

    /// <summary>
    /// 输入模式。
    /// </summary>
    [Category("行为")]
    [Description("键盘输入模式")]
    [DefaultValue(KeypadMode.Decimal)]
    public KeypadMode Mode
    {
        get => _mode;
        set { _mode = value; UpdateDisplay(); Invalidate(); }
    }

    /// <summary>
    /// 最小值（Bounded模式）。
    /// </summary>
    [Category("数据")]
    [DefaultValue(0.0)]
    public double Minimum
    {
        get => _minimum;
        set { _minimum = value; }
    }

    /// <summary>
    /// 最大值（Bounded模式）。
    /// </summary>
    [Category("数据")]
    [DefaultValue(100.0)]
    public double Maximum
    {
        get => _maximum;
        set { _maximum = value; }
    }

    /// <summary>
    /// 小数位数。
    /// </summary>
    [Category("外观")]
    [DefaultValue(2)]
    public int DecimalPlaces
    {
        get => _decimalPlaces;
        set { _decimalPlaces = Math.Max(0, value); }
    }

    /// <summary>
    /// 是否允许负数。
    /// </summary>
    [Category("行为")]
    [DefaultValue(false)]
    public bool AllowNegative
    {
        get => _allowNegative;
        set { _allowNegative = value; }
    }

    /// <summary>
    /// 显示当前模式。
    /// </summary>
    [Category("行为")]
    [DefaultValue(true)]
    public bool ShowMode
    {
        get => _showMode;
        set { _showMode = value; Invalidate(); }
    }

    /// <summary>
    /// 按钮颜色。
    /// </summary>
    [Category("外观")]
    public Color ButtonColor
    {
        get => _buttonColor;
        set { _buttonColor = value; Invalidate(); }
    }

    /// <summary>
    /// 显示文本颜色。
    /// </summary>
    [Category("外观")]
    public Color DisplayTextColor
    {
        get => _displayTextColor;
        set { _displayTextColor = value; Invalidate(); }
    }

    #endregion

    #region 事件

    /// <summary>
    /// 值变更时触发。
    /// </summary>
    public event EventHandler? ValueChanged;

    /// <summary>
    /// 确认时触发。
    /// </summary>
    public event EventHandler? Confirmed;

    /// <summary>
    /// 取消时触发。
    /// </summary>
    public event EventHandler? Cancelled;

    /// <summary>
    /// 触发值变更事件。
    /// </summary>
    protected virtual void OnValueChanged(EventArgs e)
    {
        ValueChanged?.Invoke(this, e);
    }

    #endregion

    #region 字段

    private ButtonState[,] _buttonStates = new ButtonState[5, 4];
    private Point _hoverButton = new Point(-1, -1);
    private Point _pressedButton = new Point(-1, -1);

    private enum ButtonState
    {
        None,
        Number,
        Decimal,
        Negative,
        Backspace,
        Clear,
        Enter,
        Cancel
    }

    private static readonly string[,] ButtonLabels = {
        { "7", "8", "9", "CLR" },
        { "4", "5", "6", "←" },
        { "1", "2", "3", "ENT" },
        { "0", "00", ".", "±" },
        { "C", "", "", "ESC" }
    };

    private static readonly ButtonState[,] ButtonTypes = {
        { ButtonState.Number, ButtonState.Number, ButtonState.Number, ButtonState.Clear },
        { ButtonState.Number, ButtonState.Number, ButtonState.Number, ButtonState.Backspace },
        { ButtonState.Number, ButtonState.Number, ButtonState.Number, ButtonState.Enter },
        { ButtonState.Number, ButtonState.Number, ButtonState.Decimal, ButtonState.Negative },
        { ButtonState.Clear, ButtonState.None, ButtonState.None, ButtonState.Cancel }
    };

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化数字键盘。
    /// </summary>
    public NumericKeypad()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        Size = new Size(280, 380);
        BackColor = Color.FromArgb(45, 45, 50);
        ForeColor = Color.White;
        Resize += NumericKeypad_Resize;
    }

    #endregion

    #region 方法

    /// <summary>
    /// 设置值。
    /// </summary>
    public void SetValue(double value)
    {
        Value = value.ToString($"F{_decimalPlaces}");
    }

    /// <summary>
    /// 清空输入。
    /// </summary>
    public void Clear()
    {
        _value = "0";
        _displayValue = "0";
        Invalidate();
    }

    /// <summary>
    /// 退格。
    /// </summary>
    public void Backspace()
    {
        if (_value.Length > 1)
        {
            _value = _value.Substring(0, _value.Length - 1);
        }
        else
        {
            _value = "0";
        }
        UpdateDisplay();
        Invalidate();
    }

    /// <summary>
    /// 取反。
    /// </summary>
    public void ToggleNegative()
    {
        if (_value.StartsWith("-"))
        {
            _value = _value.Substring(1);
        }
        else if (_allowNegative)
        {
            _value = "-" + _value;
        }
        UpdateDisplay();
        Invalidate();
    }

    private void UpdateDisplay()
    {
        if (_value == "0" || string.IsNullOrEmpty(_value))
        {
            _displayValue = "0";
        }
        else
        {
            _displayValue = _value;
        }
    }

    private void HandleNumber(string num)
    {
        if (_value == "0" && num != "00")
        {
            _value = num;
        }
        else
        {
            _value += num;
        }

        // 检查上限
        if (_mode == KeypadMode.Bounded && double.TryParse(_value, out var val))
        {
            if (val > _maximum)
            {
                _value = _maximum.ToString($"F{_decimalPlaces}");
            }
        }

        UpdateDisplay();
        Invalidate();
    }

    private void HandleDecimal()
    {
        if (!_value.Contains("."))
        {
            _value += ".";
        }
        UpdateDisplay();
        Invalidate();
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

            // 绘制显示区
            DrawDisplay(g);

            // 绘制按钮
            DrawButtons(g);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"NumericKeypad OnPaint error: {ex.Message}");
        }
    }

    private void DrawDisplay(Graphics g)
    {
        var displayHeight = _showMode ? 100 : 70;

        // 显示区背景
        using var displayBrush = new SolidBrush(_displayColor);
        g.FillRectangle(displayBrush, 0, 0, ClientSize.Width, displayHeight);

        // 模式标签
        if (_showMode)
        {
            using var modeFont = new Font("微软雅黑", 8);
            using var modeBrush = new SolidBrush(Color.FromArgb(120, 120, 120));
            var modeText = _mode switch
            {
                KeypadMode.Integer => "整数",
                KeypadMode.Decimal => "小数",
                KeypadMode.Bounded => string.Format("{0:F" + _decimalPlaces + "} ~ {1:F" + _decimalPlaces + "}", _minimum, _maximum),
                _ => ""
            };
            g.DrawString(modeText, modeFont, modeBrush, 10, 8);
        }

        // 当前值
        using var valueFont = _displayFont;
        using var valueBrush = new SolidBrush(_displayTextColor);
        var valueText = _displayValue;
        var valueSize = g.MeasureString(valueText, valueFont);

        // 限制显示宽度
        var maxWidth = ClientSize.Width - 20;
        while (valueSize.Width > maxWidth && valueText.Length > 3)
        {
            valueText = valueText.Substring(0, valueText.Length - 1);
            valueSize = g.MeasureString(valueText + "...", valueFont);
        }

        if (valueText != _displayValue)
        {
            valueText += "...";
        }

        g.DrawString(valueText, valueFont, valueBrush, ClientSize.Width - valueSize.Width - 10, displayHeight - valueSize.Height - 10);

        // 底部分隔线
        using var linePen = new Pen(Color.FromArgb(80, 80, 80), 2);
        g.DrawLine(linePen, 0, displayHeight, ClientSize.Width, displayHeight);
    }

    private void DrawButtons(Graphics g)
    {
        var displayHeight = _showMode ? 100 : 70;
        var buttonAreaTop = displayHeight + 10;
        var buttonAreaHeight = ClientSize.Height - buttonAreaTop - 10;
        var buttonAreaWidth = ClientSize.Width - 20;

        var cols = 4;
        var rows = 5;
        var spacing = _buttonSpacing;
        var buttonWidth = (buttonAreaWidth - spacing * (cols - 1)) / cols;
        var buttonHeight = (buttonAreaHeight - spacing * (rows - 1)) / rows;

        for (var row = 0; row < rows; row++)
        {
            for (var col = 0; col < cols; col++)
            {
                var buttonType = ButtonTypes[row, col];
                if (buttonType == ButtonState.None) continue;

                var x = 10 + col * (buttonWidth + spacing);
                var y = buttonAreaTop + row * (buttonHeight + spacing);
                var rect = new Rectangle(x, y, buttonWidth, buttonHeight);

                var isHover = _hoverButton.X == col && _hoverButton.Y == row;
                var isPressed = _pressedButton.X == col && _pressedButton.Y == row;

                var color = _buttonColor;
                if (isPressed)
                    color = _buttonPressedColor;
                else if (isHover)
                    color = _buttonHoverColor;

                // 特殊按钮颜色
                if (buttonType == ButtonState.Enter)
                    color = Color.FromArgb(0, 150, 80);
                else if (buttonType == ButtonState.Cancel)
                    color = Color.FromArgb(180, 60, 60);
                else if (buttonType == ButtonState.Clear)
                    color = Color.FromArgb(180, 140, 50);

                DrawButton(g, rect, ButtonLabels[row, col], color, buttonType);
            }
        }
    }

    private void DrawButton(Graphics g, Rectangle rect, string text, Color color, ButtonState type)
    {
        // 按钮背景
        using var brush = new SolidBrush(color);

        // 圆角矩形
        var path = CreateRoundedRectPath(rect, 8);
        g.FillPath(brush, path);

        // 边框
        using var borderPen = new Pen(Color.FromArgb(80, 80, 80), 1);
        g.DrawPath(borderPen, path);

        // 文字
        if (!string.IsNullOrEmpty(text))
        {
            using var font = _buttonFont;
            using var textBrush = new SolidBrush(Color.White);

            // 特殊按钮
            if (type == ButtonState.Enter || type == ButtonState.Cancel || type == ButtonState.Clear)
            {
                using var whiteBrush = new SolidBrush(Color.White);
                try
                {
                    var size = g.MeasureString(text, font);
                    if (size.Width > 0 && size.Height > 0)
                    {
                        var x = rect.X + (rect.Width - size.Width) / 2;
                        var y = rect.Y + (rect.Height - size.Height) / 2;
                        g.DrawString(text, font, whiteBrush, x, y);
                    }
                }
                catch { }
                return;
            }

            try
            {
                var size2 = g.MeasureString(text, font);
                if (size2.Width > 0 && size2.Height > 0)
                {
                    var x2 = rect.X + (rect.Width - size2.Width) / 2;
                    var y2 = rect.Y + (rect.Height - size2.Height) / 2;
                    g.DrawString(text, font, textBrush, x2, y2);
                }
            }
            catch { }
        }
    }

    private static GraphicsPath CreateRoundedRectPath(Rectangle rect, int radius)
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

    private void NumericKeypad_Resize(object? sender, EventArgs e)
    {
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var (col, row) = GetButtonAt(e.X, e.Y);
        if (col != _hoverButton.X || row != _hoverButton.Y)
        {
            _hoverButton = new Point(col, row);
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverButton = new Point(-1, -1);
        _pressedButton = new Point(-1, -1);
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        var (col, row) = GetButtonAt(e.X, e.Y);
        if (col >= 0 && row >= 0)
        {
            _pressedButton = new Point(col, row);
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);

        var (col, row) = GetButtonAt(e.X, e.Y);
        if (col >= 0 && row >= 0 && col == _pressedButton.X && row == _pressedButton.Y)
        {
            HandleButtonClick(row, col);
        }

        _pressedButton = new Point(-1, -1);
        Invalidate();
    }

    private (int col, int row) GetButtonAt(int x, int y)
    {
        var displayHeight = _showMode ? 100 : 70;
        var buttonAreaTop = displayHeight + 10;
        var buttonAreaLeft = 10;
        var buttonAreaWidth = ClientSize.Width - 20;

        var cols = 4;
        var rows = 5;
        var spacing = _buttonSpacing;
        var buttonWidth = (buttonAreaWidth - spacing * (cols - 1)) / cols;
        var buttonHeight = (ClientSize.Height - buttonAreaTop - 10 - spacing * (rows - 1)) / rows;

        var col = (x - buttonAreaLeft) / (buttonWidth + spacing);
        var row = (y - buttonAreaTop) / (buttonHeight + spacing);

        if (col < 0 || col >= cols || row < 0 || row >= rows)
            return (-1, -1);

        var buttonType = ButtonTypes[row, col];
        if (buttonType == ButtonState.None)
            return (-1, -1);

        return (col, row);
    }

    private void HandleButtonClick(int row, int col)
    {
        var buttonType = ButtonTypes[row, col];

        switch (buttonType)
        {
            case ButtonState.Number:
                HandleNumber(ButtonLabels[row, col]);
                break;
            case ButtonState.Decimal:
                HandleDecimal();
                break;
            case ButtonState.Negative:
                ToggleNegative();
                break;
            case ButtonState.Backspace:
                Backspace();
                break;
            case ButtonState.Clear:
                Clear();
                break;
            case ButtonState.Enter:
                OnConfirmed();
                break;
            case ButtonState.Cancel:
                OnCancelled();
                break;
        }
    }

    private void OnConfirmed()
    {
        // 应用约束
        if (_mode == KeypadMode.Bounded && double.TryParse(_value, out var val))
        {
            val = Math.Max(_minimum, Math.Min(_maximum, val));
            _value = val.ToString($"F{_decimalPlaces}");
            UpdateDisplay();
        }

        Confirmed?.Invoke(this, EventArgs.Empty);
    }

    private void OnCancelled()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.D0:
            case Keys.NumPad0:
                HandleNumber("0");
                return true;
            case Keys.D1:
            case Keys.NumPad1:
                HandleNumber("1");
                return true;
            case Keys.D2:
            case Keys.NumPad2:
                HandleNumber("2");
                return true;
            case Keys.D3:
            case Keys.NumPad3:
                HandleNumber("3");
                return true;
            case Keys.D4:
            case Keys.NumPad4:
                HandleNumber("4");
                return true;
            case Keys.D5:
            case Keys.NumPad5:
                HandleNumber("5");
                return true;
            case Keys.D6:
            case Keys.NumPad6:
                HandleNumber("6");
                return true;
            case Keys.D7:
            case Keys.NumPad7:
                HandleNumber("7");
                return true;
            case Keys.D8:
            case Keys.NumPad8:
                HandleNumber("8");
                return true;
            case Keys.D9:
            case Keys.NumPad9:
                HandleNumber("9");
                return true;
            case Keys.OemPeriod:
            case Keys.Decimal:
                HandleDecimal();
                return true;
            case Keys.Back:
                Backspace();
                return true;
            case Keys.Escape:
                OnCancelled();
                return true;
            case Keys.Enter:
                OnConfirmed();
                return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override Size DefaultSize => new Size(280, 380);

    #endregion
}
