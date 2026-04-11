using MediatR;

namespace SecondHandShop.Application.UseCases.Catalog.ProductCategories;

public sealed record UpdateProductCategoriesCommand(
    Guid ProductId,
    Guid MainCategoryId,
    IReadOnlyCollection<Guid>? SelectedCategoryIds,
    Guid? AdminUserId = null) : IRequest<ProductCategorySelectionDto>;
