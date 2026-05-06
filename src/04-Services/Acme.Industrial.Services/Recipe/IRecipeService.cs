namespace Acme.Industrial.Services.Recipe;

/// <summary>
/// 配方参数
/// </summary>
public class RecipeParameter
{
    /// <summary>关联的点位名称</summary>
    public string TagName { get; init; } = string.Empty;

    /// <summary>默认值</summary>
    public object? DefaultValue { get; set; }

    /// <summary>最小值</summary>
    public object? MinValue { get; set; }

    /// <summary>最大值</summary>
    public object? MaxValue { get; set; }

    /// <summary>单位</summary>
    public string? Unit { get; set; }
}

/// <summary>
/// 配方
/// </summary>
public class Recipe
{
    /// <summary>配方ID</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>配方名称</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>分类</summary>
    public string? Category { get; set; }

    /// <summary>描述</summary>
    public string? Description { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedAt { get; init; } = DateTime.Now;

    /// <summary>修改时间</summary>
    public DateTime ModifiedAt { get; set; } = DateTime.Now;

    /// <summary>作者</summary>
    public string? Author { get; set; }

    /// <summary>配方参数列表</summary>
    public List<RecipeParameter> Parameters { get; set; } = new();
}

/// <summary>
/// 配方验证结果
/// </summary>
public record RecipeValidationResult(bool IsValid, List<string> InvalidParameters, List<string> Errors);

/// <summary>
/// 配方服务接口
/// </summary>
public interface IRecipeService : Acme.Industrial.Core.Abstractions.IInitializable
{
    /// <summary>
    /// 获取指定分类的所有配方
    /// </summary>
    Task<System.Collections.Generic.IReadOnlyList<Recipe>> GetAllRecipesAsync(string? category = null, CancellationToken ct = default);

    /// <summary>
    /// 获取配方详情
    /// </summary>
    Task<Recipe?> GetRecipeAsync(string recipeId, CancellationToken ct = default);

    /// <summary>
    /// 保存配方
    /// </summary>
    Task<Acme.Industrial.Core.Results.OperateResult> SaveRecipeAsync(Recipe recipe, CancellationToken ct = default);

    /// <summary>
    /// 删除配方
    /// </summary>
    Task<Acme.Industrial.Core.Results.OperateResult> DeleteRecipeAsync(string recipeId, CancellationToken ct = default);

    /// <summary>
    /// 加载配方到设备（将配方值写入点位）
    /// </summary>
    Task<Acme.Industrial.Core.Results.OperateResult> LoadRecipeAsync(string recipeId, CancellationToken ct = default);

    /// <summary>
    /// 将当前设备状态保存为配方
    /// </summary>
    Task<Acme.Industrial.Core.Results.OperateResult> SaveCurrentAsRecipeAsync(string recipeId, string name, string? category = null, CancellationToken ct = default);

    /// <summary>
    /// 验证配方参数
    /// </summary>
    Task<RecipeValidationResult> ValidateRecipeAsync(Recipe recipe, CancellationToken ct = default);
}
