using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Acme.Industrial.UI.Themes;

/// <summary>
/// 主题类型。
/// </summary>
public enum ThemeType
{
    /// <summary>亮色主题。</summary>
    Light,

    /// <summary>暗色主题。</summary>
    Dark,

    /// <summary>蓝色主题。</summary>
    Blue,

    /// <summary>工业主题。</summary>
    Industrial
}

/// <summary>
/// 主题配置。
/// </summary>
public class ThemeConfig
{
    /// <summary>
    /// 主题名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 主题类型。
    /// </summary>
    public ThemeType Type { get; set; }

    /// <summary>
    /// 主色。
    /// </summary>
    public Color PrimaryColor { get; set; }

    /// <summary>
    /// 背景色。
    /// </summary>
    public Color BackgroundColor { get; set; }

    /// <summary>
    /// 前景色（文字）。
    /// </summary>
    public Color ForegroundColor { get; set; }

    /// <summary>
    /// 次要背景色。
    /// </summary>
    public Color SecondaryBackgroundColor { get; set; }

    /// <summary>
    /// 边框色。
    /// </summary>
    public Color BorderColor { get; set; }

    /// <summary>
    /// 悬停色。
    /// </summary>
    public Color HoverColor { get; set; }

    /// <summary>
    /// 选中色。
    /// </summary>
    public Color SelectedColor { get; set; }

    /// <summary>
    /// 错误色。
    /// </summary>
    public Color ErrorColor { get; set; }

    /// <summary>
    /// 警告色。
    /// </summary>
    public Color WarningColor { get; set; }

    /// <summary>
    /// 成功色。
    /// </summary>
    public Color SuccessColor { get; set; }

    /// <summary>
    /// 信息色。
    /// </summary>
    public Color InfoColor { get; set; }

    /// <summary>
    /// 表格奇数行背景。
    /// </summary>
    public Color AlternatingRowColor { get; set; }

    /// <summary>
    /// 表格表头背景。
    /// </summary>
    public Color DataGridHeaderColor { get; set; }

    /// <summary>
    /// 图表背景色。
    /// </summary>
    public Color ChartBackgroundColor { get; set; }

    /// <summary>
    /// 图表网格色。
    /// </summary>
    public Color ChartGridColor { get; set; }
}

/// <summary>
/// 主题管理器。
/// </summary>
public static class ThemeManager
{
    private static ThemeConfig _currentTheme = CreateLightTheme();
    private static ThemeType _currentThemeType = ThemeType.Light;

    /// <summary>
    /// 当前主题。
    /// </summary>
    public static ThemeConfig CurrentTheme => _currentTheme;

    /// <summary>
    /// 当前主题类型。
    /// </summary>
    public static ThemeType CurrentThemeType => _currentThemeType;

    /// <summary>
    /// 主题变更事件。
    /// </summary>
    public static event EventHandler<ThemeConfig>? ThemeChanged;

    /// <summary>
    /// 设置主题。
    /// </summary>
    public static void SetTheme(ThemeType themeType)
    {
        _currentThemeType = themeType;
        _currentTheme = themeType switch
        {
            ThemeType.Light => CreateLightTheme(),
            ThemeType.Dark => CreateDarkTheme(),
            ThemeType.Blue => CreateBlueTheme(),
            ThemeType.Industrial => CreateIndustrialTheme(),
            _ => CreateLightTheme()
        };

        ThemeChanged?.Invoke(null, _currentTheme);
    }

    /// <summary>
    /// 创建亮色主题。
    /// </summary>
    private static ThemeConfig CreateLightTheme() => new()
    {
        Name = "亮色主题",
        Type = ThemeType.Light,
        PrimaryColor = Color.FromArgb(0, 120, 215),
        BackgroundColor = Color.White,
        ForegroundColor = Color.FromArgb(33, 37, 41),
        SecondaryBackgroundColor = Color.FromArgb(248, 249, 250),
        BorderColor = Color.FromArgb(222, 226, 230),
        HoverColor = Color.FromArgb(233, 236, 239),
        SelectedColor = Color.FromArgb(0, 123, 255),
        ErrorColor = Color.FromArgb(220, 53, 69),
        WarningColor = Color.FromArgb(255, 193, 7),
        SuccessColor = Color.FromArgb(40, 167, 69),
        InfoColor = Color.FromArgb(23, 162, 184),
        AlternatingRowColor = Color.FromArgb(248, 249, 250),
        DataGridHeaderColor = Color.FromArgb(233, 236, 239),
        ChartBackgroundColor = Color.White,
        ChartGridColor = Color.FromArgb(222, 226, 230)
    };

    /// <summary>
    /// 创建暗色主题。
    /// </summary>
    private static ThemeConfig CreateDarkTheme() => new()
    {
        Name = "暗色主题",
        Type = ThemeType.Dark,
        PrimaryColor = Color.FromArgb(88, 166, 255),
        BackgroundColor = Color.FromArgb(30, 30, 30),
        ForegroundColor = Color.FromArgb(220, 220, 220),
        SecondaryBackgroundColor = Color.FromArgb(45, 45, 45),
        BorderColor = Color.FromArgb(60, 60, 60),
        HoverColor = Color.FromArgb(55, 55, 55),
        SelectedColor = Color.FromArgb(88, 166, 255),
        ErrorColor = Color.FromArgb(248, 98, 96),
        WarningColor = Color.FromArgb(255, 193, 7),
        SuccessColor = Color.FromArgb(80, 200, 120),
        InfoColor = Color.FromArgb(96, 200, 220),
        AlternatingRowColor = Color.FromArgb(40, 40, 40),
        DataGridHeaderColor = Color.FromArgb(45, 45, 45),
        ChartBackgroundColor = Color.FromArgb(25, 25, 25),
        ChartGridColor = Color.FromArgb(50, 50, 50)
    };

