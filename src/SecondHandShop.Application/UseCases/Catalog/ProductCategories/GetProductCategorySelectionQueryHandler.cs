using MediatR;
using SecondHandShop.Application.Abstractions.Persistence;

namespace SecondHandShop.Application.UseCases.Catalog.ProductCategories;

public sealed class GetProductCategorySelectionQueryHandler(IProductRepository productRepository)
    : IRequestHandler<GetProductCategorySelectionQuery, ProductCategorySelectionDto>
{
    public async Task<ProductCategorySelectionDto> Handle(
        GetProductCategorySelectionQuery request,
        CancellationToken cancellationToken)
    {
        var product = await productRepository.GetByIdWithCategoriesAsync(request.ProductId, cancellationToken)
            ?? throw new KeyNotFoundException($"Product '{request.ProductId}' was not found.");

        var selectedCategoryIds = product.ProductCategories
            .Select(x => x.CategoryId)
            .Append(product.CategoryId)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        return new ProductCategorySelectionDto(
            product.Id,
            product.CategoryId,
            selectedCategoryIds);
    }
}
