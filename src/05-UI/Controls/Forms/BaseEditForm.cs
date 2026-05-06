using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Acme.Industrial.UI.Forms;

/// <summary>
/// 编辑表单基类。
/// 提供表单编辑的标准功能。
/// </summary>
public abstract class BaseEditForm : BaseForm
{
    #region 属性

    /// <summary>
    /// 表单容器面板。
    /// </summary>
    protected Panel FormPanel { get; private set; } = null!;

    /// <summary>
    /// 按钮面板。
    /// </summary>
    protected Panel ButtonPanel { get; private set; } = null!;

    /// <summary>
    /// 是否为编辑模式。
    /// </summary>
    protected bool IsEditMode { get; private set; }

    /// <summary>
    /// 编辑实体的ID。
    /// </summary>
    protected object? EditEntityId { get; private set; }

    /// <summary>
    /// 窗体宽度。
    /// </summary>
    protected virtual int FormWidth => 500;

    /// <summary>
    /// 窗体高度。
    /// </summary>
    protected virtual int FormHeight => 500;

    /// <summary>
    /// 表单列数。
    /// </summary>
    protected virtual int FormColumns => 2;

    /// <summary>
    /// 标签宽度。
    /// </summary>
    protected virtual int LabelWidth => 120;

    /// <summary>
    /// 输入框宽度。
    /// </summary>
    protected virtual int InputWidth => 200;

    /// <summary>
    /// 行高。
    /// </summary>
    protected virtual int RowHeight => 45;

    /// <summary>
    /// 左边距。
    /// </summary>
    protected virtual int LeftMargin => 20;

    /// <summary>
    /// 上边距。
    /// </summary>
    protected virtual int TopMargin => 20;

    /// <summary>
    /// 当前行索引。
    /// </summary>
    protected int CurrentRowIndex { get; private set; }

    /// <summary>
    /// 当前列索引。
    /// </summary>
    protected int CurrentColIndex { get; private set; }

    #endregion

    #region 字段

    private TableLayoutPanel? _tableLayout;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;
    private readonly List<Control> _formControls = new();

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化编辑窗体。
    /// </summary>
    protected BaseEditForm()
    {
        ClientSize = new Size(FormWidth, FormHeight);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
    }

    #endregion

    #region 初始化

    /// <inheritdoc />
    protected override void InitializeForm()
    {
        InitializeFormPanel();
        InitializeButtonPanel();
        InitializeTableLayout();
        InitializeFormControls();

        Text = IsEditMode ? "编辑" : "新增";
    }

