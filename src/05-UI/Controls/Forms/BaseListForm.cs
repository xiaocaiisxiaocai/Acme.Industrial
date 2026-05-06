using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Acme.Industrial.UI.Forms;

/// <summary>
/// 列表窗体基类。
/// 提供查询 + 表格 + 分页的标准列表功能。
/// </summary>
public abstract class BaseListForm : BaseForm
{
    #region 属性

    /// <summary>
    /// 搜索面板。
    /// </summary>
    protected Panel SearchPanel { get; private set; } = null!;

    /// <summary>
    /// 数据表格。
    /// </summary>
    protected DataGridView? DataGrid { get; private set; }

    /// <summary>
    /// 工具栏面板。
    /// </summary>
    protected Panel ToolBarPanel { get; private set; } = null!;

    /// <summary>
    /// 分页控件面板。
    /// </summary>
    protected Panel PagerPanel { get; private set; } = null!;

    /// <summary>
    /// 当前页码。
    /// </summary>
    protected int CurrentPage { get; set; } = 1;

    /// <summary>
    /// 每页记录数。
    /// </summary>
    protected int PageSize { get; set; } = 20;

    /// <summary>
    /// 总记录数。
    /// </summary>
    protected int TotalCount { get; set; }

    /// <summary>
    /// 总页数。
    /// </summary>
    protected int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// 选中的数据行。
    /// </summary>
    protected DataGridViewRow? SelectedRow => DataGrid?.SelectedRows.Count > 0 ? DataGrid!.SelectedRows[0] : null;

    /// <summary>
    /// 选中的数据ID。
    /// </summary>
    protected object? SelectedId => SelectedRow?.Cells["Id"]?.Value ?? SelectedRow?.Cells["ID"]?.Value;

    /// <summary>
    /// 工具栏高度。
    /// </summary>
    protected virtual int ToolBarHeight => 40;

    /// <summary>
    /// 搜索面板高度。
    /// </summary>
    protected virtual int SearchPanelHeight => 45;

    /// <summary>
    /// 分页面板高度。
    /// </summary>
    protected virtual int PagerPanelHeight => 40;

    #endregion

    #region 字段

    private Label _lblTotalCount = null!;
    private Label _lblPageInfo = null!;
    private Button _btnFirst = null!;
    private Button _btnPrev = null!;
    private Button _btnNext = null!;
    private Button _btnLast = null!;
    private TextBox _txtPage = null!;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化列表窗体。
    /// </summary>
    protected BaseListForm()
    {
        WindowState = FormWindowState.Maximized;
    }

    #endregion

    #region 初始化

    /// <inheritdoc />
    protected override void InitializeForm()
    {
        InitializeSearchPanel();
        InitializeToolBar();
        InitializeDataGrid();
        InitializePagerPanel();

        LayoutControls();
    }

