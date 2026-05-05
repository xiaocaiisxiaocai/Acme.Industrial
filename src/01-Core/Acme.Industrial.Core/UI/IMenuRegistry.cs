namespace Acme.Industrial.Core.UI;

/// <summary>
/// 菜单项描述符。
/// </summary>
public class MenuItemDescriptor
{
    /// <summary>
    /// 菜单项 ID。
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// 标题。
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// 图标路径。
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// 父菜单 ID。
    /// </summary>
    public string? ParentId { get; init; }

    /// <summary>
    /// 视图键。
    /// </summary>
    public string? ViewKey { get; init; }

    /// <summary>
    /// 权限。
    /// </summary>
    public string? Permission { get; init; }

    /// <summary>
    /// 排序顺序。
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// 是否可见。
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// 菜单注册表接口。
/// </summary>
public interface IMenuRegistry
{
    /// <summary>
    /// 注册菜单项。
    /// </summary>
    void Register(MenuItemDescriptor item);

    /// <summary>
    /// 获取所有菜单项。
    /// </summary>
    IReadOnlyList<MenuItemDescriptor> GetAll();

    /// <summary>
    /// 获取当前用户可见的菜单项。
    /// </summary>
    IReadOnlyList<MenuItemDescriptor> GetForCurrentUser();

    /// <summary>
    /// 获取子菜单项。
    /// </summary>
    IReadOnlyList<MenuItemDescriptor> GetChildren(string parentId);

    /// <summary>
    /// 移除菜单项。
    /// </summary>
    void Remove(string menuId);
}
