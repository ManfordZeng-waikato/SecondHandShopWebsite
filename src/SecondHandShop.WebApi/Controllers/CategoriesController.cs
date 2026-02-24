using Microsoft.AspNetCore.Mvc;
using SecondHandShop.Application.Abstractions.Persistence;

namespace SecondHandShop.WebApi.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(ICategoryRepository categoryRepository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> ListAsync(CancellationToken cancellationToken)
    {
        var categories = await categoryRepository.ListActiveAsync(cancellationToken);
        var response = categories
            .Select(x => new CategoryResponse(
                x.Id,
                x.Name,
                x.Slug,
                x.ParentCategoryId,
                x.SortOrder,
                x.IsActive))
            .ToList();

        return Ok(response);
    }
}

public sealed record CategoryResponse(
    Guid Id,
    string Name,
    string Slug,
    Guid? ParentCategoryId,
    int SortOrder,
    bool IsActive);