    /// <summary>
    /// 初始化搜索面板。
    /// </summary>
    protected virtual void InitializeSearchPanel()
    {
        SearchPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = SearchPanelHeight,
            BackColor = Color.FromArgb(245, 245, 245),
            Padding = new Padding(10, 8, 10, 8)
        };
        Controls.Add(SearchPanel);
    }

    /// <summary>
    /// 初始化工具栏。
    /// </summary>
    protected virtual void InitializeToolBar()
    {
        ToolBarPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = ToolBarHeight,
            BackColor = Color.FromArgb(250, 250, 250),
            Padding = new Padding(10, 5, 10, 5)
        };

        // 默认添加按钮
        var btnAdd = CreateStandardButton("新增");
        btnAdd.Click += async (s, e) => await OnAddAsync();
        btnAdd.Width = 70;
        ToolBarPanel.Controls.Add(btnAdd);

        var btnEdit = CreateStandardButton("编辑");
        btnEdit.Click += async (s, e) => await OnEditAsync();
        btnEdit.Width = 70;
        btnEdit.Left = 80;
        ToolBarPanel.Controls.Add(btnEdit);

        var btnDelete = CreateStandardButton("删除");
        btnDelete.BackColor = Color.FromArgb(220, 53, 69);
        btnDelete.Click += async (s, e) => await OnDeleteAsync();
        btnDelete.Width = 70;
        btnDelete.Left = 160;
        ToolBarPanel.Controls.Add(btnDelete);

        var btnRefresh = CreateStandardButton("刷新");
        btnRefresh.BackColor = Color.FromArgb(108, 117, 125);
        btnRefresh.Click += async (s, e) => await LoadDataAsync();
        btnRefresh.Width = 70;
        btnRefresh.Left = 240;
        ToolBarPanel.Controls.Add(btnRefresh);

        var btnExport = CreateStandardButton("导出");
        btnExport.BackColor = Color.FromArgb(40, 167, 69);
        btnExport.Click += async (s, e) => await OnExportAsync();
        btnExport.Width = 70;
        btnExport.Left = 320;
        ToolBarPanel.Controls.Add(btnExport);

        Controls.Add(ToolBarPanel);
    }

    /// <summary>
    /// 初始化数据表格。
    /// </summary>
    protected virtual void InitializeDataGrid()
    {
        DataGrid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            RowHeadersWidth = 30,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            ReadOnly = true,
            AutoGenerateColumns = false,
            Font = new Font("微软雅黑", 9F),
            ColumnHeadersHeight = 35,
            RowTemplate = { Height = 30 }
        };

        DataGrid.DoubleClick += async (s, e) => await OnEditAsync();
        DataGrid.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                _ = OnEditAsync();
            }
            else if (e.KeyCode == Keys.Delete)
            {
                _ = OnDeleteAsync();
            }
        };

        // 样式
        DataGrid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(233, 236, 239),
            ForeColor = Color.FromArgb(73, 80, 87),
            Font = new Font("微软雅黑", 9F, FontStyle.Bold),
            Padding = new Padding(8, 0, 8, 0)
        };

        DataGrid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(248, 249, 250)
        };

        Controls.Add(DataGrid);
    }

    /// <summary>
    /// 初始化分页控件。
    /// </summary>
    protected virtual void InitializePagerPanel()
    {
        PagerPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = PagerPanelHeight,
            BackColor = Color.FromArgb(245, 245, 245),
            Padding = new Padding(10, 5, 10, 5)
        };

        // 总记录数
        _lblTotalCount = new Label
        {
            Text = "共 0 条记录",
            AutoSize = true,
            Font = new Font("微软雅黑", 9F),
            ForeColor = Color.FromArgb(108, 117, 125),
            Location = new Point(10, 12)
        };
        PagerPanel.Controls.Add(_lblTotalCount);

        // 页码信息
        _lblPageInfo = new Label
        {
            Text = "第 0 / 0 页",
            AutoSize = true,
            Font = new Font("微软雅黑", 9F),
            ForeColor = Color.FromArgb(73, 80, 87),
            Location = new Point(120, 12)
        };
        PagerPanel.Controls.Add(_lblPageInfo);

        // 首页
        _btnFirst = new Button
        {
            Text = "首页",
            Size = new Size(50, 26),
            Location = new Point(PagerPanel.Width - 300, 7),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("微软雅黑", 9F)
        };
        _btnFirst.Click += (s, e) => { CurrentPage = 1; _ = LoadDataAsync(); };
        PagerPanel.Controls.Add(_btnFirst);

        // 上一页
        _btnPrev = new Button
        {
            Text = "上一页",
            Size = new Size(60, 26),
            Location = new Point(PagerPanel.Width - 245, 7),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("微软雅黑", 9F)
        };
        _btnPrev.Click += (s, e) => { if (CurrentPage > 1) { CurrentPage--; _ = LoadDataAsync(); } };
        PagerPanel.Controls.Add(_btnPrev);

        // 页码输入
        _txtPage = new TextBox
        {
            Size = new Size(50, 26),
            Location = new Point(PagerPanel.Width - 180, 8),
            TextAlign = HorizontalAlignment.Center,
            Font = new Font("微软雅黑", 9F)
        };
        _txtPage.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter && int.TryParse(_txtPage.Text, out var page))
            {
                if (page >= 1 && page <= TotalPages)
                {
                    CurrentPage = page;
                    _ = LoadDataAsync();
                }
            }
        };
        PagerPanel.Controls.Add(_txtPage);

        // 下一页
        _btnNext = new Button
        {
            Text = "下一页",
            Size = new Size(60, 26),
            Location = new Point(PagerPanel.Width - 125, 7),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("微软雅黑", 9F)
        };
        _btnNext.Click += (s, e) => { if (CurrentPage < TotalPages) { CurrentPage++; _ = LoadDataAsync(); } };
        PagerPanel.Controls.Add(_btnNext);

        // 末页
        _btnLast = new Button
        {
            Text = "末页",
            Size = new Size(50, 26),
            Location = new Point(PagerPanel.Width - 60, 7),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("微软雅黑", 9F)
        };
        _btnLast.Click += (s, e) => { CurrentPage = TotalPages; _ = LoadDataAsync(); };
        PagerPanel.Controls.Add(_btnLast);

        Controls.Add(PagerPanel);
    }

    /// <summary>
    /// 布局控件。
    /// </summary>
    private void LayoutControls()
    {
        // 确保控件层级正确
        SearchPanel.BringToFront();
    }

    #endregion

    #region 虚方法

    /// <summary>
    /// 加载数据。子类实现。
    /// </summary>
    protected abstract Task LoadDataAsync();

    /// <summary>
    /// 新增。子类实现。
    /// </summary>
    protected virtual Task OnAddAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 编辑。子类实现。
    /// </summary>
    protected virtual Task OnEditAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 删除。子类实现。
    /// </summary>
    protected virtual Task OnDeleteAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 导出。子类实现。
    /// </summary>
    protected virtual Task OnExportAsync()
    {
        return Task.CompletedTask;
    }

    #endregion

    #region 受保护方法

    /// <summary>
    /// 添加搜索条件控件。
    /// </summary>
    protected void AddSearchControl(Control control, int leftOffset)
    {
        control.Location = new Point(leftOffset, 8);
        SearchPanel.Controls.Add(control);
    }

    /// <summary>
    /// 添加搜索标签和控件。
    /// </summary>
    protected void AddSearchItem(string labelText, Control control, int leftOffset)
    {
        var label = CreateStandardLabel(labelText);
        label.Location = new Point(leftOffset, 10);
        SearchPanel.Controls.Add(label);

        control.Location = new Point(leftOffset + 70, 7);
        control.Tag = label;
        SearchPanel.Controls.Add(control);
    }

    /// <summary>
    /// 添加搜索按钮。
    /// </summary>
    protected Button AddSearchButton(string text, EventHandler click, int leftOffset)
    {
        var btn = CreateStandardButton(text, click);
        btn.Location = new Point(leftOffset, 5);
        btn.Width = 70;
        SearchPanel.Controls.Add(btn);
        return btn;
    }

    /// <summary>
    /// 更新分页信息。
    /// </summary>
    protected void UpdatePager()
    {
        _lblTotalCount.Text = $"共 {TotalCount} 条记录";
        _lblPageInfo.Text = $"第 {CurrentPage} / {TotalPages} 页";
        _txtPage.Text = CurrentPage.ToString();

        _btnFirst.Enabled = CurrentPage > 1;
        _btnPrev.Enabled = CurrentPage > 1;
        _btnNext.Enabled = CurrentPage < TotalPages;
        _btnLast.Enabled = CurrentPage < TotalPages;
    }

    /// <summary>
    /// 绑定数据源。
    /// </summary>
    protected void BindData<T>(IEnumerable<T> data)
    {
        if (DataGrid != null)
        {
            DataGrid.DataSource = null;
            DataGrid.DataSource = data;
            DataGrid.AutoResizeColumns();
        }
    }

    /// <summary>
    /// 启用/禁用工具栏按钮。
    /// </summary>
    protected void SetToolBarButtonsEnabled(bool enabled)
    {
        foreach (Control ctrl in ToolBarPanel.Controls)
        {
            if (ctrl is Button btn)
            {
                btn.Enabled = enabled;
            }
        }
    }

    #endregion

    #region 窗体事件

    /// <inheritdoc />
    protected override void OnFormLoad()
    {
        base.OnFormLoad();
        _ = LoadDataAsync();
    }

    #endregion
}
