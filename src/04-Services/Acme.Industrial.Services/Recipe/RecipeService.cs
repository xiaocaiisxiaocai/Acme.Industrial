using Acme.Industrial.Core.Logging;
using Acme.Industrial.Core.Results;
using System.Collections.Concurrent;

namespace Acme.Industrial.Services.Recipe;

/// <summary>
/// 配方服务实现
/// </summary>
public class RecipeService : IRecipeService
{
    private readonly IAppLogger _logger;
    private readonly ConcurrentDictionary<string, Recipe> _recipes = new();

    public RecipeService(IAppLogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task InitializeAsync(CancellationToken ct = default)
    {
        _logger.Info("配方服务初始化完成，共加载 " + _recipes.Count + " 个配方");
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Recipe>> GetAllRecipesAsync(string? category = null, CancellationToken ct = default)
    {
        var recipes = _recipes.Values.AsEnumerable();

        if (!string.IsNullOrEmpty(category))
        {
            recipes = recipes.Where(r => r.Category == category);
        }

        var result = recipes.OrderBy(r => r.Category).ThenBy(r => r.Name).ToList();
        return Task.FromResult<IReadOnlyList<Recipe>>(result);
    }

    public Task<Recipe?> GetRecipeAsync(string recipeId, CancellationToken ct = default)
    {
        _recipes.TryGetValue(recipeId, out var recipe);
        return Task.FromResult(recipe);
    }

    public Task<OperateResult> SaveRecipeAsync(Recipe recipe, CancellationToken ct = default)
    {
        if (recipe == null)
            return Task.FromResult(OperateResult.Fail(-1, "配方不能为空"));

        if (string.IsNullOrEmpty(recipe.Name))
            return Task.FromResult(OperateResult.Fail(-1, "配方名称不能为空"));

        recipe.ModifiedAt = DateTime.Now;
        _recipes[recipe.Id] = recipe;

        _logger.Info("保存配方: " + recipe.Id + " - " + recipe.Name);
        return Task.FromResult(OperateResult.Ok());
    }

    public Task<OperateResult> DeleteRecipeAsync(string recipeId, CancellationToken ct = default)
    {
        if (_recipes.TryRemove(recipeId, out var removed))
        {
            _logger.Info("删除配方: " + removed.Id + " - " + removed.Name);
            return Task.FromResult(OperateResult.Ok());
        }

        return Task.FromResult(OperateResult.Fail(-1, "配方 " + recipeId + " 不存在"));
    }

    public Task<OperateResult> LoadRecipeAsync(string recipeId, CancellationToken ct = default)
    {
        if (!_recipes.TryGetValue(recipeId, out var recipe))
        {
            return Task.FromResult(OperateResult.Fail(-1, "配方 " + recipeId + " 不存在"));
        }

        _logger.Info("加载配方: " + recipe.Id + " - " + recipe.Name + ", 包含 " + recipe.Parameters.Count + " 个参数");

        return Task.FromResult(OperateResult.Ok());
    }

    public Task<OperateResult> SaveCurrentAsRecipeAsync(string recipeId, string name, string? category = null, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(recipeId))
            return Task.FromResult(OperateResult.Fail(-1, "配方ID不能为空"));

        if (string.IsNullOrEmpty(name))
            return Task.FromResult(OperateResult.Fail(-1, "配方名称不能为空"));

        var recipe = new Recipe
        {
            Id = recipeId,
            Name = name,
            Category = category,
            CreatedAt = DateTime.Now,
            ModifiedAt = DateTime.Now
        };

        _recipes[recipe.Id] = recipe;

        _logger.Info("创建新配方: " + recipe.Id + " - " + recipe.Name);
        return Task.FromResult(OperateResult.Ok());
    }

    public Task<RecipeValidationResult> ValidateRecipeAsync(Recipe recipe, CancellationToken ct = default)
    {
        var errors = new List<string>();
        var invalidParams = new List<string>();

        if (recipe == null)
        {
            errors.Add("配方不能为空");
            return Task.FromResult(new RecipeValidationResult(false, invalidParams, errors));
        }

        if (string.IsNullOrEmpty(recipe.Name))
        {
            errors.Add("配方名称不能为空");
        }

        foreach (var param in recipe.Parameters)
        {
            if (string.IsNullOrEmpty(param.TagName))
            {
                invalidParams.Add(param.TagName ?? "(空)");
                errors.Add("参数点位名称不能为空");
            }

            if (param.MinValue != null && param.MaxValue != null)
            {
                if (param.DefaultValue != null)
                {
                    var min = Convert.ToDouble(param.MinValue);
                    var max = Convert.ToDouble(param.MaxValue);
                    var defaultVal = Convert.ToDouble(param.DefaultValue);

                    if (defaultVal < min || defaultVal > max)
                    {
                        invalidParams.Add(param.TagName);
                        errors.Add("参数 " + param.TagName + " 的默认值 " + defaultVal + " 超出范围 [" + min + ", " + max + "]");
                    }
                }
            }
        }

        return Task.FromResult(new RecipeValidationResult(errors.Count == 0, invalidParams, errors));
    }
}
