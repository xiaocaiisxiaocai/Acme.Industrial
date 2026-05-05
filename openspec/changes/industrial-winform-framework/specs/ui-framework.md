# UI Framework - UI框架层规格

## Overview

UI 框架层提供 WinForm 开发所需的基础设施，包括 MVP 模式、窗体基类、导航服务、工业控件。

## Requirements

### R-1: MVP 模式基类

#### R-1.1 IView 接口

```csharp
public interface IView
{
    void ShowLoading(string? message = null);
    void HideLoading();
    void ShowMessage(string message);
    void ShowError(string message, Exception? ex = null);
    Task<bool> ConfirmAsync(string message);
}
```

#### R-1.2 IPresenter 接口

```csharp
public interface IPresenter
{
    Task OnLoadAsync(CancellationToken ct);
    Task OnUnloadAsync(CancellationToken ct);
}

public abstract class PresenterBase<TView> : IPresenter where TView : IView
{
    protected TView View { get; }
    protected PresenterBase(TView view) => View = view;
    public virtual Task OnLoadAsync(CancellationToken ct) => Task.CompletedTask;
    public virtual Task OnUnloadAsync(CancellationToken ct) => Task.CompletedTask;
}
```

### R-2: 窗体基类

```csharp
public abstract class BaseForm : Form, IView
{
    protected void ShowLoading(string? message);
    protected void HideLoading();
    protected Task<bool> ShowConfirmAsync(string message);
    protected void ShowToast(string message, NotificationLevel level = NotificationLevel.Info);
}
```

### R-3: 业务窗体模板

| 模板 | 用途 |
|------|------|
| BaseListForm | 查询 + 表格 + 分页 |
| BaseEditForm | 表单编辑 |
| BaseTreeListForm | 树 + 列表 |
| BaseChartForm | 图表展示 |

### R-4: 导航服务

```csharp
public interface IViewRegistry
{
    void Register<TView, TPresenter>(string viewKey)
        where TView : class, IView
        where TPresenter : class, IPresenter;
}

public interface INavigationService
{
    Task NavigateAsync(string viewKey, object? parameter = null);
    Task<TResult?> ShowDialogAsync<TResult>(string viewKey, object? parameter = null);
    Task GoBackAsync();
}
```

### R-5: 菜单注册

```csharp
public class MenuItemDescriptor
{
    public string Id { get; init; }
    public string Title { get; init; }
    public string? Icon { get; init; }
    public string? ParentId { get; init; }
    public string? ViewKey { get; init; }
    public string? Permission { get; init; }
    public int Order { get; init; }
}

public interface IMenuRegistry
{
    void Register(MenuItemDescriptor item);
    IReadOnlyList<MenuItemDescriptor> GetAll();
    IReadOnlyList<MenuItemDescriptor> GetForCurrentUser();
}
```

### R-6: 工业控件

#### 指示类

| 控件 | 说明 |
|------|------|
| IndicatorLamp | 指示灯 (开/关/闪烁) |
| DigitalDisplay | 数显控件 |
| AnalogGauge | 模拟仪表 |
| ThermometerControl | 温度计 |

#### 操作类

| 控件 | 说明 |
|------|------|
| IndustrialButton | 工业按钮 (带状态色) |
| ToggleSwitch | 开关 |
| EmergencyButton | 急停按钮 |

#### 图表类

| 控件 | 说明 |
|------|------|
| RealtimeTrendChart | 实时趋势图 |
| HistoryTrendChart | 历史趋势图 |
| WaveformChart | 波形图 |

#### 组态类

| 控件 | 说明 |
|------|------|
| PipeControl | 管道 |
| ValveControl | 阀门 |
| MotorControl | 电机 |
| TankControl | 料仓 |

### R-7: 本地化

```csharp
public interface ILocalizer
{
    string this[string key] { get; }
    string Get(string key, params object[] args);
    string CurrentCulture { get; }
    void ChangeCulture(string cultureName);
    event EventHandler? CultureChanged;
}
```

### R-8: 通知服务

```csharp
public enum NotificationLevel { Info, Success, Warning, Error }

public interface INotificationService
{
    void Show(string message, NotificationLevel level = NotificationLevel.Info,
        TimeSpan? duration = null);
    Task<bool> ConfirmAsync(string title, string message);
    Task<string?> PromptAsync(string title, string message, string? defaultValue = null);
}
```

---

## Acceptance Criteria

- [ ] IView/IViewModel 支持所有 UI 反馈操作
- [ ] BaseForm 提供统一的加载/消息/确认方法
- [ ] 业务窗体模板覆盖 CRUD 场景
- [ ] INavigationService 支持参数传递
- [ ] 工业控件支持数据绑定
- [ ] 控件支持主题切换 (亮/暗)
- [ ] 本地化支持运行时切换
- [ ] 控件支持缩放 (适应不同屏幕分辨率)

---

## Dependencies

- `core-abstractions`
