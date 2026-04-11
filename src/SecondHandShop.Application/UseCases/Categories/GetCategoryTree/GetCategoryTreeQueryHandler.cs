using MediatR;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.UseCases.Categories;
using SecondHandShop.Domain.Common;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Application.UseCases.Categories.GetCategoryTree;

public sealed class GetCategoryTreeQueryHandler(ICategoryRepository categoryRepository)
    : IRequestHandler<GetCategoryTreeQuery, IReadOnlyList<CategoryDto>>
{
    public async Task<IReadOnlyList<CategoryDto>> Handle(
        GetCategoryTreeQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await categoryRepository.ListActiveAsync(cancellationToken);
        var rootCategories = categories
            .Where(x => !x.ParentId.HasValue)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToList();

        var categoriesByParent = categories
            .Where(x => x.ParentId.HasValue)
            .GroupBy(x => x.ParentId!.Value)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<Category>)x
                    .OrderBy(c => c.SortOrder)
                    .ThenBy(c => c.Name)
                    .ToList());

        return BuildChildren(rootCategories, categoriesByParent, path: []);
    }

    private static IReadOnlyList<CategoryDto> BuildChildren(
        IReadOnlyList<Category> categories,
        IReadOnlyDictionary<Guid, IReadOnlyList<Category>> categoriesByParent,
        HashSet<Guid> path)
    {
        return categories
            .Select(child =>
            {
                if (!path.Add(child.Id))
                {
                    throw new DomainRuleViolationException("Circular category hierarchy detected.");
                }

                var dto = new CategoryDto(
                    child.Id,
                    child.Name,
                    child.Slug,
                    BuildChildren(
                        categoriesByParent.GetValueOrDefault(child.Id, []),
                        categoriesByParent,
                        path).ToList());

                path.Remove(child.Id);
                return dto;
            })
            .ToList();
    }
}
