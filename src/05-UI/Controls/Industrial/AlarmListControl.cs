using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Acme.Industrial.UI.Controls.Industrial;

/// <summary>
/// 报警级别。
/// </summary>
public enum AlarmLevel
{
    /// <summary>信息。</summary>
    Info,

    /// <summary>警告。</summary>
    Warning,

    /// <summary>错误。</summary>
    Error,

    /// <summary>严重。</summary>
    Critical
}

/// <summary>
/// 报警项。
/// </summary>
public class AlarmItem
{
    /// <summary>
    /// 报警ID。
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// 报警时间。
    /// </summary>
    public DateTime Time { get; set; } = DateTime.Now;

    /// <summary>
    /// 报警点名称。
    /// </summary>
    public string TagName { get; set; } = string.Empty;

    /// <summary>
    /// 报警描述。
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 报警级别。
    /// </summary>
    public AlarmLevel Level { get; set; } = AlarmLevel.Warning;

    /// <summary>
    /// 是否已确认。
    /// </summary>
    public bool IsAcknowledged { get; set; }

    /// <summary>
    /// 确认时间。
    /// </summary>
    public DateTime? AckTime { get; set; }

    /// <summary>
    /// 确认人。
    /// </summary>
    public string AckBy { get; set; } = string.Empty;

    /// <summary>
    /// 当前值。
    /// </summary>
    public string CurrentValue { get; set; } = string.Empty;

    /// <summary>
    /// 设定值。
    /// </summary>
    public string SetValue { get; set; } = string.Empty;
}

/// <summary>
/// 报警列表控件。
/// 用于显示和管理工业报警信息。
/// </summary>
public class AlarmListControl : Control
{
    #region 属性

    private List<AlarmItem> _alarms = new();
    private List<AlarmItem> _filteredAlarms = new();
    private bool _showAcknowledged = true;
    private bool _showUnacknowledgedOnly = false;
    private AlarmLevel? _filterLevel = null;
    private int _maxAlarms = 100;
    private bool _autoScroll = true;
    private Color _infoColor = Color.FromArgb(23, 162, 184);
    private Color _warningColor = Color.FromArgb(255, 193, 7);
    private Color _errorColor = Color.FromArgb(220, 53, 69);
    private Color _criticalColor = Color.FromArgb(111, 66, 193);
    private Font _titleFont = new Font("微软雅黑", 10, FontStyle.Bold);
    private Font _headerFont = new Font("微软雅黑", 9, FontStyle.Bold);
    private Font _contentFont = new Font("微软雅黑", 9);
    private int _rowHeight = 28;
    private int _selectedIndex = -1;

    /// <summary>
    /// 是否显示已确认的报警。
    /// </summary>
    [Category("行为")]
    [DefaultValue(true)]
    public bool ShowAcknowledged
    {
        get => _showAcknowledged;
        set { _showAcknowledged = value; ApplyFilter(); }
    }

    /// <summary>
    /// 是否仅显示未确认的报警。
    /// </summary>
    [Category("行为")]
    [DefaultValue(false)]
    public bool ShowUnacknowledgedOnly
    {
        get => _showUnacknowledgedOnly;
        set { _showUnacknowledgedOnly = value; ApplyFilter(); }
    }

    /// <summary>
    /// 最大报警数量。
    /// </summary>
    [Category("数据")]
    [DefaultValue(100)]
    public int MaxAlarms
    {
        get => _maxAlarms;
        set { _maxAlarms = value; }
    }

    /// <summary>
    /// 是否自动滚动。
    /// </summary>
    [Category("行为")]
    [DefaultValue(true)]
    public bool AutoScroll
    {
        get => _autoScroll;
        set => _autoScroll = value;
    }

    #endregion

    #region 事件

    /// <summary>
    /// 报警点击时触发。
    /// </summary>
    public event EventHandler<AlarmItem>? AlarmClicked;

    /// <summary>
    /// 报警确认时触发。
    /// </summary>
    public event EventHandler<AlarmItem>? AlarmAcknowledged;