    /// <summary>
    /// 初始化表单面板。
    /// </summary>
    protected virtual void InitializeFormPanel()
    {
        FormPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(10)
        };
        Controls.Add(FormPanel);
    }

    /// <summary>
    /// 初始化按钮面板。
    /// </summary>
    protected virtual void InitializeButtonPanel()
    {
        ButtonPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.FromArgb(245, 245, 245),
            Padding = new Padding(10)
        };
        Controls.Add(ButtonPanel);

        _btnSave = CreateStandardButton("保存", OnSaveClick);
        _btnSave.Width = 90;
        _btnSave.Height = 36;
        _btnSave.Location = new Point(ButtonPanel.Width - 200, 12);
        ButtonPanel.Controls.Add(_btnSave);

        _btnCancel = new Button
        {
            Text = "取消",
            Width = 90,
            Height = 36,
            Location = new Point(ButtonPanel.Width - 100, 12),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(108, 117, 125),
            ForeColor = Color.White,
            Font = new Font("微软雅黑", 9F)
        };
        _btnCancel.Click += OnCancelClick;
        ButtonPanel.Controls.Add(_btnCancel);
    }

    /// <summary>
    /// 初始化表格布局。
    /// </summary>
    protected virtual void InitializeTableLayout()
    {
        _tableLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = FormColumns * 2, // 每列包含标签和输入框
            CellBorderStyle = TableLayoutPanelCellBorderStyle.None
        };

        // 设置列样式
        for (var i = 0; i < FormColumns; i++)
        {
            _tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            _tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        }

        FormPanel.Controls.Add(_tableLayout);
    }

    /// <summary>
    /// 初始化表单控件。由子类实现。
    /// </summary>
    protected virtual void InitializeFormControls()
    {
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 设置为新增模式。
    /// </summary>
    public void SetNewMode()
    {
        IsEditMode = false;
        EditEntityId = null;
    }

    /// <summary>
    /// 设置为编辑模式。
    /// </summary>
    public void SetEditMode(object entityId)
    {
        IsEditMode = true;
        EditEntityId = entityId;
    }

    /// <summary>
    /// 准备表单数据（加载要编辑的数据）。
    /// </summary>
    public virtual async Task PrepareAsync()
    {
        if (IsEditMode && EditEntityId != null)
        {
            await LoadEntityAsync(EditEntityId);
        }
    }

    /// <summary>
    /// 获取表单值。
    /// </summary>
    public T? GetValue<T>(string fieldName)
    {
        var control = _formControls.FirstOrDefault(c => c.Name == fieldName);
        if (control == null) return default;

        return control switch
        {
            TextBox textBox => ConvertTo<T>(textBox.Text),
            ComboBox comboBox => comboBox.SelectedValue is T value ? value : default,
            CheckBox checkBox => ConvertTo<T>(checkBox.Checked.ToString()),
            NumericUpDown numeric => ConvertTo<T>(numeric.Value.ToString()),
            DateTimePicker picker => ConvertTo<T>(picker.Value.ToString()),
            _ => default
        };
    }

    /// <summary>
    /// 设置表单值。
    /// </summary>
    public void SetValue(string fieldName, object? value)
    {
        var control = _formControls.FirstOrDefault(c => c.Name == fieldName);
        if (control == null) return;

        switch (control)
        {
            case TextBox textBox:
                textBox.Text = value?.ToString() ?? string.Empty;
                break;
            case ComboBox comboBox:
                if (value != null)
                    comboBox.SelectedValue = value;
                break;
            case CheckBox checkBox:
                if (value is bool b)
                    checkBox.Checked = b;
                break;
            case NumericUpDown numeric:
                if (value is decimal d)
                    numeric.Value = d;
                else if (decimal.TryParse(value?.ToString(), out var parsed))
                    numeric.Value = parsed;
                break;
            case DateTimePicker picker:
                if (value is DateTime dt)
                    picker.Value = dt;
                break;
        }
    }

    /// <summary>
    /// 获取字符串值。
    /// </summary>
    public string GetStringValue(string fieldName)
    {
        var control = _formControls.FirstOrDefault(c => c.Name == fieldName);
        return control switch
        {
            TextBox textBox => textBox.Text,
            ComboBox comboBox => comboBox.SelectedValue?.ToString() ?? string.Empty,
            CheckBox checkBox => checkBox.Checked.ToString(),
            _ => string.Empty
        };
    }

    #endregion

    #region 虚方法

    /// <summary>
    /// 加载实体数据。
    /// </summary>
    protected virtual Task LoadEntityAsync(object entityId)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 验证表单。
    /// </summary>
    protected virtual bool ValidateForm()
    {
        return true;
    }

    /// <summary>
    /// 收集表单数据到实体。
    /// </summary>
    protected virtual Task CollectDataAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 保存前处理。
    /// </summary>
    protected virtual async Task<bool> BeforeSaveAsync()
    {
        return true;
    }

    /// <summary>
    /// 保存后处理。
    /// </summary>
    protected virtual Task AfterSaveAsync()
    {
        return Task.CompletedTask;
    }

    #endregion

    #region 表单控件辅助方法

    /// <summary>
    /// 添加表单行。
    /// </summary>
    protected void AddFormRow(string labelText, Control inputControl)
    {
        if (_tableLayout == null) return;

        // 重置行列索引
        if (CurrentColIndex >= FormColumns)
        {
            CurrentColIndex = 0;
            CurrentRowIndex++;
        }

        // 创建标签
        var label = new Label
        {
            Text = labelText,
            AutoSize = false,
            Size = new Size(LabelWidth, 30),
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 10, 0),
            Font = new Font("微软雅黑", 9F),
            ForeColor = Color.FromArgb(64, 64, 64)
        };

        // 设置输入控件属性
        inputControl.Name = GetFieldName(labelText);
        inputControl.Size = new Size(InputWidth, 30);

        _formControls.Add(inputControl);

        // 添加到表格
        var colIndex = CurrentColIndex * 2;
        _tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _tableLayout.Controls.Add(label, colIndex, CurrentRowIndex);
        _tableLayout.Controls.Add(inputControl, colIndex + 1, CurrentRowIndex);

        CurrentColIndex++;

        if (CurrentColIndex >= FormColumns)
        {
            CurrentColIndex = 0;
            CurrentRowIndex++;
        }
    }

    /// <summary>
    /// 添加文本框。
    /// </summary>
    protected TextBox AddTextBox(string labelText, string? defaultValue = null, bool required = false)
    {
        var textBox = new TextBox
        {
            Size = new Size(InputWidth, 30),
            Font = new Font("微软雅黑", 9F),
            BorderStyle = BorderStyle.FixedSingle,
            Text = defaultValue ?? string.Empty
        };

        AddFormRow(labelText, textBox);

        if (required)
        {
            AddRequiredMarkToTextBox(textBox);
        }

        return textBox;
    }

    /// <summary>
    /// 添加只读文本框。
    /// </summary>
    protected TextBox AddReadOnlyTextBox(string labelText, string? value = null)
    {
        var textBox = AddTextBox(labelText, value);
        textBox.ReadOnly = true;
        textBox.BackColor = Color.FromArgb(233, 236, 239);
        return textBox;
    }

    /// <summary>
    /// 添加下拉框。
    /// </summary>
    protected ComboBox AddComboBox(string labelText, object? selectedValue = null, bool required = false)
    {
        var comboBox = new ComboBox
        {
            Size = new Size(InputWidth, 30),
            Font = new Font("微软雅黑", 9F),
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            SelectedValue = selectedValue
        };

        AddFormRow(labelText, comboBox);

        return comboBox;
    }

    /// <summary>
    /// 添加数字输入框。
    /// </summary>
    protected NumericUpDown AddNumericBox(string labelText, decimal defaultValue = 0, decimal min = 0, decimal max = 100, int decimals = 0)
    {
        var numeric = new NumericUpDown
        {
            Size = new Size(InputWidth, 30),
            Font = new Font("微软雅黑", 9F),
            Value = defaultValue,
            Minimum = min,
            Maximum = max,
            DecimalPlaces = decimals,
            BorderStyle = BorderStyle.FixedSingle
        };

        AddFormRow(labelText, numeric);

        return numeric;
    }

    /// <summary>
    /// 添加日期选择框。
    /// </summary>
    protected DateTimePicker AddDatePicker(string labelText, DateTime? defaultValue = null)
    {
        var picker = new DateTimePicker
        {
            Size = new Size(InputWidth, 30),
            Font = new Font("微软雅黑", 9F),
            Value = defaultValue ?? DateTime.Now,
            Format = DateTimePickerFormat.Short,
            ShowUpDown = false
        };

        AddFormRow(labelText, picker);

        return picker;
    }

    /// <summary>
    /// 添加日期时间选择框。
    /// </summary>
    protected DateTimePicker AddDateTimePicker(string labelText, DateTime? defaultValue = null)
    {
        var picker = new DateTimePicker
        {
            Size = new Size(InputWidth, 30),
            Font = new Font("微软雅黑", 9F),
            Value = defaultValue ?? DateTime.Now,
            Format = DateTimePickerFormat.Long,
            ShowUpDown = true
        };

        AddFormRow(labelText, picker);

        return picker;
    }

    /// <summary>
    /// 添加复选框。
    /// </summary>
    protected CheckBox AddCheckBox(string labelText, bool defaultValue = false)
    {
        var checkBox = new CheckBox
        {
            Text = labelText,
            Size = new Size(InputWidth, 30),
            Font = new Font("微软雅黑", 9F),
            Checked = defaultValue,
            AutoSize = false,
            Padding = new Padding(0, 5, 0, 0)
        };

        // 复选框需要特殊处理，放在同一列
        if (_tableLayout == null) return checkBox;

        if (CurrentColIndex > 0)
        {
            CurrentColIndex = 0;
            CurrentRowIndex++;
        }

        _tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var colIndex = CurrentColIndex * 2;
        _tableLayout.SetColumnSpan(checkBox, 2);
        _tableLayout.Controls.Add(checkBox, colIndex, CurrentRowIndex);

        CurrentColIndex = 0;
        CurrentRowIndex++;

        _formControls.Add(checkBox);

        return checkBox;
    }

    /// <summary>
    /// 添加文本区域。
    /// </summary>
    protected TextBox AddTextArea(string labelText, int height = 80, string? defaultValue = null)
    {
        var textBox = new TextBox
        {
            Size = new Size(InputWidth * 2 + 10, height),
            Font = new Font("微软雅黑", 9F),
            BorderStyle = BorderStyle.FixedSingle,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            Text = defaultValue ?? string.Empty
        };

        if (_tableLayout == null) return textBox;

        if (CurrentColIndex > 0)
        {
            CurrentColIndex = 0;
            CurrentRowIndex++;
        }

        _tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var colIndex = CurrentColIndex * 2;
        _tableLayout.SetColumnSpan(textBox, 2);
        _tableLayout.Controls.Add(textBox, colIndex, CurrentRowIndex);

        CurrentColIndex = 0;
        CurrentRowIndex++;

        _formControls.Add(textBox);

        return textBox;
    }

    /// <summary>
    /// 添加必填标记。
    /// </summary>
    private void AddRequiredMarkToTextBox(TextBox textBox)
    {
        textBox.BackColor = Color.FromArgb(255, 245, 238);
    }

    /// <summary>
    /// 从标签文本获取字段名。
    /// </summary>
    private string GetFieldName(string labelText)
    {
        var name = labelText.TrimEnd('*', ' ');
        // 移除特殊字符，转换为驼峰命名
        var words = name.Split(' ', '-', '_');
        if (words.Length == 1)
            return char.ToLower(words[0][0]) + words[0].Substring(1);

        var result = words[0].ToLower();
        for (var i = 1; i < words.Length; i++)
        {
            if (words[i].Length > 0)
                result += char.ToUpper(words[i][0]) + words[i].Substring(1);
        }
        return result;
    }

    /// <summary>
    /// 转换值类型。
    /// </summary>
    private static T? ConvertTo<T>(string value)
    {
        try
        {
            if (typeof(T) == typeof(string))
                return (T)(object)value;
            if (typeof(T) == typeof(int))
                return (T)(object)int.Parse(value);
            if (typeof(T) == typeof(decimal))
                return (T)(object)decimal.Parse(value);
            if (typeof(T) == typeof(double))
                return (T)(object)double.Parse(value);
            if (typeof(T) == typeof(bool))
                return (T)(object)bool.Parse(value);
            if (typeof(T) == typeof(DateTime))
                return (T)(object)DateTime.Parse(value);

            return default;
        }
        catch
        {
            return default;
        }
    }

    #endregion

    #region 事件处理

    /// <summary>
    /// 保存按钮点击。
    /// </summary>
    private async void OnSaveClick(object? sender, EventArgs e)
    {
        if (!ValidateForm())
        {
            return;
        }

        await ExecuteAsync(async () =>
        {
            await CollectDataAsync();

            if (!await BeforeSaveAsync())
                return;

            await SaveAsync();

            await AfterSaveAsync();

            DialogResult = DialogResult.OK;
            Close();
        }, "正在保存...", true);
    }

    /// <summary>
    /// 取消按钮点击。
    /// </summary>
    private void OnCancelClick(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }

    /// <summary>
    /// 保存数据。由子类实现。
    /// </summary>
    protected virtual Task SaveAsync()
    {
        return Task.CompletedTask;
    }

    #endregion
}
