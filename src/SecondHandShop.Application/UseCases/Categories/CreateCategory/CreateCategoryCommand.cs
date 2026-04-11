using MediatR;
using SecondHandShop.Application.UseCases.Categories;

namespace SecondHandShop.Application.UseCases.Categories.CreateCategory;

public sealed record CreateCategoryCommand(
    string Name,
    string Slug,
    Guid? ParentId,
    int SortOrder = 0,
    bool IsActive = true,
    Guid? CreatedByAdminUserId = null) : IRequest<CategoryDto>;
