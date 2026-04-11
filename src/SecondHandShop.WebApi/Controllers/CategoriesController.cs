using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.UseCases.Categories;
using SecondHandShop.Application.UseCases.Categories.CreateCategory;
using SecondHandShop.Application.UseCases.Categories.GetCategoryTree;

namespace SecondHandShop.WebApi.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(ICategoryRepository categoryRepository, IMediator mediator) : ControllerBase
{
    [HttpGet]
    [OutputCache(PolicyName = "CategoriesList")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<IReadOnlyList<CategoryResponse>>> ListAsync(CancellationToken cancellationToken)
    {
        var categories = await categoryRepository.ListActiveAsync(cancellationToken);
        var response = categories
            .Select(x => new CategoryResponse(
                x.Id,
                x.Name,
                x.Slug,
                x.ParentId,
                x.SortOrder,
                x.IsActive))
            .ToList();

        return Ok(response);
    }

    [HttpGet("tree")]
    [OutputCache(PolicyName = "CategoriesTree")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<ActionResult<IReadOnlyList<CategoryTreeItemResponse>>> GetTreeAsync(CancellationToken cancellationToken)
    {
        var tree = await mediator.Send(new GetCategoryTreeQuery(), cancellationToken);
        return Ok(tree.Select(MapTreeItem).ToList());
    }

    [HttpPost]
    [Authorize(Policy = "AdminFullAccess")]
    public async Task<ActionResult<CreateCategoryResponse>> CreateAsync(
        [FromBody] CreateCategoryApiRequest request,
        CancellationToken cancellationToken)
    {
        var category = await mediator.Send(
            new CreateCategoryCommand(
                request.Name,
                request.Slug,
                request.ParentId,
                request.SortOrder,
                request.IsActive,
                GetAdminUserId()),
            cancellationToken);

        return Created($"/api/categories/{category.Id}", new CreateCategoryResponse(category.Id));
    }

    private Guid? GetAdminUserId()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    private static CategoryTreeItemResponse MapTreeItem(CategoryDto dto)
    {
        return new CategoryTreeItemResponse(
            dto.Id,
            dto.Name,
            dto.Slug,
            dto.Children.Select(MapTreeItem).ToList());
    }
}

public sealed record CategoryResponse(
    Guid Id,
    string Name,
    string Slug,
    Guid? ParentId,
    int SortOrder,
    bool IsActive);

public sealed record CreateCategoryApiRequest(
    string Name,
    string Slug,
    Guid? ParentId,
    int SortOrder = 0,
    bool IsActive = true);

public sealed record CreateCategoryResponse(Guid Id);

public sealed record CategoryTreeItemResponse(
    Guid Id,
    string Name,
    string Slug,
    List<CategoryTreeItemResponse> Children);