    /// <summary>
    /// 创建蓝色主题。
    /// </summary>
    private static ThemeConfig CreateBlueTheme() => new()
    {
        Name = "蓝色主题",
        Type = ThemeType.Blue,
        PrimaryColor = Color.FromArgb(0, 82, 147),
        BackgroundColor = Color.FromArgb(240, 248, 255),
        ForegroundColor = Color.FromArgb(20, 60, 100),
        SecondaryBackgroundColor = Color.FromArgb(220, 235, 250),
        BorderColor = Color.FromArgb(180, 210, 240),
        HoverColor = Color.FromArgb(200, 225, 250),
        SelectedColor = Color.FromArgb(0, 120, 215),
        ErrorColor = Color.FromArgb(180, 40, 40),
        WarningColor = Color.FromArgb(230, 180, 0),
        SuccessColor = Color.FromArgb(30, 130, 60),
        InfoColor = Color.FromArgb(0, 130, 160),
        AlternatingRowColor = Color.FromArgb(235, 245, 255),
        DataGridHeaderColor = Color.FromArgb(200, 220, 245),
        ChartBackgroundColor = Color.FromArgb(250, 252, 255),
        ChartGridColor = Color.FromArgb(200, 220, 240)
    };

    /// <summary>
    /// 创建工业主题。
    /// </summary>
    private static ThemeConfig CreateIndustrialTheme() => new()
    {
        Name = "工业主题",
        Type = ThemeType.Industrial,
        PrimaryColor = Color.FromArgb(255, 165, 0),
        BackgroundColor = Color.FromArgb(40, 40, 40),
        ForegroundColor = Color.FromArgb(200, 200, 200),
        SecondaryBackgroundColor = Color.FromArgb(50, 50, 50),
        BorderColor = Color.FromArgb(70, 70, 70),
        HoverColor = Color.FromArgb(60, 60, 60),
        SelectedColor = Color.FromArgb(255, 165, 0),
        ErrorColor = Color.FromArgb(255, 80, 80),
        WarningColor = Color.FromArgb(255, 200, 50),
        SuccessColor = Color.FromArgb(80, 200, 80),
        InfoColor = Color.FromArgb(80, 180, 255),
        AlternatingRowColor = Color.FromArgb(45, 45, 45),
        DataGridHeaderColor = Color.FromArgb(55, 55, 55),
        ChartBackgroundColor = Color.FromArgb(30, 30, 30),
        ChartGridColor = Color.FromArgb(60, 60, 60)
    };

    /// <summary>
    /// 应用主题到控件。
    /// </summary>
    public static void ApplyTheme(Control control)
    {
        if (control == null) return;

        var theme = _currentTheme;

        control.BackColor = theme.BackgroundColor;
        control.ForeColor = theme.ForegroundColor;

        switch (control)
        {
            case DataGridView dataGrid:
                ApplyThemeToDataGrid(dataGrid, theme);
                break;
            case Button button when button.FlatStyle == FlatStyle.Flat:
                button.BackColor = theme.PrimaryColor;
                button.ForeColor = Color.White;
                break;
            case TextBox textBox:
                textBox.BackColor = theme.SecondaryBackgroundColor;
                textBox.ForeColor = theme.ForegroundColor;
                break;
        }

        foreach (Control child in control.Controls)
        {
            ApplyTheme(child);
        }
    }

    /// <summary>
    /// 应用主题到DataGridView。
    /// </summary>
    private static void ApplyThemeToDataGrid(DataGridView dataGrid, ThemeConfig theme)
    {
        dataGrid.BackgroundColor = theme.BackgroundColor;
        dataGrid.DefaultCellStyle.BackColor = theme.BackgroundColor;
        dataGrid.DefaultCellStyle.ForeColor = theme.ForegroundColor;
        dataGrid.DefaultCellStyle.SelectionBackColor = theme.SelectedColor;
        dataGrid.DefaultCellStyle.SelectionForeColor = Color.White;
        dataGrid.AlternatingRowsDefaultCellStyle.BackColor = theme.AlternatingRowColor;
        dataGrid.ColumnHeadersDefaultCellStyle.BackColor = theme.DataGridHeaderColor;
        dataGrid.ColumnHeadersDefaultCellStyle.ForeColor = theme.ForegroundColor;
        dataGrid.RowHeadersDefaultCellStyle.BackColor = theme.SecondaryBackgroundColor;
        dataGrid.RowHeadersDefaultCellStyle.ForeColor = theme.ForegroundColor;
        dataGrid.BorderStyle = BorderStyle.None;
    }
}

/// <summary>
/// 主题支持混合器（用于WinForms控件）。
/// </summary>
public class ThemeMixin
{
    /// <summary>
    /// 应用当前主题颜色。
    /// </summary>
    public static void ApplyThemeColors(Control control)
    {
        ThemeManager.ApplyTheme(control);
    }

    /// <summary>
    /// 获取当前主题颜色。
    /// </summary>
    public static Color GetThemeColor(string colorType)
    {
        var theme = ThemeManager.CurrentTheme;
        return colorType.ToLower() switch
        {
            "primary" => theme.PrimaryColor,
            "background" => theme.BackgroundColor,
            "foreground" => theme.ForegroundColor,
            "secondary" => theme.SecondaryBackgroundColor,
            "border" => theme.BorderColor,
            "hover" => theme.HoverColor,
            "selected" => theme.SelectedColor,
            "error" => theme.ErrorColor,
            "warning" => theme.WarningColor,
            "success" => theme.SuccessColor,
            "info" => theme.InfoColor,
            _ => theme.ForegroundColor
        };
    }
}