    /// <summary>
    /// 双击报警时触发。
    /// </summary>
    public event EventHandler<AlarmItem>? AlarmDoubleClicked;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化报警列表控件。
    /// </summary>
    public AlarmListControl()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.Selectable, true);
        Size = new Size(600, 300);
        BackColor = Color.FromArgb(30, 30, 30);
        ForeColor = Color.White;
        Cursor = Cursors.Hand;
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 添加报警。
    /// </summary>
    public void AddAlarm(AlarmItem alarm)
    {
        _alarms.Insert(0, alarm);

        // 限制数量
        while (_alarms.Count > _maxAlarms)
        {
            _alarms.RemoveAt(_alarms.Count - 1);
        }

        ApplyFilter();
        Invalidate();

        if (_autoScroll)
        {
            _selectedIndex = 0;
        }
    }

    /// <summary>
    /// 添加报警（简便方法）。
    /// </summary>
    public void AddAlarm(string tagName, string message, AlarmLevel level, string currentValue = "", string setValue = "")
    {
        AddAlarm(new AlarmItem
        {
            Id = Guid.NewGuid().ToString(),
            Time = DateTime.Now,
            TagName = tagName,
            Message = message,
            Level = level,
            CurrentValue = currentValue,
            SetValue = setValue
        });
    }

    /// <summary>
    /// 确认报警。
    /// </summary>
    public void AcknowledgeAlarm(string alarmId, string ackBy = "操作员")
    {
        var alarm = _alarms.Find(a => a.Id == alarmId);
        if (alarm != null)
        {
            alarm.IsAcknowledged = true;
            alarm.AckTime = DateTime.Now;
            alarm.AckBy = ackBy;
            ApplyFilter();
            Invalidate();
            AlarmAcknowledged?.Invoke(this, alarm);
        }
    }

    /// <summary>
    /// 确认选中的报警。
    /// </summary>
    public void AcknowledgeSelected(string ackBy = "操作员")
    {
        if (_selectedIndex >= 0 && _selectedIndex < _filteredAlarms.Count)
        {
            AcknowledgeAlarm(_filteredAlarms[_selectedIndex].Id, ackBy);
        }
    }

    /// <summary>
    /// 确认所有报警。
    /// </summary>
    public void AcknowledgeAll(string ackBy = "操作员")
    {
        foreach (var alarm in _alarms.Where(a => !a.IsAcknowledged))
        {
            alarm.IsAcknowledged = true;
            alarm.AckTime = DateTime.Now;
            alarm.AckBy = ackBy;
        }
        ApplyFilter();
        Invalidate();
    }

    /// <summary>
    /// 清除所有报警。
    /// </summary>
    public void ClearAll()
    {
        _alarms.Clear();
        _filteredAlarms.Clear();
        _selectedIndex = -1;
        Invalidate();
    }

    /// <summary>
    /// 获取未确认报警数量。
    /// </summary>
    public int GetUnacknowledgedCount()
    {
        return _alarms.Count(a => !a.IsAcknowledged);
    }

    /// <summary>
    /// 获取未确认报警数量（按级别）。
    /// </summary>
    public int GetUnacknowledgedCount(AlarmLevel level)
    {
        return _alarms.Count(a => !a.IsAcknowledged && a.Level == level);
    }

    #endregion

    #region 私有方法

    private void ApplyFilter()
    {
        _filteredAlarms = _alarms.Where(a =>
        {
            if (!_showAcknowledged && a.IsAcknowledged) return false;
            if (_showUnacknowledgedOnly && a.IsAcknowledged) return false;
            if (_filterLevel.HasValue && a.Level != _filterLevel.Value) return false;
            return true;
        }).ToList();

        if (_selectedIndex >= _filteredAlarms.Count)
        {
            _selectedIndex = _filteredAlarms.Count - 1;
        }
    }

    private Color GetLevelColor(AlarmLevel level)
    {
        return level switch
        {
            AlarmLevel.Info => _infoColor,
            AlarmLevel.Warning => _warningColor,
            AlarmLevel.Error => _errorColor,
            AlarmLevel.Critical => _criticalColor,
            _ => _warningColor
        };
    }

    private string GetLevelText(AlarmLevel level)
    {
        return level switch
        {
            AlarmLevel.Info => "信息",
            AlarmLevel.Warning => "警告",
            AlarmLevel.Error => "错误",
            AlarmLevel.Critical => "严重",
            _ => "未知"
        };
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

            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var startY = 0;

            // 绘制标题栏
            DrawTitleBar(g, ref startY);

            // 绘制列标题
            DrawColumnHeaders(g, ref startY);

            // 绘制报警列表
            DrawAlarmList(g, startY);

            // 绘制底部状态栏
            DrawStatusBar(g);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AlarmListControl OnPaint error: {ex.Message}");
        }
    }

    private void DrawTitleBar(Graphics g, ref int startY)
    {
        var titleHeight = 35;

        using var brush = new SolidBrush(Color.FromArgb(45, 45, 45));
        g.FillRectangle(brush, 0, startY, ClientSize.Width, titleHeight);

        // 标题
        using var font = _titleFont;
        using var textBrush = new SolidBrush(ForeColor);

        var unackCount = GetUnacknowledgedCount();
        var title = unackCount > 0 ? $"报警列表 ({unackCount} 未确认)" : "报警列表";
        g.DrawString(title, font, textBrush, 10, startY + 8);

        // 未确认计数图标
        if (unackCount > 0)
        {
            var badgeX = ClientSize.Width - 80;
            var badgeY = startY + 8;
            var badgeColor = _errorColor;

            using var badgeBrush = new SolidBrush(badgeColor);
            g.FillEllipse(badgeBrush, badgeX, badgeY, 20, 20);

            using var badgeFont = new Font("Arial", 9, FontStyle.Bold);
            using var badgeTextBrush = new SolidBrush(Color.White);
            var text = unackCount > 99 ? "99+" : unackCount.ToString();
            var textSize = g.MeasureString(text, badgeFont);
            g.DrawString(text, badgeFont, badgeTextBrush, badgeX + (20 - textSize.Width) / 2, badgeY + 3);
        }

        startY += titleHeight;
    }

    private void DrawColumnHeaders(Graphics g, ref int startY)
    {
        var headerHeight = 30;

        using var brush = new SolidBrush(Color.FromArgb(55, 55, 55));
        g.FillRectangle(brush, 0, startY, ClientSize.Width, headerHeight);

        using var font = _headerFont;
        using var textBrush = new SolidBrush(Color.FromArgb(200, 200, 200));

        var columns = new[] { ("状态", 50), ("时间", 130), ("级别", 60), ("标签", 150), ("描述", 0), ("当前值", 80), ("确认", 100) };
        var x = 5;

        foreach (var (title, width) in columns)
        {
            if (width == 0)
            {
                // 最后一列填满
                g.DrawString(title, font, textBrush, x, startY + 8);
                break;
            }
            g.DrawString(title, font, textBrush, x, startY + 8);
            x += width;
        }

        // 底部分隔线
        using var linePen = new Pen(Color.FromArgb(80, 80, 80), 1);
        g.DrawLine(linePen, 0, startY + headerHeight, ClientSize.Width, startY + headerHeight);

        startY += headerHeight;
    }

    private void DrawAlarmList(Graphics g, int startY)
    {
        var availableHeight = ClientSize.Height - startY - 35;
        var visibleRows = (int)(availableHeight / _rowHeight);

        // 绘制可见行
        for (var i = 0; i < Math.Min(visibleRows, _filteredAlarms.Count); i++)
        {
            var rowY = startY + i * _rowHeight;
            var alarm = _filteredAlarms[i];

            // 选中行背景
            if (i == _selectedIndex)
            {
                using var selectBrush = new SolidBrush(Color.FromArgb(60, 60, 60));
                g.FillRectangle(selectBrush, 0, rowY, ClientSize.Width, _rowHeight);
            }
            else if (i % 2 == 1)
            {
                using var altBrush = new SolidBrush(Color.FromArgb(35, 35, 35));
                g.FillRectangle(altBrush, 0, rowY, ClientSize.Width, _rowHeight);
            }

            // 未确认报警左边框
            if (!alarm.IsAcknowledged)
            {
                var levelColor = GetLevelColor(alarm.Level);
                using var levelBrush = new SolidBrush(levelColor);
                g.FillRectangle(levelBrush, 0, rowY, 3, _rowHeight);
            }

            // 绘制内容
            DrawRow(g, alarm, rowY);
        }

        // 绘制分隔线
        using var linePen = new Pen(Color.FromArgb(50, 50, 50), 1);
        for (var i = 0; i < Math.Min(visibleRows, _filteredAlarms.Count); i++)
        {
            var rowY = startY + (i + 1) * _rowHeight;
            g.DrawLine(linePen, 0, rowY, ClientSize.Width, rowY);
        }
    }

    private void DrawRow(Graphics g, AlarmItem alarm, int rowY)
    {
        using var font = _contentFont;
        var y = rowY + 6;

        var columns = new[] { (GetAckStatus(alarm), 50), (alarm.Time.ToString("HH:mm:ss"), 130), (GetLevelText(alarm.Level), 60), (alarm.TagName, 150), (alarm.Message, 0), (alarm.CurrentValue, 80), (GetAckStatus(alarm), 100) };
        var x = 8;

        foreach (var (text, width) in columns)
        {
            if (width == 0)
            {
                using var msgBrush = new SolidBrush(ForeColor);
                var msgText = TruncateText(g, text, font, ClientSize.Width - x - 10);
                if (!string.IsNullOrEmpty(msgText))
                    g.DrawString(msgText, font, msgBrush, x, y);
                break;
            }

            using var brush = text == GetLevelText(alarm.Level)
                ? new SolidBrush(GetLevelColor(alarm.Level))
                : (!alarm.IsAcknowledged && text == GetAckStatus(alarm)
                    ? new SolidBrush(Color.FromArgb(255, 200, 0))
                    : new SolidBrush(ForeColor));

            g.DrawString(text, font, brush, x, y);
            x += width;
        }
    }

    private void DrawStatusBar(Graphics g)
    {
        var statusY = ClientSize.Height - 35;

        using var brush = new SolidBrush(Color.FromArgb(40, 40, 40));
        g.FillRectangle(brush, 0, statusY, ClientSize.Width, 35);

        using var font = new Font("微软雅黑", 8);
        using var textBrush = new SolidBrush(Color.FromArgb(150, 150, 150));

        var stats = $"共 {_filteredAlarms.Count} 条 | ";
        stats += $"未确认: {GetUnacknowledgedCount()} | ";
        stats += $"严重: {GetUnacknowledgedCount(AlarmLevel.Critical)} | ";
        stats += $"错误: {GetUnacknowledgedCount(AlarmLevel.Error)} | ";
        stats += $"警告: {GetUnacknowledgedCount(AlarmLevel.Warning)}";

        g.DrawString(stats, font, textBrush, 10, statusY + 10);

        // 右下角时间
        g.DrawString(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), font, textBrush, ClientSize.Width - 150, statusY + 10);
    }

    private string GetAckStatus(AlarmItem alarm)
    {
        return alarm.IsAcknowledged ? "已确认" : "未确认";
    }

    private string TruncateText(Graphics g, string text, Font font, float maxWidth)
    {
        if (g.MeasureString(text, font).Width <= maxWidth)
            return text;

        while (text.Length > 3 && g.MeasureString(text + "...", font).Width > maxWidth)
        {
            text = text.Substring(0, text.Length - 1);
        }

        return text + "...";
    }

    #endregion

    #region 控件行为

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        var headerHeight = 65; // 标题栏 + 列标题
        var row = (e.Y - headerHeight) / _rowHeight;

        if (row >= 0 && row < _filteredAlarms.Count)
        {
            _selectedIndex = row;
            Invalidate();
            AlarmClicked?.Invoke(this, _filteredAlarms[row]);
        }
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);

        var headerHeight = 65;
        var row = (e.Y - headerHeight) / _rowHeight;

        if (row >= 0 && row < _filteredAlarms.Count)
        {
            AlarmDoubleClicked?.Invoke(this, _filteredAlarms[row]);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
        {
            AcknowledgeSelected();
        }
        else if (e.KeyCode == Keys.Up && _selectedIndex > 0)
        {
            _selectedIndex--;
            Invalidate();
        }
        else if (e.KeyCode == Keys.Down && _selectedIndex < _filteredAlarms.Count - 1)
        {
            _selectedIndex++;
            Invalidate();
        }
    }

    protected override Size DefaultSize => new Size(600, 300);

    #endregion
}
