using MediatR;
using SecondHandShop.Application.Abstractions.Common;
using SecondHandShop.Application.Abstractions.Persistence;
using SecondHandShop.Application.Common.Exceptions;
using SecondHandShop.Domain.Entities;

namespace SecondHandShop.Application.UseCases.Catalog.ProductCategories;

public sealed class UpdateProductCategoriesCommandHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IClock clock) : IRequestHandler<UpdateProductCategoriesCommand, ProductCategorySelectionDto>
{
    public async Task<ProductCategorySelectionDto> Handle(
        UpdateProductCategoriesCommand request,
        CancellationToken cancellationToken)
    {
        if (request.ProductId == Guid.Empty)
        {
            throw new ValidationException("ProductId is required.");
        }

        if (request.MainCategoryId == Guid.Empty)
        {
            throw new ValidationException("MainCategoryId is required.");
        }

        var product = await productRepository.GetByIdWithCategoriesAsync(request.ProductId, cancellationToken)
            ?? throw new KeyNotFoundException($"Product '{request.ProductId}' was not found.");

        var finalCategoryIds = NormalizeSelectedCategoryIds(request.MainCategoryId, request.SelectedCategoryIds);
        var categories = await categoryRepository.ListByIdsAsync(finalCategoryIds, cancellationToken);
        if (categories.Count != finalCategoryIds.Count)
        {
            var validCategoryIds = categories.Select(x => x.Id).ToHashSet();
            var invalidIds = finalCategoryIds.Where(id => !validCategoryIds.Contains(id)).ToList();
            throw new ValidationException(
                $"One or more category ids are invalid: {string.Join(", ", invalidIds)}");
        }

        product.UpdateMainCategory(request.MainCategoryId, request.AdminUserId, clock.UtcNow);

        var finalCategoryIdSet = finalCategoryIds.ToHashSet();
        var existingCategoryIdSet = product.ProductCategories
            .Select(x => x.CategoryId)
            .ToHashSet();

        foreach (var productCategory in product.ProductCategories
                     .Where(x => !finalCategoryIdSet.Contains(x.CategoryId))
                     .ToList())
        {
            product.ProductCategories.Remove(productCategory);
        }

        foreach (var categoryId in finalCategoryIds.Where(id => !existingCategoryIdSet.Contains(id)))
        {
            product.ProductCategories.Add(ProductCategory.Create(product.Id, categoryId));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new ProductCategorySelectionDto(
            product.Id,
            product.CategoryId,
            finalCategoryIds);
    }

    private static List<Guid> NormalizeSelectedCategoryIds(
        Guid mainCategoryId,
        IReadOnlyCollection<Guid>? selectedCategoryIds)
    {
        var normalizedCategoryIds = (selectedCategoryIds ?? [])
            .Where(id => id != Guid.Empty)
            .Append(mainCategoryId)
            .Distinct()
            .ToList();

        if (normalizedCategoryIds.Count == 0)
        {
            throw new ValidationException("At least one category is required.");
        }

        return normalizedCategoryIds;
    }
}
